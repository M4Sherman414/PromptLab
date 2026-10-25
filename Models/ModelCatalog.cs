using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PromptLab.Models;

[Index(nameof(OpenRouterModelId), IsUnique = true)]
public class ModelCatalog
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string OpenRouterModelId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Precision(18, 6)]
    public decimal InputPricePerMillion { get; set; }

    [Precision(18, 6)]
    public decimal OutputPricePerMillion { get; set; }

    public bool IsFree { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastSyncedAt { get; set; }
}