using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AiChatMvcV2.Models;
using AiChatMvcV2.Objects;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Linq;
using System.Text.Json;

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
                                                            string Prompt)
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
        string? ResponseText = string.Empty;
        string TopicText = string.Empty;
        string AudioFilename = string.Empty;
        string ExceptionMessageString = string.Empty;
        int Score = 10;
        List<string> ScoreReasons = new();

        try
        {
            // read the prompt from the prompt.md file
            var StructuredPrompt = ReadPromptFile(_settings.PromptFilename);
            if (StructuredPrompt == null)
            {
                ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                $"The structured prompt for the response is missing or empty for model {model}.");
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }

            // remove carriage returns, line feeds and backslashes
            StructuredPrompt = _ResponseService.RemoveFormatStrings(StructuredPrompt);

            // do we have any last response text to insert into the structured prompt?
            // i.e. <LastResponse>...</LastResponse>
            if ((Prompt != null) && (Prompt != string.Empty))
            {
                StructuredPrompt = StructuredPrompt.Replace("</LastResponse>", Prompt + "</LastResponse>");
                _logger.LogInformation($"Structured prompt with last response text is:\n\n{StructuredPrompt}");
            }

            // call the api to get a response from the model
            _logger.LogInformation("Calling API async for model {ModelName}", model);
            ResponseText = await _ModelService.GetModelResponseAsync
            (
                model,
                StructuredPrompt
            );
            _logger.LogInformation($"The model response is:\n\n{ResponseText}");

            // check for a null or empty response from the API->Inference Server->Model call
            if (ResponseText == null || ResponseText == String.Empty)
            {
                ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                $"The response is null or empty for model {model}.");
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }

            //extract the response and topic from the JSON object embedded in the response text
            ResponseJsonObjectFlat? parsedResponse = _ResponseService.ExtractAndDeserialize(ResponseText);
            _logger.LogInformation($"Deserialized response JSON object from model response: {parsedResponse}");
            ResponseText = parsedResponse?.response ?? String.Empty;
            TopicText = parsedResponse?.topic ?? String.Empty;
            Score = parsedResponse?.score ?? 10;
            ScoreReasons = parsedResponse?.reasons ?? [];

            _logger.LogInformation("Extracted response: {ResponseText}", ResponseText);
            _logger.LogInformation("Extracted topic: {TopicText}", TopicText);
            _logger.LogInformation("Extracted score: {Score}", Score);
            foreach (var itm in parsedResponse?.reasons ?? [])
            {
                _logger.LogInformation("Score reason: {Reason}", itm);
            }

            if (TopicText == null || TopicText == String.Empty)
            {
                ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                $"Cannot generate speech file because the topic text is null or empty for model {model}.");
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }

            // call the api to generate a speech file from the TopicText summary.
            // Append a '.' period to the end of the topic before sending it
            // for audio render. This helps the model produce a more natural
            // pronunciation of the one or two word topic.
            if (_settings.PlayAudioFile == false)
            {
                _logger.LogInformation("Skipping speech file generation as per application settings.");
                AudioFilename = "N/A";
                // we still fall into the finall{} block to build out the response item
                // and return to the browser
                return Ok(ViewModel);
            }

            _logger.LogInformation("Calling API async to generate speech file for topic {Topic}.", TopicText);
            AudioFilename = await _ResponseService.GenerateSpeechFile(TopicText + ".", TtsVoice);

            //check if the audio/speech file exists, this is redundant because the file
            //checks are performed in the service layer
            if (!System.IO.File.Exists(local_path_to_assets_folder + AudioFilename))
            {
                ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                $"Speech/Audio file not found: {AudioFilename}");
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }
            else
            {
                FileInfo fileInfo = new FileInfo(local_path_to_assets_folder + AudioFilename);
                fileSizeInBytes = fileInfo.Length;
                _logger.LogInformation("Audio file generated: {file}, size: {size} bytes", AudioFilename, fileSizeInBytes);
            }
        }
        catch (Exception ex)
        {
            //don't throw the exception. this is the last method in the call chain
            //and we always return an Http OK to the browser. we use the finally block
            //to assemble the response with all meta-data including any exceptions from
            //the backend services
            ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                ex.Message.ToString());
            _logger.LogCritical(ExceptionMessageString);
            throw new Exception(ExceptionMessageString);
        }
        finally
        {
            // build out the response item object
            Item = new ResponseItem
            {
                TimeStamp = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"),
                Response = ResponseText,
                Model = model,
                Topic = TopicText.ToString(),
                Prompt = Prompt,
                NegativePrompt = String.Empty,
                Active = 1,
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"),
                AudioFilename = _settings.SpeechFileUrlLocation + AudioFilename,
                AudioFileSize = fileSizeInBytes.ToString(),
                ResponseTime = String.Format("{0:00}:{1:00}:{2:00}", TimeSpent.Hours, TimeSpent.Minutes, TimeSpent.Seconds),
                WordCount = _ResponseService.GetWordCount(ResponseText),
                Exceptions = ExceptionMessageString,
                TtsVoice = TtsVoice,
                Score = Score,
                ScoreReasons = ScoreReasons
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
                ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                $"Error inserting response into database: {Item.Model} {Item.Response} {Item.Topic}");
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }
        }
        catch (Exception ex)
        {
            ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                ex.Message.ToString());
            _logger.LogCritical(ExceptionMessageString);
            throw new Exception(ExceptionMessageString);
        }
        finally
        {
            //calculate elapsed time
            TimeSpent = DateTime.Now - StartTime;

            // since this finally block will be executed everytime it is
            // appropriate to set these values here
            Item.ResponseTime = String.Format("{0:00}:{1:00}:{2:00}", TimeSpent.Hours, TimeSpent.Minutes, TimeSpent.Seconds);
            Item.Exceptions = ExceptionMessageString;
            ViewModel.ResponseItemList.Add(Item);
        }

        // return an HTTP OK with the loaded view model
        return Ok(ViewModel);
    }

    private string ReadPromptFile(string PromptFilename)
    {
        try
        {
            string filePath = Directory.GetCurrentDirectory() + "/wwwroot/assets/" + PromptFilename;
            string text = System.IO.File.ReadAllText(filePath);

            if (text == null || text == String.Empty)
            {
                ExceptionMessageString = $"The prompt file {PromptFilename} is missing or empty.";
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }
            return text;
        }
        catch (Exception ex)
        {
            ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                ex.Message.ToString());
            _logger.LogCritical(ExceptionMessageString);
            throw new Exception(ExceptionMessageString);
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
        catch (Exception ex)
        {
            ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                ex.Message.ToString());
            _logger.LogCritical(ExceptionMessageString);
            throw new Exception(ExceptionMessageString);
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
        catch (Exception ex)
        {
            ExceptionMessageString = GetClassAndMethodName(_className,
                                                                _methodName,
                                                                ex.Message.ToString());
            _logger.LogCritical(ExceptionMessageString);
            throw new Exception(ExceptionMessageString);
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
