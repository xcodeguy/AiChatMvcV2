namespace AiChatMvcV2.Contracts

{
    public interface ICallController
    {
        public Task<string> CallApiAsync(string Model, string SystemContent, string UserContent, string NegativePrompt);
    }
}