using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AiChatMvcV2.Models;
using AiChatMvcV2.Objects;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Linq;

namespace AiChatMvcV2.Services;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationSettings _settings;
    private readonly ModelServices _ModelService;
    private readonly ResponseServices _ResponseService;
    private string ExceptionMessageString = String.Empty;
    Type _classType;
    string _className = string.Empty;
    string _methodName = string.Empty;
    Func<string, string, string, string> GetClassAndMethodName;

    public HomeController(IOptions<ApplicationSettings> settings,
                            ILogger<HomeController> logger,
                            ModelServices callController,
                            ResponseServices responseService)
    {
        _logger = logger;
        _ModelService = callController;
        _ResponseService = responseService;
        _settings = settings.Value;
        _classType = this.GetType();

        GetClassAndMethodName = (cls, mth, exp) => $"{_className}.{MethodBase.GetCurrentMethod()?.Name ?? "Unknown Method"}: {exp}";

    }

    [HttpPost]
    public async Task<IActionResult> QueryModelForResponse(string model,
                                                string SystemContent,
                                                string UserContent,
                                                string NegativePrompt)
    {
        DateTime StartTime = DateTime.Now;
        HomeViewModel ViewModel = new HomeViewModel();
        ViewModel.ResponseItemList = new List<ResponseItem>();
        TimeSpan TimeSpent = new TimeSpan();
        ResponseItem Item;
        List<string> TtsVoices = _settings.TtsVoices;
        Random rand = new Random();
        string TtsVoice = TtsVoices[rand.Next(TtsVoices.Count)];
        long fileSizeInBytes = 0;
        string local_path_to_assets_folder = _settings.SpeechFilePlaybackLocation!;
        string TextResponse = string.Empty;
        string TextTopic = string.Empty;
        string AudioFilename = string.Empty;
        string ExceptionMessageString = string.Empty;

        //////////////////////////////////////////
        // API Round trip TRY/CATCH block
        //////////////////////////////////////////
        try
        {
            // call the api which calls the inference server to generate a response
            _logger.LogInformation("Calling API async for model {ModelName}", model);
            TextResponse = await _ModelService.GetModelResponseAsync
            (
                model,
                SystemContent,
                UserContent,
                NegativePrompt
            );

            // Best summary models for summary prompt so far:
            // deepseek-r1, gemma3, wizard-vicuna
            // call the api which calls the inference server to summarize the response 
            // into a one or two word topic
            var HardModel = model;
            if (!_settings.AllowModelToSummarizeOwnResponse)
            {
                HardModel = _settings.TopicSummaryModelName;
            }
            _logger.LogInformation("Calling API async for Topic summary using {model}", model);
            TextTopic = await _ModelService.GetModelResponseAsync(
                HardModel,
                _settings.TopicSummaryPrompt,
                TextResponse,
                _settings.TopicSummaryNegativePrompt
            );

            //truncate the topic text if the model went off on a tangent response
            if (TextTopic.Length > 45)
            {
                _logger.LogInformation($"The topic length is {TextTopic.Length} chars in length. Truncating to 45.");
                TextTopic = TextTopic.Substring(1, 45);
            }

            //call the api which calls the inference server to generate a speech file from the topic response
            // append a '.' period to the end of the topic before sending it
            // for audio render. This helps the model produce a more natural
            // pronunciation of the one or tqo word topic.
            _logger.LogInformation("Calling API async to generate speech file for topic {Topic}.", TextTopic);
            AudioFilename = await _ResponseService.GenerateSpeechFile(TextTopic, TtsVoice);

            //check if the audio/speech file exists, this is redundant because the file
            //checks are performed in the service layer
            if (!System.IO.File.Exists(local_path_to_assets_folder + AudioFilename))
            {
                ExceptionMessageString = String.Format("Speech/Audio file not found: {0}", AudioFilename);
                _logger.LogInformation(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }
            else
            {
                FileInfo fileInfo = new FileInfo(local_path_to_assets_folder + AudioFilename);
                fileSizeInBytes = fileInfo.Length;
                _logger.LogInformation("Audio file generated: {file}, size: {size} bytes", AudioFilename, fileSizeInBytes);
            }
        }
        catch (Exception e)
        {
            //don't throw the exception. this is the last method in the call chain
            //and we always return an Http OK to the browser. we use the finally block
            //to assemble the response with all meta-data including any exceptions from
            //the backend services
            ExceptionMessageString = e.Message.ToString();
            _logger.LogCritical(ExceptionMessageString);
        }
        finally
        {
            // build out the response object
            Item = new ResponseItem
            {
                TimeStamp = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"),
                Response = TextResponse,
                Model = model,
                Topic = TextTopic.ToString(),
                Prompt = SystemContent + UserContent,
                NegativePrompt = NegativePrompt,
                Active = 1,
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"),
                AudioFilename = _settings.SpeechFileUrlLocation + AudioFilename,
                AudioFileSize = fileSizeInBytes.ToString(),
                ResponseTime = String.Format("{0:00}:{1:00}:{2:00}", TimeSpent.Hours, TimeSpent.Minutes, TimeSpent.Seconds),
                WordCount = _ResponseService.GetWordCount(TextResponse),
                Exceptions = ExceptionMessageString,
                TtsVoice = TtsVoice
            };
        }

        //////////////////////////////////////////
        // INSERT into databased TRY/CATCH block
        //////////////////////////////////////////
        try
        {
            //insert the response and meta-data into the database
            bool success = _ModelService.InsertResponse(Item);
            if (!success)
            {
                ExceptionMessageString = String.Format("Error inserting response into database: {0}", Item);
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception("Error inserting response into database.");
            }
        }
        catch (Exception e)
        {
            ExceptionMessageString = e.Message.ToString();
            _logger.LogCritical(ExceptionMessageString);
        }
        finally
        {

        }

        //calculate elapsed time
        TimeSpent = DateTime.Now - StartTime;

        //assign the last two properties and return an HTTP OK with the loaded view model
        Item.ResponseTime = String.Format("{0:00}:{1:00}:{2:00}", TimeSpent.Hours, TimeSpent.Minutes, TimeSpent.Seconds);
        Item.Exceptions = ExceptionMessageString;
        ViewModel.ResponseItemList.Add(Item);
        return Ok(ViewModel);
    }

    [HttpPost]
    public IActionResult GetStartupPrompt()
    {
        try
        {
            string filePath = "prompts.md";

            string text = System.IO.File.ReadAllText(filePath);
            return Ok(text);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e.Message.ToString());
            throw;
        }
    }

    [HttpPost]
    public IActionResult GetNegativePrompt()
    {
        try
        {
            // Call the service to get the startup prompt
            string NegativePrompt = _settings.NegativePrompt;
            return Ok(NegativePrompt);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e.Message.ToString());
            throw;
        }
    }

    [HttpPost]
    public IActionResult GetModelNames()
    {
        try
        {
            List<string> ModelNameList = _settings.LLMs;
            return Ok(ModelNameList);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e.Message.ToString());
            throw;
        }
    }

    [HttpPost]
    public async Task<IActionResult> PlaySpeechFile()
    {
        try
        {
            //call the service to play the speech file
            //the service returns true or false
            bool result = await _ResponseService.PlaySpeechFile();
            if (!result)
            {
                ExceptionMessageString = "Error playing speech file";
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e.Message.ToString());
            throw;
        }

        return Ok();
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();

    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public string OpenPromptFile()
    {
        string filePath = "prompts.md";

        try
        {
            string text = System.IO.File.ReadAllText(filePath);
            return text;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening prompt file: {ex.Message}");
            return null;
        }
    }

}
