using System.ComponentModel.DataAnnotations;

namespace PromptLab.Models.ViewModels;

public class NewChatViewModel
{
    [Required(ErrorMessage = "Lütfen bir model seçin.")]
    [Display(Name = "Model")]
    public int SelectedModelId { get; set; }

    [MaxLength(4000)]
    [Display(Name = "Sistem talimatı (isteğe bağlı)")]
    public string? SystemPrompt { get; set; }

    public List<ModelCatalog> ActiveModels { get; set; } = new();
}
