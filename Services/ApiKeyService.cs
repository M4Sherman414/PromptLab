using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PromptLab.Data;
using PromptLab.Models;

namespace PromptLab.Services;

public enum ApiKeyStatus
{
    Missing,   // kayıtlı anahtar yok
    Valid,     // OpenRouter anahtarı tanıdı
    Invalid,   // OpenRouter anahtarı tanımadı (401)
    Unknown    // OpenRouter'a ulaşılamadı, durum bilinmiyor
}

public record ApiKeyCheck(ApiKeyStatus Status, OpenRouterKeyInfo? Info = null);

public class ApiKeyService
{
    private static readonly TimeSpan CheckCacheDuration = TimeSpan.FromMinutes(10);

    private readonly ApplicationDbContext _db;
    private readonly IDataProtector _protector;
    private readonly OpenRouterClient _openRouter;
    private readonly IMemoryCache _cache;

    public ApiKeyService(
        ApplicationDbContext db,
        IDataProtectionProvider provider,
        OpenRouterClient openRouter,
        IMemoryCache cache)
    {
        _db = db;
        _protector = provider.CreateProtector("PromptLab.UserApiKey.v1");
        _openRouter = openRouter;
        _cache = cache;
    }

    private static string CacheKey(string userId) => $"apikey-check:{userId}";

    /// <summary>
    /// Kaydedilmeden önce anahtarı OpenRouter'a sorar (GET /key, kredi harcamaz).
    /// </summary>
    public async Task<ApiKeyCheck> ValidateAsync(string plainKey, CancellationToken ct = default)
    {
        try
        {
            var info = await _openRouter.GetKeyInfoAsync(plainKey, ct);
            return info is null ? new ApiKeyCheck(ApiKeyStatus.Invalid) : new ApiKeyCheck(ApiKeyStatus.Valid, info);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ApiKeyCheck(ApiKeyStatus.Unknown);
        }
    }

    /// <summary>
    /// Kullanıcının kayıtlı anahtarının OpenRouter'daki durumu. Sonuç kısa süre önbellekte tutulur;
    /// anahtar kaydedilince/silinince ya da sohbette 401 alınınca temizlenir.
    /// </summary>
    public async Task<ApiKeyCheck> CheckStoredKeyAsync(string userId, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey(userId), out ApiKeyCheck? cached) && cached is not null)
        {
            return cached;
        }

        var key = await GetDecryptedKeyAsync(userId);
        if (string.IsNullOrWhiteSpace(key))
        {
            return new ApiKeyCheck(ApiKeyStatus.Missing);
        }

        var result = await ValidateAsync(key, ct);
        if (result.Status != ApiKeyStatus.Unknown)
        {
            _cache.Set(CacheKey(userId), result, CheckCacheDuration);
        }

        return result;
    }

    public void ForgetCheck(string userId) => _cache.Remove(CacheKey(userId));

    public Task<UserApiKey?> GetAsync(string userId)
    {
        return _db.UserApiKeys.AsNoTracking().FirstOrDefaultAsync(k => k.UserId == userId);
    }

    public async Task SaveAsync(string userId, string plainKey)
    {
        plainKey = plainKey.Trim();

        var entity = await _db.UserApiKeys.FirstOrDefaultAsync(k => k.UserId == userId);
        if (entity is null)
        {
            entity = new UserApiKey { UserId = userId };
            _db.UserApiKeys.Add(entity);
        }

        entity.EncryptedKey = _protector.Protect(plainKey);
        entity.LastFour = plainKey[^4..];
        entity.CreatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        ForgetCheck(userId);
    }

    public async Task DeleteAsync(string userId)
    {
        var entity = await _db.UserApiKeys.FirstOrDefaultAsync(k => k.UserId == userId);
        if (entity is null)
        {
            return;
        }

        _db.UserApiKeys.Remove(entity);
        await _db.SaveChangesAsync();
        ForgetCheck(userId);
    }

    public async Task<string?> GetDecryptedKeyAsync(string userId)
    {
        var entity = await GetAsync(userId);
        if (entity is null)
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(entity.EncryptedKey);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
