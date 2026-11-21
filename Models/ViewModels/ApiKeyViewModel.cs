using System.ComponentModel.DataAnnotations;
using PromptLab.Services;

namespace PromptLab.Models.ViewModels;

public class ApiKeyViewModel
{
    public bool HasKey { get; set; }
    public string? LastFour { get; set; }
    public DateTime? SavedAt { get; set; }

    // Anahtar kaydedildikten sonra dönülecek yerel adres (ör. sohbet sayfası).
    public string? ReturnUrl { get; set; }

    // Kayıtlı anahtarın OpenRouter'daki durumu (geçerli / tanınmıyor / bilinmiyor) ve limit bilgisi.
    public ApiKeyCheck? Check { get; set; }

    [Required(ErrorMessage = "API anahtarı zorunludur.")]
    [RegularExpression(@"^sk-or-\S{10,}$", ErrorMessage = "Geçerli bir OpenRouter anahtarı girin. Anahtar sk-or- ile başlar.")]
    [DataType(DataType.Password)]
    [Display(Name = "OpenRouter API anahtarı")]
    public string NewKey { get; set; } = string.Empty;
}
