namespace WebHook.API.Models
{
    public class QueueMessage
    {
        public int Id { get; set; }
        public CardType CardType { get; set; }
        public bool IsNew { get; set; }
    }
}
