using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PromptLab.Models
{
    public class Comparison
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public string? SystemPrompt { get; set; }

        [Required]
        public string UserPrompt { get; set; } = string.Empty;

        public double? Temperature { get; set; }

        public int? MaxTokens { get; set; }

        public bool IsBlind { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<ComparisonResult> ComparisonResults { get; set; } = new List<ComparisonResult>();
        public Vote? Vote { get; set; }
    }
}