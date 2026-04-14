namespace Bragi.Application.Configuration;

public sealed record BragiConfig
{
    public CsvColumns CsvColumns { get; init; } = new();

    public InputOptions InputOptions { get; init; } = new();

    public List<CategoryRule> CategoryRules { get; init; } = [];

    public Output Output { get; init; } = new();

    public TextTemplate TextTemplate { get; init; } = new();

    public BehaviorOptions BehaviorOptions { get; init; } = new();
}
