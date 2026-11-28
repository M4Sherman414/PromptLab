using System.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using PromptLab.Data;
using PromptLab.Models;

namespace PromptLab.Services;

public record ComparisonRequest(
    int ModelAId,
    int ModelBId,
    string UserPrompt,
    string? SystemPrompt = null,
    double? Temperature = null,
    int? MaxTokens = null,
    bool IsBlind = false);

public class ComparisonService
{
    private readonly ApplicationDbContext _db;
    private readonly OpenRouterClient _client;
    private readonly ApiKeyService _apiKeys;

    public ComparisonService(ApplicationDbContext db, OpenRouterClient client, ApiKeyService apiKeys)
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

    public Task<Comparison?> GetComparisonAsync(int comparisonId, string userId, CancellationToken ct = default)
    {
        return _db.Comparisons
            .AsNoTracking()
            .Include(c => c.ComparisonResults.OrderBy(r => r.Slot))
                .ThenInclude(r => r.ModelCatalog)
            .Include(c => c.Vote)
            .FirstOrDefaultAsync(c => c.Id == comparisonId && c.UserId == userId, ct);
    }

    /// <summary>
    /// Aynı prompt'u iki aktif modele paralel olarak gönderir, gelen cevapları ve ölçümleri
    /// Comparison + iki adet ComparisonResult kaydı olarak veritabanına yazar.
    /// </summary>
    public async Task<Comparison> CreateComparisonAsync(
        string userId,
        ComparisonRequest request,
        CancellationToken ct = default)
    {
        ValidateRequest(request);

        var models = await _db.ModelCatalogs
            .AsNoTracking()
            .Where(m => m.IsActive && (m.Id == request.ModelAId || m.Id == request.ModelBId))
            .ToDictionaryAsync(m => m.Id, ct);

        if (!models.TryGetValue(request.ModelAId, out var modelA) ||
            !models.TryGetValue(request.ModelBId, out var modelB))
        {
            throw new InvalidOperationException("Seçilen modellerden biri bulunamadı veya aktif değil.");
        }

        var apiKey = await _apiKeys.GetDecryptedKeyAsync(userId);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new MissingApiKeyException();
        }

        var trimmedPrompt = request.UserPrompt.Trim();
        var trimmedSystem = string.IsNullOrWhiteSpace(request.SystemPrompt)
            ? null
            : request.SystemPrompt.Trim();

        var messages = BuildMessages(trimmedSystem, trimmedPrompt);

        // DbContext thread-safe olmadığı için paralel görevler yalnızca HTTP çağrısı yapar;
        // veritabanı kaydı iki görev de bittikten sonra tek seferde yapılır.
        var taskA = ExecuteModelAsync(userId, apiKey, modelA, ResultSlot.A, messages, request.Temperature, request.MaxTokens, ct);
        var taskB = ExecuteModelAsync(userId, apiKey, modelB, ResultSlot.B, messages, request.Temperature, request.MaxTokens, ct);
        var results = await Task.WhenAll(taskA, taskB);

        var comparison = new Comparison
        {
            UserId = userId,
            SystemPrompt = trimmedSystem,
            UserPrompt = trimmedPrompt,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            IsBlind = request.IsBlind,
            CreatedAt = DateTime.UtcNow,
            ComparisonResults = results.ToList()
        };

        _db.Comparisons.Add(comparison);
        await _db.SaveChangesAsync(ct);

        foreach (var result in comparison.ComparisonResults)
        {
            result.ModelCatalog = result.ModelId == modelA.Id ? modelA : modelB;
        }

        return comparison;
    }

    private async Task<ComparisonResult> ExecuteModelAsync(
        string userId,
        string apiKey,
        ModelCatalog model,
        ResultSlot slot,
        IReadOnlyList<OpenRouterMessage> messages,
        double? temperature,
        int? maxTokens,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await _client.ChatAsync(
                apiKey,
                model.OpenRouterModelId,
                messages,
                ct,
                temperature,
                maxTokens);

            stopwatch.Stop();
            var elapsedMs = (int)stopwatch.ElapsedMilliseconds;
            var estimatedCost = response.Cost
                ?? CalculateEstimatedCost(model, response.PromptTokens, response.CompletionTokens);

            double? tokensPerSecond = elapsedMs > 0 && response.CompletionTokens > 0
                ? Math.Round(response.CompletionTokens / (elapsedMs / 1000.0), 2)
                : null;

            return new ComparisonResult
            {
                ModelId = model.Id,
                Slot = slot,
                Output = response.Content,
                LatencyMs = elapsedMs,
                InputTokens = response.PromptTokens,
                OutputTokens = response.CompletionTokens,
                TokensPerSecond = tokensPerSecond,
                EstimatedCost = estimatedCost
            };
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            if (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _apiKeys.ForgetCheck(userId);
            }

            return new ComparisonResult
            {
                ModelId = model.Id,
                Slot = slot,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                Error = FormatHttpError(ex)
            };
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new ComparisonResult
            {
                ModelId = model.Id,
                Slot = slot,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                Error = "Model isteği zaman aşımına uğradı."
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            return new ComparisonResult
            {
                ModelId = model.Id,
                Slot = slot,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                Error = $"Model yanıtı alınırken beklenmeyen bir hata oluştu: {ex.Message}"
            };
        }
    }

    private static void ValidateRequest(ComparisonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserPrompt))
        {
            throw new ArgumentException("Prompt boş olamaz.", nameof(request));
        }

        if (request.ModelAId == request.ModelBId)
        {
            throw new ArgumentException("Karşılaştırma için iki farklı model seçmelisiniz.", nameof(request));
        }

        if (request.Temperature is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Temperature değeri 0 ile 2 arasında olmalıdır.");
        }

        if (request.MaxTokens is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Maksimum token sayısı 0'dan büyük olmalıdır.");
        }
    }

    private static List<OpenRouterMessage> BuildMessages(string? systemPrompt, string userPrompt)
    {
        var messages = new List<OpenRouterMessage>(2);
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new OpenRouterMessage("system", systemPrompt));
        }

        messages.Add(new OpenRouterMessage("user", userPrompt));
        return messages;
    }

    private static decimal CalculateEstimatedCost(ModelCatalog model, int promptTokens, int completionTokens)
    {
        var inputCost = (promptTokens / 1_000_000m) * model.InputPricePerMillion;
        var outputCost = (completionTokens / 1_000_000m) * model.OutputPricePerMillion;
        return inputCost + outputCost;
    }

    private static string FormatHttpError(HttpRequestException ex) => ex.StatusCode switch
    {
        HttpStatusCode.Unauthorized => "OpenRouter API anahtarınızı tanımadı (401). Lütfen anahtarınızı kontrol edin.",
        HttpStatusCode.NotFound => "Seçilen model OpenRouter üzerinde bulunamadı (404).",
        HttpStatusCode.TooManyRequests => "Model şu anda istek limitine ulaştı (429). Lütfen biraz sonra tekrar deneyin.",
        _ => $"OpenRouter isteği başarısız oldu: {ex.Message}"
    };
}
