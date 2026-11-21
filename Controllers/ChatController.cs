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
    private readonly ApiKeyService _apiKeys;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatController(ChatService chat, ApiKeyService apiKeys, UserManager<ApplicationUser> userManager)
    {
        _chat = chat;
        _apiKeys = apiKeys;
        _userManager = userManager;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var sessions = await _chat.GetUserSessionsAsync(CurrentUserId, ct);
        return View(sessions);
    }

    // Yeni sohbet ekranı artık ana sayfa (Home/Index). Eski adres oraya yönlenir.
    [HttpGet]
    public IActionResult Create() => RedirectToAction("Index", "Home");

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireApiKey]
    public async Task<IActionResult> Create(NewChatViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        try
        {
            var session = await _chat.CreateSessionAsync(CurrentUserId, vm.SelectedModelId, vm.SystemPrompt, ct);

            // İlk mesaj sohbet sayfasında JavaScript ile gönderilir: kullanıcı mesajını
            // hemen görür, ardından "yazıyor…" ve cevap gelir. TempData tek seferlik
            // olduğu için sayfa yenilenince mesaj ikinci kez gönderilmez.
            TempData["PendingPrompt"] = vm.Prompt.Trim();
            return RedirectToAction(nameof(Session), new { id = session.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "Home");
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

        var keyStatus = (await _apiKeys.CheckStoredKeyAsync(CurrentUserId, ct)).Status;
        ViewBag.HasApiKey = keyStatus is ApiKeyStatus.Valid or ApiKeyStatus.Unknown;
        ViewBag.KeyStatus = keyStatus;
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
                // Kullanıcının mesajı tarayıcıda zaten gösterildi; yalnızca model cevabı döner.
                return PartialView("_ChatMessage", turn.AssistantMessage);
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
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Anahtar sonradan OpenRouter'da silinmiş/iptal edilmiş olabilir.
            _apiKeys.ForgetCheck(CurrentUserId);
            const string invalidKey = "OpenRouter API anahtarınızı tanımadı (401). Lütfen anahtarınızı güncelleyin.";
            var apiKeyUrl = Url.Action("Index", "ApiKey", new { returnUrl = Url.Action(nameof(Session), new { id }) });
            if (isAjax)
            {
                return Conflict(new { error = invalidKey, redirectUrl = apiKeyUrl });
            }

            TempData["Error"] = invalidKey;
            return Redirect(apiKeyUrl!);
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
