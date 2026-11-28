using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PromptLab.Models;
using PromptLab.Models.ViewModels;
using PromptLab.Services;

namespace PromptLab.Controllers;

public class HomeController : Controller
{
    private readonly ChatService _chat;
    private readonly ApiKeyService _apiKeys;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ChatService chat, ApiKeyService apiKeys, UserManager<ApplicationUser> userManager)
    {
        _chat = chat;
        _apiKeys = apiKeys;
        _userManager = userManager;
    }

    // Ana sayfa = "Yeni sohbet / Karşılaştırma" başlangıç ekranı.
    // Kullanıcı modu seçer (Sohbet veya Karşılaştırma), modellerini belirler ve ilk mesajını gönderir.
    public async Task<IActionResult> Index(string? mode = null, CancellationToken ct = default)
    {
        var vm = new NewChatViewModel
        {
            IsSignedIn = User.Identity?.IsAuthenticated == true,
            Mode = string.Equals(mode, "compare", StringComparison.OrdinalIgnoreCase) ? "compare" : "chat"
        };

        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return View(vm);
        }

        // Anahtarın OpenRouter'da geçerli olup olmadığı daha mesaj yazılmadan gösterilir.
        vm.KeyStatus = (await _apiKeys.CheckStoredKeyAsync(userId, ct)).Status;
        vm.ActiveModels = await _chat.GetActiveModelsAsync(ct);

        // Varsayılan model: kullanıcının son sohbetindeki model hâlâ aktifse o,
        // değilse listedeki ilk model (liste ücretsizler önde sıralı gelir).
        var lastSession = (await _chat.GetUserSessionsAsync(userId, ct)).FirstOrDefault();
        vm.SelectedModelId = vm.ActiveModels.Any(m => m.Id == lastSession?.ModelId)
            ? lastSession!.ModelId
            : vm.ActiveModels.FirstOrDefault()?.Id ?? 0;

        vm.ModelAId = vm.SelectedModelId;
        vm.ModelBId = vm.ActiveModels.FirstOrDefault(m => m.Id != vm.ModelAId)?.Id ?? 0;

        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
