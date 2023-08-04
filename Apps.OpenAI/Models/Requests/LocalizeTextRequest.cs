namespace Apps.OpenAI.Models.Requests
{
    public class LocalizeTextRequest
    {
        public string Text { get; set; }
        public string Locale { get; set; }
        public string? Model { get; set; }
    }
}

