using AiChatMvcV2.Models;

namespace AiChatMvcV2.Contracts
{
    public interface IResponseServices
    {
        public Task<string> RemoveHtmlAndThinkTagsFromModelResponse(string json);
        public Task<bool> PlaySpeechFile();
        public Dictionary<string, object> ExtractAndDeserialize(string prompt, string plainText);
        public ResponseJsonObjectFlat GradeJsonForResponseObject(Dictionary<string, object> jsonCandidate, ResponseJsonObjectFlat responseFlat);
    }
}