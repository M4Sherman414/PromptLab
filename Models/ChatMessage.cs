using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PromptLab.Models;

public class ChatMessage
{
    public int Id { get; set; }

    [Required]
    public int ChatSessionId { get; set; }

    [ForeignKey("ChatSessionId")]
    public ChatSession? ChatSession { get; set; }

    [Required]
    public ChatRole Role { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public int? InputTokens { get; set; }

    public int? OutputTokens { get; set; }

    public int? LatencyMs { get; set; }

    public int? TimeToFirstTokenMs { get; set; }

    [Precision(18, 8)]
    public decimal? EstimatedCost { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
