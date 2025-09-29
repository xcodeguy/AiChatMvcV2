namespace AiChatMvcV2.Contracts

{
    public interface IModelServices
    {
        public Task<string> GetModelResponseAsync(string Model, string SystemContent, string UserContent, string NegativePrompt);
    }
}