using System.Text.Json;
using AiChatMvcV2.Contracts;
using AiChatMvcV2.Objects;
using Microsoft.Extensions.Options;
using AiChatMvcV2.Classes;

namespace AiChatMvcV2.Controllers
{
    public class ResponseController : IResponseController
    {
        private readonly ILogger<CallController> _logger;
        private readonly ApplicationSettings _settings;

        public ResponseController(IOptions<ApplicationSettings> settings, ILogger<CallController> logger)
        {
            _logger = logger;
            _settings = settings.Value;
            _logger.LogInformation("ResponseController class initialized.");
        }
        public Task<string> ParseJsonForObject(string json)
        {

            string JsonString = json;

            List<char> NO_CHARS = new List<char>() { '{', '}', '\\' };
            List<char> GOOD_CHARS = new List<char>() {'a','b','c','d','e','f','g','h','i','j','k','l','m','o','p','q','r','s','t','u','v','x','y','z',
                                                    'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
                                                    '0','1','2','3','4','5','6','7','8','9',
                                                    ' ',',',':','[',']','"','<','>','?','/','.','!','@','#','$','%','^','&','*','(',')','-','_','+','=','|','\\'};

            Dictionary<string, object> item = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonString)!;
            var ReturnString = item!["response"];
            String s = String.Format("{0}", ReturnString);
            ReturnString = ReturnString.ToString()!.Replace("\n", String.Empty);
            ReturnString = ReturnString.ToString()!.Replace("\"","");
            Console.WriteLine(ReturnString);


            return Task.Run(ReturnString.ToString)!;
        }
    }

}


