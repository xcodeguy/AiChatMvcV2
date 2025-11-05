using System.Text.Json;
using AiChatMvcV2.Contracts;
using AiChatMvcV2.Objects;
using Microsoft.Extensions.Options;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;
using AiChatMvcV2.Models;
using System.Timers;

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
        Func<string, string, string, string> GetClassAndMethodName;
        #endregion

        #region methods
        public ResponseServices(IOptions<ApplicationSettings> settings, ILogger<ResponseServices> logger)
        {
            _logger = logger;
            _settings = settings.Value;
            _classType = this.GetType();
            _className = _classType.Name.ToString();
            _methodName = string.Empty;

            GetClassAndMethodName = (cls, mth, exp) => $"{_className}.{MethodBase.GetCurrentMethod()?.Name ?? "Unknown Method"}: {exp}";

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
                ExceptionMessageString = GetClassAndMethodName(_className, _methodName, ex.Message);
                _logger.LogCritical(ExceptionMessageString);
                throw;
            }
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
                int resultLength = ResponseString != null ? ResponseString.Length : 0;
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
                return Task.FromResult(result.ToString()!);
            }
            catch (Exception ex)
            {
                ExceptionMessageString = GetClassAndMethodName(_className, _methodName, ex.Message);
                _logger.LogCritical(ExceptionMessageString);
                throw;
            }
        }

        public async Task<string> GenerateSpeechFile(string TextForSpeech, string Voice)
        {
            _methodName = MethodBase.GetCurrentMethod()?.Name ?? "Unknown Method";
            string TtsEndpointUrl = _settings.TTSApiEndpointUrl;
            string ModelName = _settings.TTSModelName;
            string TtsRequest;

            try
            {
                TtsRequest = $@"{{""model"": ""{ModelName}"", 
                                    ""input"": ""{TextForSpeech}"", 
                                    ""voice"": ""{Voice}"", 
                                    ""response_format"" : ""{_settings.SpeechFileFormat}"", 
                                    ""speed"": ""{_settings.PlaybackSpeed}""}}";

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

                        //get the content-disposition from the header
                        //Content-Disposition: attachment; filename="your_audio_file.wav"                     ;
                        if (!response.Content.Headers.Contains("Content-Disposition") ||
                            !response.Content.Headers.GetValues("Content-Disposition").Any())
                        {
                            ExceptionMessageString = "Content-Disposition header is missing or empty";
                            throw new Exception(ExceptionMessageString);
                        }
                        ContentDisposiion = response.Content.Headers.GetValues("Content-Disposition").First();

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
                        DropFilename += ContentDispositionArray[1].Replace("\"", string.Empty);

                        //copy the speech file to the website assets location
                        WebAssetFilename = CopySpeechFileToAssets(DropFilename);

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
                ExceptionMessageString = GetClassAndMethodName(_className, _methodName, ex.Message);
                _logger.LogCritical(ExceptionMessageString);
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
                //check for null
                if (SourceFile == null)
                {
                    ExceptionMessageString = GetClassAndMethodName(_className,
                                                                    _methodName,
                                                                    $"Error: Source file is NULL");
                    _logger.LogCritical(ExceptionMessageString);
                    throw new Exception(ExceptionMessageString);
                }

                // Check if the source file exists
                if (!File.Exists(SourceFile))
                {
                    ExceptionMessageString = GetClassAndMethodName(_className,
                                                                    _methodName,
                                                                    $"Error: Source file not found: {SourceFile}");
                    _logger.LogCritical(ExceptionMessageString);
                    throw new Exception(ExceptionMessageString);
                }

                // Copy the file
                File.Copy(SourceFile, DestinationFile, true); // The 'true' parameter enables overwriting if the file exists
                _logger.LogInformation("File '{SourceFile}' copied to '{DestinationFile}' successfully.", SourceFile, DestinationFile);

                //return just the audio filename to play back from the wwwroot/assets
                return _settings.SpeechFilePlaybackName!;
            }
            catch (Exception ex)
            {
                ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                ex.Message);
                _logger.LogCritical(ExceptionMessageString);
                throw;
            }
        }

        public async Task<bool> PlaySpeechFile()
        {
            try
            {
                string filePath = _settings.SpeechFilePlaybackLocation + _settings.SpeechFilePlaybackName;
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                $"Speech file not found at {(string.IsNullOrWhiteSpace(filePath) ? "Filename is empty or whitespace" : filePath)}");
                    _logger.LogCritical(ExceptionMessageString);
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
                ExceptionMessageString = GetClassAndMethodName(_className, _methodName, ex.Message);
                _logger.LogCritical(ExceptionMessageString);
                throw;
            }

        }

        public ResponseJsonObjectFlat? ExtractAndDeserialize(string plainText)
        {
            Dictionary<string, object> jsonCandidate = [];
            ResponseJsonObjectFlat responseFlat = new()
            {
                reasons = [],
                score = _settings.MaxScore
            };

            int startIndex = plainText.IndexOf('{');
            int endIndex = plainText.IndexOf('}') - startIndex + 1;

            if (startIndex == -1 || endIndex == -1 || startIndex >= endIndex)
            {
                ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                $"JSON boundaries not found in the response text.");
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }

            // Extract a substring that is assumed to be JSON, but may not always be valid JSON.
            string extractedJsonCandidate = plainText.Substring(startIndex, endIndex).Trim();


            if (string.IsNullOrEmpty(extractedJsonCandidate))
            {
                ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                $": Extracted JSON candidate string is null or empty.");
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }

            try
            {
                // Attempt to deserialize the extracted JSON candidate
                _logger.LogInformation($"Attempting to deserialize extracted JSON candidate.");
                jsonCandidate = JsonSerializer.Deserialize<Dictionary<string, object>>(extractedJsonCandidate)!;

                /// If deserialization is successful, determine the typeof the response and topic properties
                if (jsonCandidate != null)
                {
                    // Handle missing response property
                    if (!jsonCandidate.ContainsKey("response"))
                    {
                        responseFlat.score -= 1;
                        responseFlat.reasons.Add("Deducting point for: Missing response property");
                    }

                    // Handle missing topic property
                    if (!jsonCandidate.ContainsKey("topic"))
                    {
                        responseFlat.score -= 1;
                        responseFlat.reasons.Add("Deducting point for: Missing topic property");
                    }

                    // Handle null response property
                    if (jsonCandidate.ContainsKey("response")
                                        && jsonCandidate["response"]
                                        is JsonElement jsonElementResponseNull
                                        && jsonElementResponseNull.ValueKind == JsonValueKind.Null)
                    {
                        responseFlat.score -= 1;
                        responseFlat.reasons.Add("Deducting point for: Null response property");
                    }

                    // Handle null topic property  
                    if (jsonCandidate.ContainsKey("topic")
                                        && jsonCandidate["topic"]
                                        is JsonElement jsonElementTopicStringNull
                                        && jsonElementTopicStringNull.ValueKind == JsonValueKind.Null)
                    {
                        responseFlat.score -= 1;
                        responseFlat.reasons.Add("Deducting point for: Null topic property");
                    }

                    // Handle response as string
                    if (jsonCandidate.ContainsKey("response")
                                        && jsonCandidate["response"]
                                        is JsonElement jsonElementResponseString
                                        && jsonElementResponseString.ValueKind == JsonValueKind.String)
                    {
                        responseFlat.response = jsonElementResponseString.ToString()!;
                    }

                    // Handle topic as string
                    if (jsonCandidate.ContainsKey("topic")
                                        && jsonCandidate["topic"]
                                        is JsonElement jsonElementTopicString
                                        && jsonElementTopicString.ValueKind == JsonValueKind.String)
                    {
                        responseFlat.topic = jsonElementTopicString.ToString()!;
                    }

                    // Handle response as array
                    if (jsonCandidate.ContainsKey("response")
                                        && jsonCandidate["response"]
                                        is JsonElement jsonElementResponseArray
                                        && jsonElementResponseArray.ValueKind == JsonValueKind.Array)
                    {
                        var responseArray = jsonElementResponseArray.EnumerateArray().Select(e => e.GetString()).Where(s => s != null).ToArray();
                        responseFlat.response = string.Join(" ", responseArray);
                        responseFlat.score -= 1; //reduce grade by 1 for response being an array
                        responseFlat.reasons.Add("Deducting point for: Response property is an array");
                    }

                    // Handle topic as array
                    if (jsonCandidate.ContainsKey("topic")
                                        && jsonCandidate["topic"]
                                        is JsonElement jsonElementTopicArray
                                        && jsonElementTopicArray.ValueKind == JsonValueKind.Array)
                    {
                        var topicArray = jsonElementTopicArray.EnumerateArray().Select(e => e.GetString()).Where(s => s != null).ToArray();
                        responseFlat.topic = string.Join(" ", topicArray);
                        responseFlat.score -= 1; //reduce grade by 1 for topic being an array
                        responseFlat.reasons.Add("Deducting point for: Topic property is an array");
                    }
                }
                else
                {
                    // jsonCandidate is null, set grade to 0
                    responseFlat.response = string.Empty;
                    responseFlat.topic = string.Empty;
                    responseFlat.score = 0;
                    responseFlat.reasons.Add("Deducting point for: Deserialized object is null");
                }
                return responseFlat;
            }
            catch (Exception ex)
            {
                // On exception, set grade to 0
                responseFlat.response = string.Empty;
                responseFlat.topic = string.Empty;
                responseFlat.score = 0;

                if (ex is JsonException jsonEx)
                {
                    // Log JSON-specific errors
                    ExceptionMessageString = GetClassAndMethodName(_className,
                                                                    _methodName,
                                                                    $"JSON deserialization failed. Error: {jsonEx.Message}");
                    _logger.LogCritical(ExceptionMessageString);
                    responseFlat.reasons.Add("Deducting 0 points: JSON deserialization failed");
                }
                else
                {
                    // Log other types of exceptions
                    ExceptionMessageString = GetClassAndMethodName(_className,
                                                                    _methodName,
                                                                    $"An error occurred during JSON deserialization: {ex.Message}");
                    _logger.LogCritical(ExceptionMessageString);
                    responseFlat.reasons.Add("Deducting 0 points: General exception during deserialization");
                }
            }
            finally
            {
                _logger.LogInformation($"Deserialization attempt completed for extracted JSON candidate.");
            }

            return responseFlat;
        }

        public string RemoveFormatStrings(string text)
        {
            // remove carriage returns, line feeds and backslashes
            text = text.ToString()!.Replace("\r", string.Empty);
            text = text.ToString()!.Replace("\n", string.Empty);
            text = text.ToString()!.Replace("\"", string.Empty);
            text = text.ToString()!.Replace("\\", string.Empty);
            return text;
        }
        #endregion

    }       //end class

}       //end namespace


