using System.ComponentModel.DataAnnotations;
using PromptLab.Services;

namespace PromptLab.Models.ViewModels;

// Ana sayfadaki "Yeni sohbet" ekranının modeli: sohbet ya da karşılaştırma modu seçimi,
// model seçimleri, isteğe bağlı parametreler ve ilk mesaj/prompt.
public class NewChatViewModel
{
    public string Mode { get; set; } = "chat";

    [Required(ErrorMessage = "Lütfen bir model seçin.")]
    [Display(Name = "Model")]
    public int SelectedModelId { get; set; }

    [Display(Name = "Model A")]
    public int ModelAId { get; set; }

    [Display(Name = "Model B")]
    public int ModelBId { get; set; }

    [MaxLength(4000)]
    [Display(Name = "System prompt (isteğe bağlı)")]
    public string? SystemPrompt { get; set; }

    [Range(0.0, 2.0, ErrorMessage = "Temperature 0 ile 2 arasında olmalıdır.")]
    public double? Temperature { get; set; }

    [Range(1, 128000, ErrorMessage = "Maksimum token 1 ile 128.000 arasında olmalıdır.")]
    public int? MaxTokens { get; set; }

    [Required(ErrorMessage = "Sohbeti başlatmak için bir mesaj yazın.")]
    [MaxLength(20000)]
    [Display(Name = "Mesaj")]
    public string Prompt { get; set; } = string.Empty;

    public List<ModelCatalog> ActiveModels { get; set; } = new();

    // Ekran durumu (formdan gelmez)
    public bool IsSignedIn { get; set; }
    public ApiKeyStatus KeyStatus { get; set; } = ApiKeyStatus.Missing;

    // OpenRouter'a ulaşılamadıysa (Unknown) kullanıcı engellenmez.
    public bool HasUsableKey => KeyStatus is ApiKeyStatus.Valid or ApiKeyStatus.Unknown;
}
