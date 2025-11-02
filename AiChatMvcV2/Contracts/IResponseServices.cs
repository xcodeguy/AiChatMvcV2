using AiChatMvcV2.Models;

namespace AiChatMvcV2.Contracts
{
    public interface IResponseServices
    {
        public Task<string> RemoveHtmlAndThinkTagsFromModelResponse(string json);
        public Task<bool> PlaySpeechFile();
        public ResponseJsonObject? ExtractAndDeserialize(string plainText);
    }
}