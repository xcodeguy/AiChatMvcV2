namespace AiChatMvcV2.Objects
{
    public class ApplicationSettings
    {
        public required string Url { get; set; }
        public required string TTSApiEndpointUrl { get; set; }
        public required string TTSModelName { get; set; }
        public required string SpeechFileDropLocation { get; set; }
        public required string SpeechFilePlaybackLocation { get; set; }
        public required string SpeechFilePlaybackName { get; set; }
        public required string SpeechFileUrlLocation { get; set; }
        public required string SpeechFileFormat { get; set; }
        public required string PlaybackSpeed { get; set; }
    }
}