namespace AiChatMvcV2.Models
{
    public class ResponseItem
    {
        public string? TimeStamp { get; set; } = null;
        public string? Response { get; set; } = null;
        public string? Model { get; set; } = null;
        public string? Topic { get; set; } = null;
        public string? Prompt { get; set; } = null;
        public string? NegativePrompt { get; set; } = null;
        public int? Active { get; set; } = null;
        public string? LastUpdated { get; set; } = null;
        public string? ResponseTime { get; set; } = null;
        public int? WordCount { get; set; } = null;
        public string? AudioFilename { get; set; } = null;
        public string? AudioFileSize { get; set; } = null;
    }

    public class HomeViewModel
    {
        public List<ResponseItem>? ResponseItemList { get; set; } = null;
    }
}