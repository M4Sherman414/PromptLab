namespace PromptLab.Models.ViewModels;

public enum HistoryItemKind
{
    Chat,
    Comparison
}

public class ChatHistoryItem
{
    public int Id { get; init; }
    public HistoryItemKind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ModelSummary { get; init; } = string.Empty;
    public bool IsFreeOnly { get; init; }
    public bool HasError { get; init; }
    public bool IsPending { get; init; }
    public DateTime ActivityAt { get; init; }
}

public class ChatHistoryViewModel
{
    public List<ChatHistoryItem> Items { get; init; } = new();
    public int ChatCount { get; init; }
    public int ComparisonCount { get; init; }
}
