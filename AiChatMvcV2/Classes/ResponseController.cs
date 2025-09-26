using System.Text.Json;
using AiChatMvcV2.Contracts;
using AiChatMvcV2.Objects;
using Microsoft.Extensions.Options;
using AiChatMvcV2.Classes;
using System.Text;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Media;
using System.Diagnostics;

namespace AiChatMvcV2.Controllers
{
    public class ResponseController : IResponseController
    {
        private readonly ILogger<CallController> _logger;
        private readonly ApplicationSettings _settings;
        private const float temperature = 0.8f;     //0.8
        private const int num_ctx = 2048;           //2048
        private const int num_predict = -1;         //-1
        private string ExceptionMessageString = string.Empty;

        public ResponseController(IOptions<ApplicationSettings> settings, ILogger<CallController> logger)
        {
            _logger = logger;
            _settings = settings.Value;
            _logger.LogInformation("ResponseController class initialized.");
        }

        public String GetTopicFromResponse(string TheResponse)
        {
            try
            {
                string[] PossibleTopics = TheResponse.Split("|");
                if (PossibleTopics.Length > 0)
                {
                    string[] WordsInTopic;

                    if (PossibleTopics.Length == 1)
                    {
                        //only one block of text. take a chance that the
                        //first word in the block will suffice as as topic
                        //description
                        WordsInTopic = PossibleTopics[0].Split(" ");
                        return WordsInTopic[0];
                    }

                    //there are multiple possibilities for a topic
                    //loop through the possible topics from last to first
                    //(because this is the pattern we have in the prompt)
                    //return up to a 3 word topic
                    for (int i = PossibleTopics.Length - 1; i >= 0; i--)
                    {
                        if (PossibleTopics[i].Trim() != String.Empty)
                        {
                            //return up to a 3 word description
                            WordsInTopic = PossibleTopics[i].Split(" ");
                            if (WordsInTopic.Length <= 5)
                            {
                                return PossibleTopics[i];
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical("Exception: ResponseController->GetTopicFromResponse, {ExMessage}", e.Message);
                throw;
            }

            return "Unknown";
        }

        public int GetWordCount(string TheResponse)
        {
            string[] a = TheResponse.Split(" ");
            return a.Length;
        }

        public Task<string> ParseJsonForObject(string json)
        {

            string JsonString = json;

            List<char> NO_CHARS = new List<char>() { '{', '}', '\\' };
            List<char> GOOD_CHARS = new List<char>()
            {
                'a',
                'b',
                'c',
                'd',
                'e',
                'f',
                'g',
                'h',
                'i',
                'j',
                'k',
                'l',
                'm',
                'n',
                'o',
                'p',
                'q',
                'r',
                's',
                't',
                'u',
                'v',
                'w',
                'x',
                'y',
                'z',
                'A',
                'B',
                'C',
                'D',
                'E',
                'F',
                'G',
                'H',
                'I',
                'J',
                'K',
                'L',
                'M',
                'N',
                'O',
                'P',
                'Q',
                'R',
                'S',
                'T',
                'U',
                'V',
                'W',
                'X',
                'Y',
                'Z',
                '0',
                '1',
                '2',
                '3',
                '4',
                '5',
                '6',
                '7',
                '8',
                '9',
                ' ',
                ',',
                ':',
                '[',
                ']',
                '"',
                '\'',
                '<',
                '>',
                '?',
                '/',
                '.',
                '!',
                '@',
                '#',
                '$',
                '%',
                '^',
                '&',
                '*',
                '(',
                ')',
                '-',
                '+',
                '=',
                '|',
                '{',
                '}'
            };

            Dictionary<string, object> item = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonString)!;
            String ReturnString = item!["response"].ToString()!;

            if (ReturnString == null)
            {
                _logger.LogCritical("ParseJsonForObject: 'response' field is null or does not exist in response JSON.");
                return Task.FromResult(string.Empty);
            }

            ReturnString = ReturnString.ToString()!.Replace("\n", String.Empty);
            ReturnString = ReturnString.ToString()!.Replace("\"", String.Empty);
            ReturnString = ReturnString.ToString()!.Replace("\\", String.Empty);

            int resultLength = ReturnString != null ? ReturnString.ToString()!.Length : 0;
            StringBuilder result = new StringBuilder(resultLength);
            if (resultLength != 0)
            {
                foreach (char c in ReturnString!.ToString()!)
                {
                    if (GOOD_CHARS.Contains(c)) {
                        result.Append(c);
                    } 
                }
            }

            return Task.FromResult(result != null ? result.ToString()! : string.Empty);
        }

        public async Task<string> GenerateTextToSpeechResourceFile(string ResponseText, string Voice)
        {
            string? url = _settings.TTSApiEndpointUrl;
            string? ModelName = _settings.TTSModelName;
            string data;
            var options = "\"options\" : {{\"temperature\" : " + temperature + ", \"num_ctx\" : " + num_ctx + ", \"num_predict\" : " + num_predict + "}}";

            try
            {
                data = String.Format("{{\"model\": \"{0}\", \"input\": \"{1}\", \"voice\": \"{2}\", \"response_format\" : \"{3}\", \"speed\":\"{4}\"}}", ModelName, ResponseText, Voice, "wav", "1.0");
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(300); //5 min timeout

                    _logger.LogInformation("Calling TTS endpoint");
                    var content = new StringContent(data, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("TTS model response success");

                        // get the content-disposition from the http header
                        // split on the = sign and take the assumed last element
                        // copy file from source to destination
                        string cd; string fn; string sf;
                        cd = response.Content.Headers.GetValues("Content-Disposition").ToList()[0];

                        //AI generated and its pretty damn good
                        if (string.IsNullOrEmpty(_settings.SpeechFileDropLocation))
                        {
                            _logger.LogCritical("SpeechFileDropLocation is null or empty in ApplicationSettings.");
                            throw new InvalidOperationException("SpeechFileDropLocation cannot be null or empty.");
                        }
                        //end AI

                        //build the filename from the drop location and the filename from the header
                        fn = _settings.SpeechFileDropLocation;
                        fn += cd.Split("=")[1].ToString().Replace("\"", string.Empty);

                        //write the file to the website assets location
                        sf = CopySpeechFileToAssets(fn);
                        _logger.LogInformation("Returning sound file from assets: {model}:{prompt}:{fn}", ModelName, ResponseText, sf);

                        return sf;
                    }
                    else
                    {
                        ExceptionMessageString = String.Format("Exception in CallController::GenerateTextToSpeechResourceFile() {0} {1}, {2}\nException: {3}", ModelName, url, ResponseText, response.RequestMessage);
                        throw new Exception(ExceptionMessageString);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical("{ 0} \n{1}", ExceptionMessageString, ex.Message);
            }

            return string.Empty;
        }

        public string CopySpeechFileToAssets(string SourceFile)
        {
            string DestinationFile = _settings.SpeechFilePlaybackLocation!;
            DestinationFile += _settings.SpeechFilePlaybackName!;

            try
            {
                // Check if the source file exists
                if (!File.Exists(SourceFile))
                {
                    _logger.LogInformation("Error: Source file not found: {file}", SourceFile);
                    return string.Empty;
                }

                // Copy the file
                File.Copy(SourceFile, DestinationFile, true); // The 'true' parameter enables overwriting if the file exists
                _logger.LogInformation("File '{f1}' copied to '{f2}' successfully.", SourceFile, DestinationFile);


                //return just the audio filename. No path, url or location information
                DestinationFile = _settings.SpeechFilePlaybackName!;

                PlayWavOnMac(_settings.SpeechFilePlaybackLocation + DestinationFile);

                return DestinationFile;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("An error occurred: {0}", ex.Message);
            }

            return string.Empty;
        }

        public static void PlayWavOnMac(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found at {filePath}");
                return;
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "afplay",
                    Arguments = $"\"{filePath}\"", // Quote the path to handle spaces
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo)!)
                {
                    process.WaitForExit(); // Optional: Wait for playback to finish
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing WAV file: {ex.Message}");
            }
        }

    }       //end class

}       //end namespace


