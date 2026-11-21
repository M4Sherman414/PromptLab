using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PromptLab.Filters;
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
    [RequireApiKey]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var vm = new NewChatViewModel
        {
            ActiveModels = await _chat.GetActiveModelsAsync(ct)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireApiKey]
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
    [RequireApiKey]
    public async Task<IActionResult> SendMessage(int id, string prompt, CancellationToken ct)
    {
        // Sohbet sayfasındaki JavaScript isteği AJAX ile gönderir ve cevabı
        // sayfayı yenilemeden ekler. JavaScript kapalıysa normal form akışı çalışır.
        var isAjax = RequireApiKeyFilter.IsAjax(Request);

        if (string.IsNullOrWhiteSpace(prompt))
        {
            const string emptyMessage = "Boş mesaj gönderilemez.";
            if (isAjax)
            {
                return BadRequest(new { error = emptyMessage });
            }

            TempData["Error"] = emptyMessage;
            return RedirectToAction(nameof(Session), new { id });
        }

        try
        {
            var turn = await _chat.SendMessageAsync(id, CurrentUserId, prompt, ct);

            if (isAjax)
            {
                // Servis oturumu takip ettiği için EF, ChatSession bağlantısını kendisi doldurur.
                ViewData["ModelName"] = turn.AssistantMessage.ChatSession?.ModelCatalog?.DisplayName;
                return PartialView("_ChatTurn", turn);
            }
        }
        catch (MissingApiKeyException ex)
        {
            if (isAjax)
            {
                return Conflict(new { error = ex.Message, redirectUrl = Url.Action("Index", "ApiKey") });
            }

            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "ApiKey");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = $"OpenRouter isteği başarısız oldu: {ex.Message}";
            if (isAjax)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = errorMessage });
            }

            TempData["Error"] = errorMessage;
        }

        return RedirectToAction(nameof(Session), new { id });
    }
}
