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
        string ResponseText = string.Empty;
        string SummaryText = string.Empty;
        string SanitizedResponseText = string.Empty;
        string AudioFilename = string.Empty;
        string ExceptionMessageString = string.Empty;

        //////////////////////////////////////////
        // API Round trip TRY/CATCH block
        //////////////////////////////////////////
        try
        {
            //////////////////////////////////////////////////////////////////
            // GET MODEL RESPONSE
            //////////////////////////////////////////////////////////////////
            //read the prompt from the prompt.md file
            //remove any carriage returns because this causes
            //an exception somewhere in the ollama api
            var StructuredPrompt = ReadPromptFile(_settings.PromptFilename);
            if (StructuredPrompt == null)
            {
                ExceptionMessageString = $"The structured prompt for the response is missing or empty for model {model}.";
                throw new Exception(ExceptionMessageString);
            }

            //remove carriage returns
            StructuredPrompt = StructuredPrompt.ToString()!.Replace("\n", string.Empty);

            // Do we have prompt text to insert into the structured prompt?
            if ((Prompt != null) && (Prompt != string.Empty))
            {
                //////////////////////////////////////////////////////////////////
                // SANITIZE MODEL RESPONSE
                //////////////////////////////////////////////////////////////////
                // we santize the response here. we do this by sending it
                // back to the AI model and ask it to clean it up by
                // performing the following prompt
                var SanitizerPrompt = ReadPromptFile(_settings.SanitizerPromptFilename);
                if (SanitizerPrompt == null)
                {
                    ExceptionMessageString = $"The sanitizer prompt for the response is missing or empty for model {model}.";
                    throw new Exception(ExceptionMessageString);
                }

                // remove carriage returns and drop in the prompt (response)
                // from the last model
                SanitizerPrompt = SanitizerPrompt.ToString()!.Replace("\n", string.Empty);
                SanitizerPrompt = SanitizerPrompt.ToString()!.Replace("\"", string.Empty);
                SanitizerPrompt = SanitizerPrompt.ToString()!.Replace("\\", string.Empty);

                SanitizerPrompt = SanitizerPrompt.Replace("</target>", Prompt + "</target>");
                _logger.LogInformation($"Sanitizer prompt with response text from last model is:\n\n{SanitizerPrompt}");

                _logger.LogInformation("Calling API async for sanitizer {SanitizerModelName} on model response from {ModelName}", _settings.SanitizerModelName, model);
                SanitizedResponseText = await _ModelService.GetModelResponseAsync
                (
                    _settings.SanitizerModelName,
                    SanitizerPrompt
                );

                _logger.LogInformation($"Sanitized response text for the structured prompt is:\n\n{SanitizedResponseText}");
                StructuredPrompt = StructuredPrompt.Replace("</other>", SanitizedResponseText + "</other>");
                _logger.LogInformation($"Structured prompt with sanitized text is:\n\n{StructuredPrompt}");
                //////////////////////////////////////////////////////////////////
            }

            // call the api which calls the inference server to generate a response
            _logger.LogInformation("Calling API async for model {ModelName}", model);
            ResponseText = await _ModelService.GetModelResponseAsync
            (
                model,
                StructuredPrompt
            );
            _logger.LogInformation($"The final response is:\n\n{ResponseText}");
            //////////////////////////////////////////////////////////////////


            //////////////////////////////////////////////////////////////////
            // GET MODEL RESPONSE FOR SUMMARY
            //////////////////////////////////////////////////////////////////
            // Best summary models for summary prompt so far:
            // deepseek-r1, gemma3, wizard-vicuna
            // call the api which calls the inference server to summarize the response 
            // into a one or two word topic

            //allow the model to summarize it's own response by default
            var ResponseSummaryModelName = model;

            //if the model is not allowed to summarize it's own response
            //use the default summary model name
            if (!_settings.AllowModelToSummarizeOwnResponse)
            {
                ResponseSummaryModelName = _settings.TopicSummaryModelName;
            }

            //get the response summary prompt and make sure it's valid
            var ResponseSummaryPrompt = ReadPromptFile(_settings.SummaryPromptFilename);
            if (ResponseSummaryPrompt == null)
            {
                ExceptionMessageString = $"The prompt for the response summary is missing or empty for model {model}.";
                throw new Exception(ExceptionMessageString);
            }

            //remove carriage returns and drop in the response text to summarize
            ResponseSummaryPrompt = ResponseSummaryPrompt.ToString()!.Replace("\n", string.Empty);
            ResponseSummaryPrompt = ResponseSummaryPrompt.Replace("</target>", ResponseText + "</target>");

            _logger.LogInformation("Calling API async for response summary using {model}", ResponseSummaryModelName);
            SummaryText = await _ModelService.GetModelResponseAsync(
                ResponseSummaryModelName,
                ResponseSummaryPrompt.ToString()!
            );
            //////////////////////////////////////////////////////////////////


            //truncate the topic text if the model went off on a tangent response
            if (SummaryText.Length > 45)
            {
                _logger.LogInformation($"The summarized topic length is {SummaryText.Length} chars in length. Truncating to 45.");
                SummaryText = SummaryText.Substring(1, 45);
            }

            // call the api which calls the inference server to generate a speech file from the response
            // summary. Append a '.' period to the end of the topic before sending it
            // for audio render. This helps the model produce a more natural
            // pronunciation of the one or two word summary.
            _logger.LogInformation("Calling API async to generate speech file for topic {Topic}.", SummaryText);
            AudioFilename = await _ResponseService.GenerateSpeechFile(SummaryText, TtsVoice);

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
                Response = ResponseText,
                Model = model,
                Topic = SummaryText.ToString(),
                Prompt = Prompt,
                NegativePrompt = String.Empty,
                Active = 1,
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"),
                AudioFilename = _settings.SpeechFileUrlLocation + AudioFilename,
                AudioFileSize = fileSizeInBytes.ToString(),
                ResponseTime = String.Format("{0:00}:{1:00}:{2:00}", TimeSpent.Hours, TimeSpent.Minutes, TimeSpent.Seconds),
                WordCount = _ResponseService.GetWordCount(ResponseText),
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

    private string ReadPromptFile(string PromptFilename)
    {
        try
        {
            string filePath = Directory.GetCurrentDirectory() + "/wwwroot/assets/" + PromptFilename;
            string text = System.IO.File.ReadAllText(filePath);
            return text;
        }
        catch (Exception e)
        {
            _logger.LogCritical(e.Message.ToString());
            throw;
        }
    }

    private string GetTopicSummaryPrompt()
    {
        try
        {
            string filePath = Directory.GetCurrentDirectory() + "/wwwroot/assets/summary_prompt.md";

            string text = System.IO.File.ReadAllText(filePath);
            return text;
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

}
