///////////////////////////////////////////////
//    David Ferrell
//    Copyright (C) 2025, Xcodeguy Software
//    Class for calling LLM API's in OLlama
///////////////////////////////////////////////
using AiChatMvcV2.Contracts;
using System.Text;
using AiChatMvcV2.Objects;
using Microsoft.Extensions.Options;

namespace AiChatMvcV2.Classes
{
    public class CallController : ICallController
    {
        private readonly ILogger<CallController> _logger;
        private readonly ApplicationSettings _settings;
        private const float temperature = 0.8f;     //0.8
        private const int num_ctx = 2048;           //2048
        private const int num_predict = -1;         //-1

        public CallController(IOptions<ApplicationSettings> settings, ILogger<CallController> logger)
        {
            _logger = logger;
            _settings = settings.Value;
            _logger.LogDebug(1, "CallApi class initialized. Injections are happy");
        }

        public async Task<string> CallApiAsync(string Model, string SystemContent, string UserContent, string NegativePrompt)
        {
            string? url = _settings.Url;
            string data;
            UserContent = SystemContent + " " + UserContent + " " + NegativePrompt;
            var options = "\"options\" : {{\"temperature\" : " + temperature + ", \"num_ctx\" : " + num_ctx + ", \"num_predict\" : " + num_predict + "}}";
            //data = "{\"model\": \"" + Model + "\",\"messages\":[{\"role\":\"system\",\"content\":\"" + SystemContent + "\"},{\"role\": \"user\", \"content\": \"" + UserContent + "\"}],\"stream\": false}";
            /*
                EXAMPLE:
                {
                    "model": "gemma3",
                    "prompt": "Simulate a Model UN session regarding global nutrition.",
                    "stream": false,
                    "options": {
                        "temperature": 2,
                        "num_ctx": 2048,
                        "num_predict": -1
                    }
                }
            */
            data = String.Format("{{\"model\": \"{0}\", \"prompt\": \"{1}\", \"stream\": false, " + options + "}}", Model, UserContent);
            _logger.LogInformation("Built data string");

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(300); //5 min timeout

                _logger.LogInformation("Calling AI model API");
                var content = new StringContent(data, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("AI model response success");
                    var text = response.Content.ReadAsStreamAsync();
                    _logger.LogInformation("Returning contents of {model}:{prompt}:{text}", Model, UserContent, text);
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    String ExceptionMessageString = String.Format("Exception in CallController::CallApiAsync() {0} {1}, {2}\nException: {3}", Model, UserContent, NegativePrompt, response.RequestMessage);
                    _logger.LogCritical(ExceptionMessageString);
                    throw new Exception(ExceptionMessageString);
                }
            }
        }
    }       //end class

}       //end namespace