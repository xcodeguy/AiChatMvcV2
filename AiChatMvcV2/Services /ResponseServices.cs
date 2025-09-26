using System.Text.Json;
using AiChatMvcV2.Contracts;
using AiChatMvcV2.Objects;
using Microsoft.Extensions.Options;
using AiChatMvcV2.Services;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AiChatMvcV2.Services  
{
    public class ResponseServices : IResponseServices
    {
        private readonly ILogger<ResponseServices> _logger;
        private readonly ApplicationSettings _settings;
        private string ExceptionMessageString = string.Empty;

        public ResponseServices(IOptions<ApplicationSettings> settings, ILogger<ResponseServices> logger)
        {
            _logger = logger;
            _settings = settings.Value;
            _logger.LogInformation("ResponseController class initialized.");
        }

        public int GetWordCount(string TheResponse)
        {
            string[] a = TheResponse.Split(" ");
            return a.Length;
        }

        public Task<string> SanitizeResponseFromJson(string json)
        {
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

            Dictionary<string, object> item = JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;

            string HtmlTagPattern = "<[^>]*>.*?<\\/[^>]*>";
            string ThinkTagPattern = "<think>.*?<\\/think>";
            string UglyString = item["response"].ToString()!;
            string BetterString = Regex.Replace(UglyString, HtmlTagPattern, string.Empty);
            string CleanString = Regex.Replace(BetterString, ThinkTagPattern, string.Empty);
            CleanString = CleanString.ToString()!.Replace("\n", string.Empty);
            CleanString = CleanString.ToString()!.Replace("\"", string.Empty);
            CleanString = CleanString.ToString()!.Replace("\\", string.Empty);

            int resultLength = CleanString != null ? CleanString.ToString()!.Length : 0;
            StringBuilder result = new StringBuilder(resultLength);
            if (resultLength != 0)
            {
                foreach (char c in CleanString!.ToString()!)
                {
                    if (GOOD_CHARS.Contains(c))
                    {
                        result.Append(c);
                    }
                }
            }

            return Task.FromResult(result != null ? result.ToString()! : "Response from the model is null, empty, or invalid.");
        }

        public async Task<string> GenerateSpeechFile(string ResponseText, string Voice)
        {
            string url = _settings.TTSApiEndpointUrl;
            string ModelName = _settings.TTSModelName;
            string data;

            try
            {
                data = string.Format("{{\"model\": \"{0}\", \"input\": \"{1}\", \"voice\": \"{2}\", \"response_format\" : \"{3}\", \"speed\":\"{4}\"}}",
                                        ModelName,
                                        ResponseText,
                                        Voice,
                                        _settings.SpeechFileFormat,
                                        _settings.PlaybackSpeed);
                                        
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(300); //5 min timeout

                    _logger.LogInformation("Calling TTS endpoint");
                    var content = new StringContent(data, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("TTS model response success");

                        string ContentDisposiion;
                        string DropFilename;
                        string AssetFilename;
                        string LocalAssetFilename;

                        //get the content-disposition from the header
                        //Content-Disposition: attachment; filename="your_audio_file.wav"                     ;
                        ContentDisposiion = response.Content.Headers.GetValues("Content-Disposition").ToList()[0];

                        //build the filename from the drop location and from the header
                        DropFilename = _settings.SpeechFileDropLocation!;
                        DropFilename += ContentDisposiion.Split("=")[1].ToString().Replace("\"", string.Empty);

                        //copy the speech file to the website assets location
                        AssetFilename = CopySpeechFileToAssets(DropFilename);
                        LocalAssetFilename = _settings.SpeechFilePlaybackLocation! + AssetFilename;

                        _logger.LogInformation("Returning sound file ({Filename}): DropFile={source}, Destination={dest}", AssetFilename, DropFilename, LocalAssetFilename);
                        return AssetFilename;
                    }
                    else
                    {
                        ExceptionMessageString = string.Format("Exception in ResponseController::GenerateTextToSpeechResourceFile() {0} {1}, {2}\nException: {3}", ModelName, url, ResponseText, response.RequestMessage);
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


