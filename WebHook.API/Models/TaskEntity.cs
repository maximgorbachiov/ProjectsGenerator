using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHook.API.Models
{
    public class TaskEntity
    {
        [Key]
        public int TaskId { get; set; }
        public int TaskCardId { get; set; }
        [ForeignKey("FeatureEntity")]
        public int? FeatureId { get; set; }
        [ForeignKey("ProjectEntity")]
        public int? ProjectId { get; set; }
        public string? TrelloCardId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Link { get; set; }
    }
}
