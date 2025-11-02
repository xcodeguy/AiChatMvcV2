using System.Text.Json;
using AiChatMvcV2.Contracts;
using AiChatMvcV2.Objects;
using Microsoft.Extensions.Options;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;
using AiChatMvcV2.Models;

namespace AiChatMvcV2.Services
{
    public class ResponseServices : IResponseServices
    {
        #region variables
        private readonly ILogger<ResponseServices> _logger;
        private readonly ApplicationSettings _settings;
        private string ExceptionMessageString = string.Empty;
        Type _classType;
        string _className = string.Empty;
        string _methodName;
        #endregion

        #region methods
        public ResponseServices(IOptions<ApplicationSettings> settings, ILogger<ResponseServices> logger)
        {
            _logger = logger;
            _settings = settings.Value;
            _classType = this.GetType();
            _className = _classType.Name.ToString();
            _methodName = string.Empty;
        }

        public int GetWordCount(string TheResponse)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TheResponse))
                {
                    return 0;
                }
                string[] a = TheResponse.Split(" ");
                return a.Length;
            }
            catch (Exception ex)
            {
                _methodName = MethodBase.GetCurrentMethod()?.Name ?? "Unknown Method";
                _logger.LogCritical($"{_className}.{_methodName}: {ex.Message}");
            }
            return 0;
        }

        public Task<string> RemoveHtmlAndThinkTagsFromModelResponse(string json)
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
                string ResponseString = item["response"].ToString()!;

                if (ResponseString == null)
                {
                    return Task.FromResult("Response from the model is null or empty.");
                }

                if (ResponseString.Contains("<think>"))
                {
                    _logger.LogInformation("Found think tag in response. Deleting elements.");
                    ResponseString = Regex.Replace(ResponseString, ThinkTagPattern, string.Empty);
                }

                if (ResponseString.Contains("</"))
                {
                    _logger.LogInformation("Found html tags in response. Deleting elements.");
                    ResponseString = Regex.Replace(ResponseString, HtmlTagPattern, string.Empty);
                }

                // append only good characters to result
                int resultLength = ResponseString != null ? ResponseString.ToString()!.Length : 0;
                StringBuilder result = new(resultLength);
                if (resultLength != 0)
                {
                    foreach (char c in ResponseString!.ToString()!)
                    {
                        if (GOOD_CHARS.Contains(c))
                        {
                            result.Append(c);
                        }
                    }
                }
                return Task.FromResult(result != null ? result.ToString()! : "Response from the model is null or empty.");
            }
            catch (Exception ex)
            {
                _methodName = MethodBase.GetCurrentMethod()?.Name ?? "Unknown Method";
                _logger.LogCritical($"{_className}.{_methodName}: {ex.Message}");
            }

            return Task.FromResult(string.Empty);
        }

        public async Task<string> GenerateSpeechFile(string TextForSpeech, string Voice)
        {
            _methodName = MethodBase.GetCurrentMethod()?.Name ?? "Unknown Method";
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
                    client.Timeout = TimeSpan.FromSeconds(_settings.HttpTtsTimeout);
                    var content = new StringContent(TtsRequest, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(TtsEndpointUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string ContentDisposiion;
                        string DropFilename;
                        string WebAssetFilename;
                        //string LocalAssetFilename;

                        //get the content-disposition from the header
                        //Content-Disposition: attachment; filename="your_audio_file.wav"                     ;
                        ContentDisposiion = response.Content.Headers.GetValues("Content-Disposition").ToList()[0];

                        //build the filename from the drop location and from the header
                        //if the header is null throw an exception 
                        DropFilename = _settings.SpeechFileDropLocation!;
                        if (ContentDisposiion.ToString() is null)
                        {
                            ExceptionMessageString = string.Format("Content-Disposition header is null");
                            throw new Exception(ExceptionMessageString);
                        }

                        //content-disposition: attachment; filename="your_audio_file.wav"  
                        //split the string on the '=' character and get the second element in the array
                        var ContentDispositionArray = ContentDisposiion.Split("=");
                        if (ContentDispositionArray.Length < 2)
                        {
                            ExceptionMessageString = string.Format("Content-Disposition header is invalid (split array < 2): {0}", ContentDisposiion);
                            throw new Exception(ExceptionMessageString);
                        }

                        //get the filename and remove the " characters
                        DropFilename += ContentDispositionArray[1].ToString().Replace("\"", string.Empty);

                        //copy the speech file to the website assets location
                        WebAssetFilename = CopySpeechFileToAssets(DropFilename);
                        //LocalAssetFilename = _settings.SpeechFilePlaybackLocation! + WebAssetFilename;

                        return WebAssetFilename;
                    }
                    else
                    {
                        ExceptionMessageString = response.RequestMessage != null ? response.RequestMessage.ToString() : "RequestMessage is null";
                        throw new Exception(ExceptionMessageString);
                    }
                }
            }
            catch (Exception ex)
            {
                _methodName = MethodBase.GetCurrentMethod()?.Name ?? "Unknown Method";
                _logger.LogCritical($"{_className}.{_methodName}: {ex.Message}");
            }

            return string.Empty;
        }

        public string CopySpeechFileToAssets(string SourceFile)
        {
            //get the path..then append default filename to string
            string DestinationFile = _settings.SpeechFilePlaybackLocation!;
            DestinationFile += _settings.SpeechFilePlaybackName!;

            try
            {
                //check for null
                if (SourceFile == null)
                {
                    ExceptionMessageString = "Error: Source file is NULL";
                    throw new Exception(ExceptionMessageString);
                }

                // Check if the source file exists
                if (!File.Exists(SourceFile))
                {
                    ExceptionMessageString = string.Format("Error: Source file not found: {file}", SourceFile);
                    throw new Exception(ExceptionMessageString);
                }

                // Copy the file
                File.Copy(SourceFile, DestinationFile, true); // The 'true' parameter enables overwriting if the file exists
                _logger.LogInformation("File '{f1}' copied to '{f2}' successfully.", SourceFile, DestinationFile);

                //return just the audio filename to play back from the wwwroot/assets
                DestinationFile = _settings.SpeechFilePlaybackName!;

                return DestinationFile;
            }
            catch (Exception ex)
            {
                _methodName = MethodBase.GetCurrentMethod()?.Name ?? "Unknown Method";
                _logger.LogCritical($"{_className}.{_methodName}: {ex.Message}");
            }

            return string.Empty;
        }

        public async Task<bool> PlaySpeechFile()
        {
            try
            {
                string filePath = _settings.SpeechFilePlaybackLocation + _settings.SpeechFilePlaybackName;
                if ((filePath == null) || (!File.Exists(filePath)))
                {
                    ExceptionMessageString = $"Speech file not found at {(filePath != null ? filePath : "Filename is NULL")}";
                    _logger.LogInformation(ExceptionMessageString);
                    throw new Exception(ExceptionMessageString);
                }

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

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _methodName = MethodBase.GetCurrentMethod()?.Name ?? "Unknown Method";
                _logger.LogCritical($"{_className}.{_methodName}: {ex.Message}");
            }

            return await Task.FromResult(false);
        }

        public ResponseJsonObject? ExtractAndDeserialize(string plainText)
        {
            // Example: Assuming the JSON string is between "START_JSON" and "END_JSON"
            int startIndex = plainText.IndexOf("{");
            int endIndex = (plainText.IndexOf("}") - startIndex) + 1;

            if (startIndex == -1 || endIndex == -1 || startIndex >= endIndex)
            {
                ExceptionMessageString = "JSON boundaries not found in the response text. MODEL FAILS PROMPT";
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }

            string jsonString = plainText.Substring(startIndex, endIndex).Trim();
            if (jsonString == null || jsonString == String.Empty)
            {
                ExceptionMessageString = "Extracted JSON string is null or empty. MODEL FAILS PROMPT";
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }

            try
            {
                return JsonSerializer.Deserialize<ResponseJsonObject>(jsonString);
            }
            catch (JsonException ex)
            {
                _logger.LogError("JSON Deserialization error: {Message}", ex.Message);
                throw;
            }
        }
        #endregion

    }       //end class

}       //end namespace


