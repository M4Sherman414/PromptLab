using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PromptLab.Models
{
    public class ComparisonResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ComparisonId { get; set; }

        [ForeignKey("ComparisonId")]
        public  Comparison? Comparison { get; set; }

        [Required]
        public int ModelId { get; set; }

        [ForeignKey("ModelId")]
        public ModelCatalog? ModelCatalog { get; set; }

        [Required]
        public string Slot { get; set; } = string.Empty; // A / B

        public string? Output { get; set; }

        public int? LatencyMs { get; set; }

        public int? TimeToFirstTokenMs { get; set; }

        public int? InputTokens { get; set; }

        public int? OutputTokens { get; set; }

        public double? TokensPerSecond { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? EstimatedCost { get; set; }

        public string? Error { get; set; }
    }
}