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
    public async Task<IActionResult> Index()
    {
        var model = new ApiKeyViewModel();
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

        await _apiKeys.SaveAsync(CurrentUserId, model.NewKey);
        TempData["Message"] = "API anahtarınız kaydedildi.";
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
    }
}
