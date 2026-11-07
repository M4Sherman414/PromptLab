using System.Text.Json.Serialization;

namespace PromptLab.Services;

// Model listesi: GET /models
public class OpenRouterModelList
{
    [JsonPropertyName("data")]
    public List<OpenRouterModel> Data { get; set; } = new();
}

public class OpenRouterModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("pricing")]
    public OpenRouterPricing? Pricing { get; set; }
}

public class OpenRouterPricing
{
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("completion")]
    public string? Completion { get; set; }
}

// Cevap isteği: POST /chat/completions
public record OpenRouterMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

public class OpenRouterChatResponse
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenRouterChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public OpenRouterUsage? Usage { get; set; }
}

public class OpenRouterChoice
{
    [JsonPropertyName("message")]
    public OpenRouterMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class OpenRouterUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("cost")]
    public decimal? Cost { get; set; }
}

// Uygulamanın geri kalanına göre dönen sade sonuç
public class OpenRouterChatResult
{
    public string Content { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? FinishReason { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal? Cost { get; set; }
}