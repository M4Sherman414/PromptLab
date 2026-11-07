using System.ComponentModel.DataAnnotations;

namespace PromptLab.Models.ViewModels;

public class ModelEditViewModel
{
    public int Id { get; set; }

    public string OpenRouterModelId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Görünen ad zorunludur.")]
    [MaxLength(200, ErrorMessage = "Görünen ad en fazla 200 karakter olabilir.")]
    [Display(Name = "Görünen ad")]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; }
}
