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
    string _className = "HomeController";

    public HomeController(IOptions<ApplicationSettings> settings,
                            ILogger<HomeController> logger,
                            ModelServices callController,
                            ResponseServices responseService)
    {
        _logger = logger;
        _ModelService = callController;
        _ResponseService = responseService;
        _settings = settings.Value;

        Type declaringType = MethodBase.GetCurrentMethod()!.DeclaringType!;
        _className = declaringType.Name;
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
        string GradeText = string.Empty;
        string TopicText = string.Empty;
        string AudioFilename = string.Empty;
        string ExceptionMessageString = string.Empty;
        int Score = 0;
        int Grade = 0;
        List<string> ScoreReasons = new();

        try
        {
            // read the prompt from the prompt.md file
            var StructuredPrompt = ReadPromptFile(_settings.PromptFilename);
            if (StructuredPrompt == null)
            {
                ExceptionMessageString = $"The structured prompt for the response is missing or empty for model {model}.";
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

            // Check for a null or empty response from the API->Inference Server->Model call
            if (ResponseText == null || ResponseText == String.Empty)
            {
                ExceptionMessageString = $"The response is null or empty for model {model}.";
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }

            // Extract the possible JSON object embedded in the response text
            Dictionary<string, object> responseAsDictionary = _ResponseService.ExtractAndDeserialize(Prompt!, ResponseText);
            _logger.LogInformation($"Deserialized response JSON object from model response: {responseAsDictionary}");

            // Perform a grading algorithym on the Json Object 
            // and get a response object if available
            ResponseJsonObjectFlat responseAsObject = new ResponseJsonObjectFlat();
            responseAsObject = _ResponseService.GradeJsonForResponseObject(responseAsDictionary, responseAsObject);

            // Extract the properties
            ResponseText = responseAsObject?.ResponseText ?? String.Empty;
            TopicText = responseAsObject?.TopicText ?? String.Empty;
            Score = (int)(responseAsObject?.JsonScore)!;
            ScoreReasons = responseAsObject?.PonitDeductionReasons ?? [];
            Grade = (int)(responseAsObject?.ComparisonGrade)!;

            _logger.LogInformation($"Extracted response: {ResponseText}");
            _logger.LogInformation($"Extracted topic: {TopicText}");
            _logger.LogInformation($"Extracted score: {Score}");
            _logger.LogInformation($"Extracted grade: {Grade}");
            if (Score > Grade)
            {
                Score -= Grade;
            }
            else if (Score < Grade)
            {
                Score = Grade - Score;
            }
            else if (Score == Grade)
            {
                Score = Grade;
            }
            
            foreach (var itm in responseAsObject?.PonitDeductionReasons ?? [])
            {
                _logger.LogInformation($"Score reason: {itm}");
            }

            // call the api to generate a speech file from the TopicText summary.
            // Append a '.' period to the end of the topic before sending it
            // for audio render. This helps the model produce a more natural
            // pronunciation of the one or two word topic.
            if (_settings.PlayAudioFile == true)
            {
                _logger.LogInformation("Generating speech file as per application settings.");
                AudioFilename = "N/A";

                if (TopicText == null || TopicText == String.Empty)
                {
                    ExceptionMessageString = $"Cannot generate speech file because the topic text is null or empty for model {model}.";
                    _logger.LogCritical(ExceptionMessageString);
                    throw new Exception(ExceptionMessageString);
                }

                _logger.LogInformation("Calling API async to generate speech file for topic {Topic}.", TopicText);
                AudioFilename = await _ResponseService.GenerateSpeechFile(TopicText + ".", TtsVoice);

                //check if the audio/speech file exists, this is redundant because the file
                //checks are performed in the service layer as well, but we do it here to
                //gather the file size for the response item
                if (!System.IO.File.Exists(local_path_to_assets_folder + AudioFilename))
                {
                    ExceptionMessageString = $"Speech/Audio file not found: {AudioFilename}.";
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
        }
        catch (Exception ex)
        {
            //don't throw the exception. this is the last method in the call chain
            //and we always return an Http OK to the browser. we use the finally block
            //to assemble the response with all meta-data including any exceptions from
            //the backend services
            ExceptionMessageString = $"HomeController.cs->QueryModelForResponse: {ex.Message}";
            _logger.LogCritical(ExceptionMessageString);
            Score = 0;
            ScoreReasons.Add(ExceptionMessageString);
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
                ExceptionMessageString = $"Error inserting response into database: {Item.Model}";
                Item.Score = 0;
                Item.ScoreReasons.Add($"Deducting {_settings.MaxScore} for an unsuccessful Insert {ExceptionMessageString}");
                _logger.LogCritical(ExceptionMessageString);
                throw new Exception(ExceptionMessageString);
            }
        }
        catch (Exception ex)
        {
            ExceptionMessageString = $"HomeController.cs->QueryModelForResponse: {ex.Message}";
            Item.Score = 0;
            Item.ScoreReasons.Add($"Deducting {_settings.MaxScore} points for an exception. {ExceptionMessageString}");
            _logger.LogCritical(ExceptionMessageString);
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

    private string? ReadPromptFile(string PromptFilename)
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
            ExceptionMessageString = $"{_className}.{MethodBase.GetCurrentMethod()}: {ex.Message}";
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
            ExceptionMessageString = $"{_className}.{MethodBase.GetCurrentMethod()}: {ex.Message.ToString()}";
            _logger.LogCritical(ExceptionMessageString);
            throw new Exception(ExceptionMessageString);
        }
    }

    [HttpPost]
    public async Task<IActionResult> ReadLogFile()
    {
        string logFilePath = $"/Users/dferrell/Documents/GitHub/AiChatMvcV2/AiChatMvcV2/bin/Debug/{DateTime.Now.ToString("yyyy/MM/dd")}.log";
        int numberOfLines = 20;

        try
        {
            // Read the last 'numberOfLines' lines from the log file
            var lastLines = System.IO.File.ReadLines(logFilePath)
                                .Reverse() // Read lines in reverse order
                                .Take(numberOfLines) // Take the specified number of lines
                                .Reverse() // Reverse again to get them in original order
                                .ToList(); // Convert to a List

            _logger.LogInformation("Last {count} lines of {file}", numberOfLines, logFilePath);
            foreach (string line in lastLines)
            {
                _logger.LogInformation(line);
            }

            return Ok(lastLines);
        }
        catch (System.IO.FileNotFoundException)
        {
            _logger.LogWarning("Error: The file '{file}' was not found.", logFilePath);
            return NotFound($"Error: The file '{logFilePath}' was not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred reading the log file.");
            return BadRequest($"An error occurred: {ex.Message}");
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
            ExceptionMessageString = $"{_className}.{MethodBase.GetCurrentMethod()}: {ex.Message}";
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
