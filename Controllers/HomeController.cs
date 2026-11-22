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

    // Ana sayfa = "Yeni sohbet" ekranı (docs/Ekran_Taslaklari/Home&NewChat.jpg).
    // Kullanıcı modeli seçer, ilk mesajını yazar; form Chat/Create'e gider.
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new NewChatViewModel { IsSignedIn = User.Identity?.IsAuthenticated == true };

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
