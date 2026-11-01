using System.ComponentModel.DataAnnotations;

namespace PromptLab.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalı.")]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Şifreler aynı değil.")]
    [Display(Name = "Şifre tekrarı")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
