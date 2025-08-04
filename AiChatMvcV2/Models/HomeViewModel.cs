namespace AiChatMvcV2.Models
{
    public class HomeViewModelListItem
    {
        public string? TimeStamp { get; set; } = null;
        public string? ModelName { get; set; } = null;
        public string? Prompt { get; set; } = null;
        public string? Response { get; set; } = null;
        public float? Iteration { get; set; } = null;

        /*        public HomeViewModelListItem()
                {
                    TimeStamp = DateTime.Now.ToString();
                    ModelName = String.Empty;
                    Prompt = String.Empty;
                    Response = String.Empty;
                    Iteration = 0;
                }*/
    }
    public class HomeViewModel
    {
        public List<HomeViewModelListItem>? ChatNonStreamingList { get; set; } = null;
    }
}