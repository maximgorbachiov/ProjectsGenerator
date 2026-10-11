namespace WebHook.API.Models
{
    public class FeatureCard : Card
    {
        /// <summary>
        /// Id that is setup manually in the Trello card as unique identifier inside project namespace.
        /// </summary>
        public int FeatureId { get; set; }
    }
}
