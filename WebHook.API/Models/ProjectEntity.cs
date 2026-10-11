using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHook.API.Models
{
    public class ProjectEntity
    {
        [Key]
        public int ProjectId { get; set; }
        public int ProjectCardId { get; set; }
        public string? TrelloCardId { get; set; }
        public string? Name { get; set; }
        public string? Aliases { get; set; }
        public string? Description { get; set; }
        public string? Link { get; set; }
    }
}
