using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PromptLab.Data;
using PromptLab.Models;

namespace PromptLab.Services;

public record ChatTurnResult(ChatMessage UserMessage, ChatMessage AssistantMessage);

public class MissingApiKeyException : InvalidOperationException
{
    public MissingApiKeyException()
        : base("Sohbet edebilmek için önce bir OpenRouter API anahtarı kaydetmelisiniz.")
    {
    }
}

public class ChatService
{
    private readonly ApplicationDbContext _db;
    private readonly OpenRouterClient _client;
    private readonly ApiKeyService _apiKeys;

    public ChatService(ApplicationDbContext db, OpenRouterClient client, ApiKeyService apiKeys)
    {
        _db = db;
        _client = client;
        _apiKeys = apiKeys;
    }

    public Task<List<ModelCatalog>> GetActiveModelsAsync(CancellationToken ct = default)
    {
        return _db.ModelCatalogs
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderByDescending(m => m.IsFree)
            .ThenBy(m => m.DisplayName)
            .ToListAsync(ct);
    }

    public Task<List<ChatSession>> GetUserSessionsAsync(string userId, CancellationToken ct = default)
    {
        return _db.ChatSessions
            .AsNoTracking()
            .Include(s => s.ModelCatalog)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(ct);
    }

    public Task<ChatSession?> GetSessionAsync(int sessionId, string userId, CancellationToken ct = default)
    {
        return _db.ChatSessions
            .AsNoTracking()
            .Include(s => s.ModelCatalog)
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);
    }

    public async Task<bool> HasApiKeyAsync(string userId)
    {
        return !string.IsNullOrWhiteSpace(await _apiKeys.GetDecryptedKeyAsync(userId));
    }

    public async Task<ChatSession> CreateSessionAsync(
        string userId,
        int modelId,
        string? systemPrompt = null,
        CancellationToken ct = default)
    {
        var model = await _db.ModelCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == modelId && m.IsActive, ct);

        if (model is null)
        {
            throw new InvalidOperationException("Seçilen model bulunamadı veya aktif değil.");
        }

        var now = DateTime.UtcNow;
        var session = new ChatSession
        {
            UserId = userId,
            ModelId = modelId,
            Title = "Yeni sohbet",
            SystemPrompt = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        session.ModelCatalog = model;
        return session;
    }

    public async Task<ChatTurnResult> SendMessageAsync(
        int sessionId,
        string userId,
        string userPrompt,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            throw new ArgumentException("Mesaj boş olamaz.", nameof(userPrompt));
        }

        var session = await _db.ChatSessions
            .Include(s => s.ModelCatalog)
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);

        if (session is null || session.ModelCatalog is null)
        {
            throw new KeyNotFoundException("Sohbet oturumu bulunamadı.");
        }

        var apiKey = await _apiKeys.GetDecryptedKeyAsync(userId);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new MissingApiKeyException();
        }

        var trimmedPrompt = userPrompt.Trim();
        var requestMessages = BuildOpenRouterMessages(session, trimmedPrompt);

        var stopwatch = Stopwatch.StartNew();
        var result = await _client.ChatAsync(apiKey, session.ModelCatalog.OpenRouterModelId, requestMessages, ct);
        stopwatch.Stop();

        var userCreatedAt = DateTime.UtcNow;
        var userMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = ChatRole.User,
            Content = trimmedPrompt,
            CreatedAt = userCreatedAt
        };

        var assistantCreatedAt = DateTime.UtcNow;
        if (assistantCreatedAt <= userCreatedAt)
        {
            assistantCreatedAt = userCreatedAt.AddTicks(1);
        }

        var estimatedCost = result.Cost
            ?? CalculateEstimatedCost(session.ModelCatalog, result.PromptTokens, result.CompletionTokens);

        var assistantMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = ChatRole.Assistant,
            Content = result.Content,
            InputTokens = result.PromptTokens,
            OutputTokens = result.CompletionTokens,
            LatencyMs = (int)stopwatch.ElapsedMilliseconds,
            EstimatedCost = estimatedCost,
            CreatedAt = assistantCreatedAt
        };

        if (session.Messages.Count == 0 && session.Title == "Yeni sohbet")
        {
            session.Title = BuildTitle(trimmedPrompt);
        }

        session.UpdatedAt = assistantCreatedAt;

        _db.ChatMessages.Add(userMessage);
        _db.ChatMessages.Add(assistantMessage);
        await _db.SaveChangesAsync(ct);

        return new ChatTurnResult(userMessage, assistantMessage);
    }

    private static List<OpenRouterMessage> BuildOpenRouterMessages(ChatSession session, string newUserPrompt)
    {
        var messages = new List<OpenRouterMessage>();

        if (!string.IsNullOrWhiteSpace(session.SystemPrompt))
        {
            messages.Add(new OpenRouterMessage("system", session.SystemPrompt));
        }

        foreach (var message in session.Messages.OrderBy(m => m.CreatedAt).ThenBy(m => m.Id))
        {
            messages.Add(new OpenRouterMessage(ToOpenRouterRole(message.Role), message.Content));
        }

        messages.Add(new OpenRouterMessage("user", newUserPrompt));
        return messages;
    }

    private static string ToOpenRouterRole(ChatRole role) => role switch
    {
        ChatRole.System => "system",
        ChatRole.User => "user",
        ChatRole.Assistant => "assistant",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Desteklenmeyen sohbet rolü.")
    };

    private static decimal CalculateEstimatedCost(ModelCatalog model, int promptTokens, int completionTokens)
    {
        var inputCost = (promptTokens / 1_000_000m) * model.InputPricePerMillion;
        var outputCost = (completionTokens / 1_000_000m) * model.OutputPricePerMillion;
        return inputCost + outputCost;
    }

    private static string BuildTitle(string prompt)
    {
        var singleLine = string.Join(' ', prompt.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (singleLine.Length <= 60)
        {
            return singleLine;
        }

        return singleLine[..57] + "...";
    }
}
