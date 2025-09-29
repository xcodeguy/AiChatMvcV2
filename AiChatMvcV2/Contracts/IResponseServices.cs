namespace AiChatMvcV2.Contracts
{
    public interface IResponseServices
    {
        public Task<string> SanitizeResponseFromJson(string json);
        public void PlaySpeechFile();
    }
}