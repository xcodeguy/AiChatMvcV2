using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;

namespace AiChatMvcV2.Models
{
    public class ResponseItem
    {
        public string TimeStamp { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string NegativePrompt { get; set; } = string.Empty;
        public int Active { get; set; } = 0;
        public string LastUpdated { get; set; } = string.Empty;
        public string ResponseTime { get; set; } = string.Empty;
        public int WordCount { get; set; } = 0;
        public string AudioFilename { get; set; } = string.Empty;
        public string AudioFileSize { get; set; } = string.Empty;
        public required string Exceptions { get; set; } = string.Empty;
        public required string TtsVoice { get; set; } = string.Empty;
        public required int Score { get; set; } = 0;
        public required int Grade { get; set; } = 0;
        public required List<string> ScoreReasons { get; set; } = [];
    }

    public class HomeViewModel
    {
        public List<ResponseItem>? ResponseItemList { get; set; } = null;
    }

    public class ResponseJsonObjectFlat
    {
        public string? ResponseText { get; set; } = string.Empty;
        public string? TopicText { get; set; } = string.Empty;
        public int JsonScore { get; set; } = 0;
        public int ComparisonGrade { get; set; } = 0;
        public List<String> PonitDeductionReasons { get; set; } = [];
    }

    public class ModelParameters
    {
        public string ParameterName { get; set; } = string.Empty;
        public Double ParameterValue { get; set; } = 0;
    }
}