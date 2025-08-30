using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AiChatMvcV2.Models;
using AiChatMvcV2.Classes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiChatMvcV2.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly CallController _callController;
    private readonly ResponseController _responseController;

    public HomeController(ILogger<HomeController> logger, CallController callController, ResponseController responseController)
    {
        _logger = logger;
        _callController = callController;
        _responseController = responseController;

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

            _logger.LogInformation("Calling API async for model {ModelName}", model);
            var Json = await _callController.CallApiAsync(model, SystemContent, UserContent, NegativePrompt);
            var Response = await _responseController.ParseJsonForObject(Json.ToString()!);
            TimeSpan TimeSpent = (DateTime.Now - StartTime);

            _logger.LogInformation("Calling API async for Topic summary.");
            var JsonTopic = await _callController.CallApiAsync("gemma3",
            "Summarize the following text into a one to two word description and put a | in front of the first word of the description and a | after the last word of the description. Put the description at the end of the response.",
            Response,
            "");
            var ResponseTopic = await _responseController.ParseJsonForObject(JsonTopic.ToString()!);

            Item = new ResponseItem();
            Item.TimeStamp = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            Item.Response = Response;
            Item.Model = model;
            Item.Topic = _responseController.GetTopicFromResponse(ResponseTopic).Trim();
            Item.Prompt = SystemContent + UserContent;
            Item.NegativePrompt = NegativePrompt;
            Item.Active = 1;
            Item.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            Item.ResponseTime = String.Format("{0:00}:{1:00}:{2:00}", TimeSpent.Hours, TimeSpent.Minutes, TimeSpent.Seconds);

            Item.WordCount = _responseController.GetWordCount(Response);

            ViewModel.ResponseItemList?.Add(Item);

            bool success = _callController.InsertResponse(Item);

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
