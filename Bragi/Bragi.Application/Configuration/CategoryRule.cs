namespace Bragi.Application.Configuration;

public sealed record CategoryRule
{
    public string Key { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string OutputFileName { get; init; } = string.Empty;

    public List<string> IncludeKeywords { get; init; } = [];

    public List<string> ExcludeKeywords { get; init; } = [];

    public List<string> RequireAnyKeywords { get; init; } = [];

    public bool DisableForFiction { get; init; }

    public bool DisableForJuvenile { get; init; }

    public CategoryMatchMode IncludeMatchMode { get; init; } = CategoryMatchMode.Contains;

    public CategoryMatchMode ExcludeMatchMode { get; init; } = CategoryMatchMode.Contains;

    public CategoryMatchMode RequireAnyMatchMode { get; init; } = CategoryMatchMode.Contains;

    public int SortOrder { get; init; }

    public bool Enabled { get; init; } = true;
}
