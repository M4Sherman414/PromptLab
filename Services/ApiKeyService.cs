using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PromptLab.Data;
using PromptLab.Models;

namespace PromptLab.Services;

public class ApiKeyService
{
    private readonly ApplicationDbContext _db;
    private readonly IDataProtector _protector;

    public ApiKeyService(ApplicationDbContext db, IDataProtectionProvider provider)
    {
        _db = db;
        _protector = provider.CreateProtector("PromptLab.UserApiKey.v1");
    }

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
