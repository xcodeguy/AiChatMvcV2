namespace AiChatMvcV2.Contracts
{
    public interface IResponseController
    {
        public Task<string> ParseJsonForObject(string json);
    }
}