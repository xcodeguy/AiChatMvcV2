using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AiChatMvcV2.Models;
using AiChatMvcV2.Objects;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace AiChatMvcV2.Services;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationSettings _settings;
    private readonly ModelServices _callController;
    private readonly ResponseServices _responseService;

    public HomeController(IOptions<ApplicationSettings> settings,
                            ILogger<HomeController> logger,
                            ModelServices callController,
                            ResponseServices responseService)
    {
        _logger = logger;
        _callController = callController;
        _responseService = responseService;
        _settings = settings.Value;

        _logger.LogDebug(1, "NLog injected into HomeController");

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
        String ExceptionMessageString = string.Empty;

        //////////////////////////////////////////
        // API Round trip TRY/CATCH block
        //////////////////////////////////////////
        try
        {
            // call the api which calls the inference server to generate a response
            _logger.LogInformation("Calling API async for model {ModelName}", model);
            TextResponse = await _callController.GetModelResponseAsync
            (
                model,
                SystemContent,
                UserContent,
                NegativePrompt
            );

            // call the api which calls the inference server to summarize the response 
            // into a one or two word topic
            _logger.LogInformation("Calling API async for Topic summary");
            TextTopic = await _callController.GetModelResponseAsync(
                _settings.TopicSummaryModelName,
                _settings.TopicSummaryPrompt,
                TextResponse,
                _settings.TopicSummaryNegativePrompt
            );

            //call the api which calls the inference server to generate a speech file from the topic response
            _logger.LogInformation("Calling API async to generate speech file for topic {Topic}", TextTopic);
            AudioFilename = await _responseService.GenerateSpeechFile(TextTopic, TtsVoice);

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
            ExceptionMessageString = String.Format("Exception in HomeController::QueryModelForResponse() {0}, {1} {2}, {3}", e.Message.ToString(), model, UserContent, NegativePrompt);
            _logger.LogCritical(ExceptionMessageString);

            //don't throw the exception. this is the last method in the call chain
            //and we always return an Http OK to the browser. we use the finally block
            //to assemble the response with all meta-data including any exceptions from
            //the backend services
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
                WordCount = _responseService.GetWordCount(TextResponse),
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
            bool success = _callController.InsertResponse(Item);
            if (!success)
            {
                ExceptionMessageString = String.Format("Error inserting response into database: {0}", Item);
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception("Error inserting response into database.");
            }
        }
        catch (Exception e)
        {
            ExceptionMessageString = String.Format("Exception in HomeController::QueryModelForResponse() {0}, {1} {2}, {3}", model, UserContent, NegativePrompt, e.Message.ToString());
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
    public async Task<IActionResult> PlaySpeechFile()
    {
        try
        {
            //call the service to play the speech file
            //the service returns true or false
            bool result = await _responseService.PlaySpeechFile();
            if(!result)
            {
                string ExceptionMessageString = String.Format("Error in HomeController::PlaySpeechFile() playing speech file.");
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }
        }
        catch (Exception e)
        {
            string ExceptionMessageString = String.Format("Exception in HomeController::PlaySpeechFile() {0}", e.Message.ToString());
            _logger.LogCritical(ExceptionMessageString);
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
}
