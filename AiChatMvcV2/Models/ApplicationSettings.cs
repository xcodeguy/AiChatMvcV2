namespace AiChatMvcV2.Objects
{
    public class ApplicationSettings
    {
        public string? Model1 { get; set; } = null;
        public string? Model2 { get; set; } = null;
        public string? Url { get; set; } = null;
        public string? TTSApiEndpointUrl { get; set; } = null;
        public string? TTSModelName { get; set; } = null;
        public string? SpeechFileDropLocation { get; set; } = null;
        public string? SpeechFilePlaybackLocation { get; set; } = null;
        public string? SpeechFilePlaybackName { get; set; } = null;
    }
}