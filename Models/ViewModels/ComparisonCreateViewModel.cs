using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PromptLab.Services;

namespace PromptLab.Models.ViewModels;

public class ComparisonCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Lütfen Model A'yı seçin.")]
    [Display(Name = "Model A")]
    public int ModelAId { get; set; }

    [Required(ErrorMessage = "Lütfen Model B'yi seçin.")]
    [Display(Name = "Model B")]
    public int ModelBId { get; set; }

    [Required(ErrorMessage = "Karşılaştırma için bir prompt yazın.")]
    [MaxLength(20000, ErrorMessage = "Prompt en fazla 20.000 karakter olabilir.")]
    [Display(Name = "Prompt")]
    public string UserPrompt { get; set; } = string.Empty;

    [MaxLength(4000, ErrorMessage = "System prompt en fazla 4.000 karakter olabilir.")]
    [Display(Name = "System prompt (isteğe bağlı)")]
    public string? SystemPrompt { get; set; }

    [ModelBinder(BinderType = typeof(InvariantDoubleModelBinder))]
    [Range(0.0, 2.0, ErrorMessage = "Temperature değeri 0 ile 2 arasında olmalıdır.")]
    [Display(Name = "Temperature (isteğe bağlı)")]
    public double? Temperature { get; set; }

    [Range(1, 128000, ErrorMessage = "Maksimum token sayısı 1 ile 128.000 arasında olmalıdır.")]
    [Display(Name = "Maksimum token (isteğe bağlı)")]
    public int? MaxTokens { get; set; }

    public List<ModelCatalog> ActiveModels { get; set; } = new();

    public ApiKeyStatus KeyStatus { get; set; } = ApiKeyStatus.Missing;

    public bool HasUsableKey => KeyStatus is ApiKeyStatus.Valid or ApiKeyStatus.Unknown;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ModelAId > 0 && ModelBId > 0 && ModelAId == ModelBId)
        {
            yield return new ValidationResult(
                "Karşılaştırma için iki farklı model seçmelisiniz.",
                new[] { nameof(ModelBId) });
        }
    }
}

/// <summary>
/// Tarayıcının <input type="number"> alanından gönderdiği noktalı (0.7) veya
/// kullanıcının girdiği virgüllü (0,7) ondalık değerleri sistem kültüründen bağımsız ayrıştırır.
/// </summary>
public class InvariantDoubleModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);
        var rawValue = valueResult.FirstValue?.Trim();

        if (string.IsNullOrEmpty(rawValue))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        var normalized = rawValue.Replace(',', '.');
        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                "Geçerli bir ondalık sayı girin (ör. 0.7).");
        }

        return Task.CompletedTask;
    }
}
