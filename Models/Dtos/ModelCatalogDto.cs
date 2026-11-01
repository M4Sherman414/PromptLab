namespace PromptLab.Models.Dtos;

public class ModelCatalogDto
{
    public int Id { get; set; }
    public string OpenRouterModelId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal InputPricePerMillion { get; set; }
    public decimal OutputPricePerMillion { get; set; }
    public bool IsFree { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}