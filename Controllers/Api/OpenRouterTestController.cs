using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PromptLab.Models;
using PromptLab.Services;

namespace PromptLab.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class OpenRouterTestController : ControllerBase
{
    private readonly OpenRouterClient _client;
    private readonly ApiKeyService _apiKeys;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public OpenRouterTestController(
        OpenRouterClient client,
        ApiKeyService apiKeys,
        UserManager<ApplicationUser> userManager,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        _client = client;
        _apiKeys = apiKeys;
        _userManager = userManager;
        _config = config;
        _env = env;
    }

    public class ChatTestRequest
    {
        [Required]
        public string Model { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Prompt { get; set; } = string.Empty;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<OpenRouterChatResult>> Chat(ChatTestRequest request)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        // Önce giriş yapan admin'in "API anahtarım" sayfasında kaydettiği şifreli anahtar,
        // yoksa user-secrets'taki test anahtarı kullanılır.
        var userId = _userManager.GetUserId(User)!;
        var apiKey = await _apiKeys.GetDecryptedKeyAsync(userId)
                     ?? _config["OpenRouter:TestApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return BadRequest("Kayıtlı bir API anahtarı ya da OpenRouter:TestApiKey ayarı bulunamadı.");
        }

        try
        {
            var messages = new[] { new OpenRouterMessage("user", request.Prompt) };
            return await _client.ChatAsync(apiKey, request.Model, messages);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, ex.Message);
        }
    }
}
