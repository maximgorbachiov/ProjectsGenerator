namespace WebHook.API.Models
{
    public class Card
    {
        public string TrelloCardId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
    }
}
