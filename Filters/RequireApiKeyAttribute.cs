using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using PromptLab.Models;
using PromptLab.Services;

namespace PromptLab.Filters;

/// <summary>
/// OpenRouter API anahtarı kayıtlı olmayan ya da OpenRouter tarafından tanınmayan
/// kullanıcıyı API anahtarı sayfasına yönlendirir.
/// Anahtar kaydedildikten sonra kullanıcı geldiği sayfaya geri döner (returnUrl).
/// AJAX isteklerinde yönlendirme yerine 409 ve JSON döner.
/// </summary>
public class RequireApiKeyAttribute : TypeFilterAttribute
{
    public RequireApiKeyAttribute() : base(typeof(RequireApiKeyFilter))
    {
    }
}

public class RequireApiKeyFilter : IAsyncActionFilter
{
    public const string Message = "Devam etmeden önce OpenRouter API anahtarınızı kaydetmelisiniz.";
    public const string InvalidMessage = "Kayıtlı OpenRouter anahtarınız OpenRouter tarafından tanınmıyor (401). Lütfen anahtarınızı güncelleyin.";

    private readonly ApiKeyService _apiKeys;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUrlHelperFactory _urlHelperFactory;

    public RequireApiKeyFilter(
        ApiKeyService apiKeys,
        UserManager<ApplicationUser> userManager,
        IUrlHelperFactory urlHelperFactory)
    {
        _apiKeys = apiKeys;
        _userManager = userManager;
        _urlHelperFactory = urlHelperFactory;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userId = _userManager.GetUserId(context.HttpContext.User);

        // Giriş yapmamış kullanıcıyı [Authorize] zaten giriş sayfasına gönderir.
        if (userId is null)
        {
            await next();
            return;
        }

        // Anahtar yoksa ya da OpenRouter tanımıyorsa (401) durdurulur.
        // OpenRouter'a ulaşılamadıysa (Unknown) kullanıcı engellenmez.
        var status = (await _apiKeys.CheckStoredKeyAsync(userId, context.HttpContext.RequestAborted)).Status;
        if (status is ApiKeyStatus.Valid or ApiKeyStatus.Unknown)
        {
            await next();
            return;
        }

        var message = status == ApiKeyStatus.Invalid ? InvalidMessage : Message;

        var request = context.HttpContext.Request;
        var urlHelper = _urlHelperFactory.GetUrlHelper(context);
        var returnUrl = BuildReturnUrl(request, urlHelper);
        var apiKeyUrl = urlHelper.Action("Index", "ApiKey", new { returnUrl });

        if (IsAjax(request))
        {
            context.Result = new JsonResult(new { error = message, redirectUrl = apiKeyUrl })
            {
                StatusCode = StatusCodes.Status409Conflict
            };
            return;
        }

        if (context.Controller is Controller controller)
        {
            controller.TempData["Error"] = message;
        }

        context.Result = new RedirectResult(apiKeyUrl ?? "/ApiKey");
    }

    private static string? BuildReturnUrl(HttpRequest request, IUrlHelper urlHelper)
    {
        // GET isteğinde kullanıcı aynı sayfaya döner.
        if (HttpMethods.IsGet(request.Method))
        {
            return request.PathBase + request.Path + request.QueryString;
        }

        // POST isteğinde (ör. mesaj gönderme) formun bulunduğu sayfaya döner.
        if (Uri.TryCreate(request.Headers.Referer.ToString(), UriKind.Absolute, out var referer)
            && string.Equals(referer.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase))
        {
            var local = referer.PathAndQuery;
            return urlHelper.IsLocalUrl(local) ? local : null;
        }

        return null;
    }

    public static bool IsAjax(HttpRequest request) =>
        string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
