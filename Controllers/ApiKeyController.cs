using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PromptLab.Models;
using PromptLab.Models.ViewModels;
using PromptLab.Services;

namespace PromptLab.Controllers;

[Authorize]
public class ApiKeyController : Controller
{
    private readonly ApiKeyService _apiKeys;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApiKeyController(ApiKeyService apiKeys, UserManager<ApplicationUser> userManager)
    {
        _apiKeys = apiKeys;
        _userManager = userManager;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;

    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl = null)
    {
        // Sorgu dizesindeki returnUrl ModelState'e girer ve asp-for onu tercih eder;
        // temizlenmezse doğrulanmamış adres gizli alana yazılır.
        ModelState.Clear();

        var model = new ApiKeyViewModel
        {
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
        };
        await FillStatusAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ApiKeyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillStatusAsync(model);
            return View(nameof(Index), model);
        }

        // Anahtar kaydedilmeden önce OpenRouter'a doğrulatılır; tanınmayan anahtar kaydedilmez.
        var check = await _apiKeys.ValidateAsync(model.NewKey, HttpContext.RequestAborted);
        if (check.Status != ApiKeyStatus.Valid)
        {
            ModelState.AddModelError(nameof(model.NewKey), check.Status == ApiKeyStatus.Invalid
                ? "OpenRouter bu anahtarı tanımadı (401). Anahtarı openrouter.ai/keys sayfasından eksiksiz kopyaladığınızdan emin olun."
                : "OpenRouter'a şu an ulaşılamadı, anahtar doğrulanamadı. Biraz sonra tekrar deneyin.");
            await FillStatusAsync(model);
            return View(nameof(Index), model);
        }

        await _apiKeys.SaveAsync(CurrentUserId, model.NewKey);
        TempData["Message"] = "API anahtarınız OpenRouter tarafından doğrulandı ve kaydedildi.";

        // Kullanıcı sohbet gibi bir sayfadan yönlendirildiyse oraya geri döner.
        // Açık yönlendirmeye karşı yalnızca yerel adreslere izin verilir.
        if (Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl!);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete()
    {
        await _apiKeys.DeleteAsync(CurrentUserId);
        TempData["Message"] = "API anahtarınız silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task FillStatusAsync(ApiKeyViewModel model)
    {
        var key = await _apiKeys.GetAsync(CurrentUserId);
        model.HasKey = key is not null;
        model.LastFour = key?.LastFour;
        model.SavedAt = key?.CreatedAt;
        model.Check = key is null ? null : await _apiKeys.CheckStoredKeyAsync(CurrentUserId, HttpContext.RequestAborted);
    }
}
