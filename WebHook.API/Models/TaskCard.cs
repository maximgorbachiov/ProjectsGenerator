namespace WebHook.API.Models
{
    public class TaskCard : Card
    {
        /// <summary>
        /// Id that is setup manually in the Trello card as unique identifier inside project namespace.
        /// </summary>
        public int TaskId { get; set; }
    }
}
