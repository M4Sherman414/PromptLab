using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PromptLab.Services;

public class OpenRouterClient
{
    private readonly HttpClient _http;

    public OpenRouterClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<OpenRouterModel>> GetModelsAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<OpenRouterModelList>("models",ct);
        return result?.Data ?? new List<OpenRouterModel>();
    }

    public async Task<OpenRouterChatResult> ChatAsync(
        string apiKey,
        string model,
        IEnumerable<OpenRouterMessage> messages,
        CancellationToken ct = default)
    {
        using var request = BuildChatRequest(apiKey, model, messages, stream: false);
        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);

        var body = await response.Content.ReadFromJsonAsync<OpenRouterChatResponse>(ct) 
        ?? throw new InvalidOperationException("OpenRouter boş bir cevap döndürdü.");

        var choice = body.Choices.FirstOrDefault();

        return new OpenRouterChatResult
        {
            Content = choice?.Message?.Content ?? string.Empty,
            Model = body.Model,
            FinishReason = choice?.FinishReason,
            PromptTokens = body.Usage?.PromptTokens ?? 0,
            CompletionTokens = body.Usage?.CompletionTokens ?? 0,
            TotalTokens = body.Usage?.TotalTokens ?? 0,
            Cost = body.Usage?.Cost
        };
    }

    private static HttpRequestMessage BuildChatRequest(string apiKey, string model, IEnumerable<OpenRouterMessage> messages,
    bool stream)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(new { model, messages, stream })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorBody = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(
            $"OpenRouter hatası ({(int)response.StatusCode}):{errorBody}",
            null,
            response.StatusCode);
    }
}