using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PromptLab.Models;
using PromptLab.Models.ViewModels;
using PromptLab.Services;

namespace PromptLab.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly ChatService _chat;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatController(ChatService chat, UserManager<ApplicationUser> userManager)
    {
        _chat = chat;
        _userManager = userManager;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var sessions = await _chat.GetUserSessionsAsync(CurrentUserId, ct);
        return View(sessions);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        if (!await _chat.HasApiKeyAsync(CurrentUserId))
        {
            TempData["Error"] = "Sohbet başlatmadan önce OpenRouter API anahtarınızı kaydetmelisiniz.";
            return RedirectToAction("Index", "ApiKey");
        }

        var vm = new NewChatViewModel
        {
            ActiveModels = await _chat.GetActiveModelsAsync(ct)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NewChatViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.ActiveModels = await _chat.GetActiveModelsAsync(ct);
            return View(vm);
        }

        try
        {
            var session = await _chat.CreateSessionAsync(CurrentUserId, vm.SelectedModelId, vm.SystemPrompt, ct);
            return RedirectToAction(nameof(Session), new { id = session.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.ActiveModels = await _chat.GetActiveModelsAsync(ct);
            return View(vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Session(int id, CancellationToken ct)
    {
        var session = await _chat.GetSessionAsync(id, CurrentUserId, ct);
        if (session is null)
        {
            return NotFound();
        }

        ViewBag.HasApiKey = await _chat.HasApiKeyAsync(CurrentUserId);
        return View(session);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int id, string prompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            TempData["Error"] = "Boş mesaj gönderilemez.";
            return RedirectToAction(nameof(Session), new { id });
        }

        try
        {
            await _chat.SendMessageAsync(id, CurrentUserId, prompt, ct);
        }
        catch (MissingApiKeyException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "ApiKey");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = $"OpenRouter isteği başarısız oldu: {ex.Message}";
        }

        return RedirectToAction(nameof(Session), new { id });
    }
}
