namespace WebHook.API.Models
{
    public class ProjectCard : Card
    {
        /// <summary>
        /// Id that is setup manually in the Trello card as unique identifier inside project namespace.
        /// </summary>
        public int ProjectId { get; set; }
    }
}
