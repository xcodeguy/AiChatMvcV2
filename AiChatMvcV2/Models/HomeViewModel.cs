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
    }

    public class HomeViewModel
    {
        public List<ResponseItem>? ResponseItemList { get; set; } = null;
    }
}