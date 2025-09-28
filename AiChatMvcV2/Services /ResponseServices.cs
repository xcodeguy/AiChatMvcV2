using System.Text.Json;
using AiChatMvcV2.Contracts;
using AiChatMvcV2.Objects;
using Microsoft.Extensions.Options;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.IO;
using System.Collections.Generic;

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
            List<char> NO_CHARS = new() { '{', '}', '\\' };
            List<char> GOOD_CHARS = new()
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

            try
            {
                Dictionary<string, object> item = JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;

                string HtmlTagPattern = $"(<[^>]*>)";
                string ThinkTagPattern = $"(?<=<think>).*?(?=</think>)";
                string OriginalString = item["response"].ToString()!;
                if(OriginalString.Contains("<think>"))
                {
                    Console.WriteLine("Found think tag in response");
                }
                string BetterString = Regex.Replace(OriginalString, ThinkTagPattern, string.Empty);
                string CleanString = Regex.Replace(BetterString, HtmlTagPattern, string.Empty);
                CleanString = CleanString.ToString()!.Replace("\n", string.Empty);
                CleanString = CleanString.ToString()!.Replace("\"", string.Empty);
                CleanString = CleanString.ToString()!.Replace("\\", string.Empty);

                int resultLength = CleanString != null ? CleanString.ToString()!.Length : 0;
                StringBuilder result = new(resultLength);
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
            catch (Exception ex)
            {
                _logger.LogCritical("ResponseServices::SanitizeResponseFromJson() Exception: {ExceptionMessage}", ex.Message);
                throw;
            }

        }

        public async Task<string> GenerateSpeechFile(string TextForSpeech, string Voice)
        {
            string TtsEndpointUrl = _settings.TTSApiEndpointUrl;
            string ModelName = _settings.TTSModelName;
            string TtsRequest;

            try
            {
                TtsRequest = string.Format("{{\"model\": \"{0}\", \"input\": \"{1}\", \"voice\": \"{2}\", \"response_format\" : \"{3}\", \"speed\":\"{4}\"}}",
                                        ModelName,
                                        TextForSpeech,
                                        Voice,
                                        _settings.SpeechFileFormat,
                                        _settings.PlaybackSpeed);

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(300); //5 min timeout

                    _logger.LogInformation("Calling TTS endpoint");
                    var content = new StringContent(TtsRequest, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(TtsEndpointUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("TTS model response success");

                        string ContentDisposiion;
                        string DropFilename;
                        string WebAssetFilename;
                        string LocalAssetFilename;

                        //get the content-disposition from the header
                        //Content-Disposition: attachment; filename="your_audio_file.wav"                     ;
                        ContentDisposiion = response.Content.Headers.GetValues("Content-Disposition").ToList()[0];

                        //build the filename from the drop location and from the header
                        //if the header is null throw an exception 
                        DropFilename = _settings.SpeechFileDropLocation!;
                        if (ContentDisposiion.ToString() is null)
                        {
                            ExceptionMessageString = string.Format("Exception in ResponseController::GenerateTextToSpeechResourceFile() {0} {1}, {2}\nException: {3}", ModelName, TtsEndpointUrl, TextForSpeech, "Content-Disposition header is null");
                            throw new Exception(ExceptionMessageString);
                        }
                        DropFilename += ContentDisposiion.Split("=")[1].ToString().Replace("\"", string.Empty);

                        //copy the speech file to the website assets location
                        WebAssetFilename = CopySpeechFileToAssets(DropFilename);
                        LocalAssetFilename = _settings.SpeechFilePlaybackLocation! + WebAssetFilename;

                        _logger.LogInformation("Returning sound file ({Filename}): DropFile={source}, Destination={dest}", WebAssetFilename, DropFilename, LocalAssetFilename);
                        return WebAssetFilename;
                    }
                    else
                    {
                        ExceptionMessageString = string.Format("Exception in ResponseController::GenerateTextToSpeechResourceFile() {0} {1}, {2}\nException: {3}", ModelName, TtsEndpointUrl, TextForSpeech, response.RequestMessage);
                        throw new Exception(ExceptionMessageString);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical("ResponseServices::GenerateSpeechFile() {MesageString}\n{ExceptionMessage}", ExceptionMessageString, ex.Message);
                throw;
            }
        }

        public string CopySpeechFileToAssets(string SourceFile)
        {
            //get the path..then append default filename to string
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
                _logger.LogInformation("ResponseController::An error occurred while copying speech asset file: {0}", ex.Message);
                throw;
            }

        }

        public void PlayWavOnMac(string filePath)
        {
            if ((filePath == null) || (!File.Exists(filePath)))
            {
                ExceptionMessageString = $"ResponseController::PlayWavOnMac Error: File not found at {(filePath != null ? filePath : "Filename is NULL")}";
                _logger.LogInformation(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }

            try
            {
                ProcessStartInfo startInfo = new()
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
                _logger.LogCritical($"Error playing WAV file: {ex.Message}");
                throw;
            }
        }

    }       //end class

}       //end namespace


