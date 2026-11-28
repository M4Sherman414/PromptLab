using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PromptLab.Filters;
using PromptLab.Models;
using PromptLab.Models.ViewModels;
using PromptLab.Services;

namespace PromptLab.Controllers;

[Authorize]
public class ComparisonController : Controller
{
    private readonly ComparisonService _comparison;
    private readonly ApiKeyService _apiKeys;
    private readonly UserManager<ApplicationUser> _userManager;

    public ComparisonController(
        ComparisonService comparison,
        ApiKeyService apiKeys,
        UserManager<ApplicationUser> userManager)
    {
        _comparison = comparison;
        _apiKeys = apiKeys;
        _userManager = userManager;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new ComparisonCreateViewModel();
        await PopulateViewModelAsync(vm, ct);

        if (vm.ActiveModels.Count >= 2)
        {
            vm.ModelAId = vm.ActiveModels[0].Id;
            vm.ModelBId = vm.ActiveModels[1].Id;
        }
        else if (vm.ActiveModels.Count == 1)
        {
            vm.ModelAId = vm.ActiveModels[0].Id;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireApiKey]
    public async Task<IActionResult> Create(ComparisonCreateViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulateViewModelAsync(vm, ct);
            return View("Index", vm);
        }

        try
        {
            var request = new ComparisonRequest(
                vm.ModelAId,
                vm.ModelBId,
                vm.UserPrompt,
                vm.SystemPrompt,
                vm.Temperature,
                vm.MaxTokens);

            var comparison = await _comparison.CreateComparisonAsync(CurrentUserId, request, ct);
            return RedirectToAction(nameof(Result), new { id = comparison.Id });
        }
        catch (MissingApiKeyException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "ApiKey", new { returnUrl = Url.Action(nameof(Index)) });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateViewModelAsync(vm, ct);
            return View("Index", vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Result(int id, CancellationToken ct)
    {
        var comparison = await _comparison.GetComparisonAsync(id, CurrentUserId, ct);
        if (comparison is null)
        {
            return NotFound();
        }

        return View(comparison);
    }

    private async Task PopulateViewModelAsync(ComparisonCreateViewModel vm, CancellationToken ct)
    {
        vm.KeyStatus = (await _apiKeys.CheckStoredKeyAsync(CurrentUserId, ct)).Status;
        vm.ActiveModels = await _comparison.GetActiveModelsAsync(ct);
    }
}
