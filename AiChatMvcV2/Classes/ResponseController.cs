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
            List<char> GOOD_CHARS = new List<char>() {'a','b','c','d','e','f','g','h','i','j','k','l','m','o','p','q','r','s','t','u','v','x','y','z',
                                                    'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
                                                    '0','1','2','3','4','5','6','7','8','9',
                                                    ' ',',',':','[',']','"','<','>','?','/','.','!','@','#','$','%','^','&','*','(',')','-','_','+','=','|','\\'};

            Dictionary<string, object> item = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonString)!;
            var ReturnString = item!["response"];
            String s = String.Format("{0}", ReturnString);
            ReturnString = ReturnString.ToString()!.Replace("\n", String.Empty);
            ReturnString = ReturnString.ToString()!.Replace("\"", "");
            Console.WriteLine(ReturnString);


            return Task.Run(ReturnString.ToString)!;
        }
    }

}


