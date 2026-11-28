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

    // Karşılaştırma başlatma artık ana sayfadaki (Home/Index) mod seçimi üzerinden yapılır.
    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "Home", new { mode = "compare" });

    /// <summary>
    /// AJAX ile karşılaştırmayı başlatır: Comparison ve iki bekleyen ComparisonResult kaydı açar,
    /// ardından istemci Slot A ve Slot B için paralel olarak RunSlot çağrısı yapar.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireApiKey]
    public async Task<IActionResult> Start(ComparisonCreateViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Lütfen form alanlarını kontrol edin.";
            return BadRequest(new { error = firstError });
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

            var comparison = await _comparison.StartComparisonAsync(CurrentUserId, request, ct);
            return Json(new
            {
                comparisonId = comparison.Id,
                resultUrl = Url.Action(nameof(Result), new { id = comparison.Id })
            });
        }
        catch (MissingApiKeyException ex)
        {
            return Conflict(new
            {
                error = ex.Message,
                redirectUrl = Url.Action("Index", "ApiKey", new { returnUrl = Url.Action("Index", "Home", new { mode = "compare" }) })
            });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Tek bir slotu (A veya B) çalıştırır ve güncellenmiş _ComparisonResultCard partial HTML'ini döner.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireApiKey]
    public async Task<IActionResult> RunSlot(int id, ResultSlot slot, CancellationToken ct)
    {
        try
        {
            var result = await _comparison.ExecuteSlotAsync(id, CurrentUserId, slot, ct);
            return PartialView("_ComparisonResultCard", result);
        }
        catch (MissingApiKeyException ex)
        {
            return Conflict(new
            {
                error = ex.Message,
                redirectUrl = Url.Action("Index", "ApiKey", new { returnUrl = Url.Action(nameof(Result), new { id }) })
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // JavaScript kapalıysa klasik form gönderimi için yedek akış.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireApiKey]
    public async Task<IActionResult> Create(ComparisonCreateViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage;
            return RedirectToAction("Index", "Home", new { mode = "compare" });
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
            return RedirectToAction("Index", "ApiKey", new { returnUrl = Url.Action("Index", "Home", new { mode = "compare" }) });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "Home", new { mode = "compare" });
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

        var keyStatus = (await _apiKeys.CheckStoredKeyAsync(CurrentUserId, ct)).Status;
        ViewBag.HasApiKey = keyStatus is ApiKeyStatus.Valid or ApiKeyStatus.Unknown;
        return View(comparison);
    }
}
