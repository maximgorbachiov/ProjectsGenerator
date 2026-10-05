using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHook.API.Models
{
    public class FeatureEntity
    {
        [Key]
        public int FeatureId { get; set; }
        [ForeignKey("ProjectEntity")]
        public int ProjectId { get; set; }
        public string? CardId { get; set; }
        public string? Aliases { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Link { get; set; }
    }
}
