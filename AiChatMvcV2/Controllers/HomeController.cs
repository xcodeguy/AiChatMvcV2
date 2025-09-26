using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AiChatMvcV2.Models;
using AiChatMvcV2.Classes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.IO;
using System.Media;
using AiChatMvcV2.Objects;
using Microsoft.Extensions.Options;

namespace AiChatMvcV2.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationSettings _settings;
    private readonly CallController _callController;
    private readonly ResponseController _responseController;

    public HomeController(IOptions<ApplicationSettings> settings,
                            ILogger<HomeController> logger,
                            CallController callController,
                            ResponseController responseController)
    {
        _logger = logger;
        _callController = callController;
        _responseController = responseController;
        _settings = settings.Value;

        _logger.LogDebug(1, "NLog injected into HomeController");

    }

    [HttpPost]
    public async Task<IActionResult> MakeApiCall(string model,
                                                string SystemContent,
                                                string UserContent,
                                                string NegativePrompt)
    {
        try
        {
            DateTime StartTime = DateTime.Now;
            HomeViewModel ViewModel = new HomeViewModel();
            ViewModel.ResponseItemList = new List<ResponseItem>();
            ResponseItem Item;

            // call the inference server to egenrate a response
            _logger.LogInformation("Calling API async for model {ModelName}", model);
            var JsonResponse = await _callController.CallApiAsync(model, SystemContent, UserContent, NegativePrompt);
            var TextResponse = await _responseController.ParseJsonForObject(JsonResponse.ToString()!);
            TimeSpan TimeSpent = DateTime.Now - StartTime;

            // call the inference server to summarize the response into a one or two word topic
            _logger.LogInformation("Calling API async for Topic summary.");
            var JsonTopic = await _callController.CallApiAsync("llama3.1",
                "Summarize the following text into a one or two word description:",
                TextResponse,
                "The response cannot be more than two words. Do not use any special characters. Do not use any punctuation."
            );
            var ResponseTopic = await _responseController.ParseJsonForObject(JsonTopic.ToString()!);

            //call the inference server to generate natural language for the topic
            var AudioFilename = await _responseController.GenerateTextToSpeechResourceFile(TextResponse, "tara");
            // Check if the source file exists
            long fileSizeInBytes = 0;
            string local_path_to_assets_folder = _settings.SpeechFilePlaybackLocation!;
            if (!System.IO.File.Exists(local_path_to_assets_folder + AudioFilename))
            {
                _logger.LogInformation("Error: Source file not found: {file}", AudioFilename);
                return NotFound($"Audio file not found: {local_path_to_assets_folder + AudioFilename}");
            }
            else
            {
                FileInfo fileInfo = new FileInfo(local_path_to_assets_folder + AudioFilename);
                fileSizeInBytes = fileInfo.Length;
                _logger.LogInformation("Audio file generated: {file}, size: {size} bytes", AudioFilename, fileSizeInBytes);
            }

            Item = new ResponseItem
            {
                TimeStamp = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"),
                Response = TextResponse,
                Model = model,
                Topic = ResponseTopic.ToString(),
                Prompt = SystemContent + UserContent,
                NegativePrompt = NegativePrompt,
                Active = 1,
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"),
                AudioFilename = _settings.SpeechFileUrlLocation + AudioFilename,
                AudioFileSize = fileSizeInBytes.ToString(),
                ResponseTime = String.Format("{0:00}:{1:00}:{2:00}", TimeSpent.Hours, TimeSpent.Minutes, TimeSpent.Seconds),
                WordCount = _responseController.GetWordCount(TextResponse)
            };

            ViewModel.ResponseItemList?.Add(Item);

            bool success = _callController.InsertResponse(Item);
            if (!success)
            {
                _logger.LogCritical("Error inserting response into database: {responseItem}", Item);
                throw new Exception("Error inserting response into database.");
            }

            _logger.LogInformation("Returning a response for model {ModelName}", model);
            return Ok(ViewModel);
        }
        catch (Exception e)
        {
            String ExceptionMessageString = String.Format("Exception in HomeController::MakeApiCall() {0}, {1} {2}, {3}", model, UserContent, NegativePrompt, e.Message.ToString());
            _logger.LogCritical(ExceptionMessageString);
            throw;
        }
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
