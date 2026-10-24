using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PromptLab.Models
{
    public class Vote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ComparisonId { get; set; }

        [ForeignKey("ComparisonId")]
        public Comparison? Comparison { get; set; }

        public VoteChoice Choice { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    }
}