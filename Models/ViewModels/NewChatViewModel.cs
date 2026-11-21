using System.ComponentModel.DataAnnotations;
using PromptLab.Services;

namespace PromptLab.Models.ViewModels;

// Ana sayfadaki "Yeni sohbet" ekranının modeli: model seçimi, isteğe bağlı
// system prompt ve sohbeti başlatan ilk mesaj.
public class NewChatViewModel
{
    [Required(ErrorMessage = "Lütfen bir model seçin.")]
    [Display(Name = "Model")]
    public int SelectedModelId { get; set; }

    [MaxLength(4000)]
    [Display(Name = "System prompt (isteğe bağlı)")]
    public string? SystemPrompt { get; set; }

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
