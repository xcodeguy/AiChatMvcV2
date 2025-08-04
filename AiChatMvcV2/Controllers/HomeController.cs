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
    public async Task<IActionResult> MakeApiCall(string model, String SystemContent, string UserContent, string NegativePrompt)
    {
        try
        {
            HomeViewModel ViewModel = new HomeViewModel();
            ViewModel.ChatNonStreamingList = new List<HomeViewModelListItem>();
            HomeViewModelListItem ChatItem;

            _logger.LogInformation("Calling API async for model {ModelName}", model);
            var Json = await _callController.CallApiAsync(model, SystemContent, UserContent, NegativePrompt);
            var Response = await _responseController.ParseJsonForObject(Json.ToString()!);

            ChatItem = new HomeViewModelListItem();
            ChatItem.Iteration = 1;
            ChatItem.TimeStamp = DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss tt");
            ChatItem.ModelName = model;
            ChatItem.Prompt = SystemContent + UserContent + NegativePrompt;
            ChatItem.Response = Response.ToString();
            ViewModel.ChatNonStreamingList?.Add(ChatItem);

            _logger.LogInformation("Returning a response for model {ModelName}", model);
            return  Ok(ViewModel);
        }
        catch (Exception e)
        {
            String ExceptionMessageString = String.Format("Exception in HomeController::MakeApiCall() {0}, {1} {2}, {3}", model, UserContent, NegativePrompt, e.Message.ToString());
            _logger.LogCritical(ExceptionMessageString);
            throw new Exception(ExceptionMessageString);
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
