namespace AiChatMvcV2.Contracts

{
    public interface IModelServices
    {
        public Task<string> GetModelResponseAsync(string Model, string Prompt);
    }
}