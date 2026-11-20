using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PromptLab.Models;

public class ChatSession
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    [Required]
    public int ModelId { get; set; }

    [ForeignKey("ModelId")]
    public ModelCatalog? ModelCatalog { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = "Yeni sohbet";

    public string? SystemPrompt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
