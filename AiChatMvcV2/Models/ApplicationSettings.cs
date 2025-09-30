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
        public required int HttpTtsTimeout { get; set; }
        public required int HttpApiTimeout { get; set; }
        public required string TopicSummaryModelName { get; set; }
        public required string TopicSummaryPrompt { get; set; }
        public required string TopicSummaryNegativePrompt { get; set; }
        public required List<string> TtsVoices { get; set; }
        public required bool ModelServicesTestException { get; set; }
        public required bool ResponseServicesTestException { get; set; }
        public required bool MySqlTestException { get; set; }
        public required string StartupPrompt { get; set; }
        public required string NegativePrompt { get; set; }
    }
}