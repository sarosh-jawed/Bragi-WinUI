using System.Collections.ObjectModel;
using Bragi.Domain.ValueObjects;

namespace Bragi.Domain.Results;

public sealed record ExportResult
{
    public ExportResult(
        string outputDirectory,
        IReadOnlyList<string> generatedFiles,
        IReadOnlyDictionary<CategoryKey, int> categoryExportLineCounts,
        int uncategorizedExportLineCount,
        int totalExportedCategoryLines,
        bool outputsSorted,
        bool outputsDeduplicated)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory cannot be null, empty, or whitespace.", nameof(outputDirectory));
        }

        ArgumentNullException.ThrowIfNull(generatedFiles);
        ArgumentNullException.ThrowIfNull(categoryExportLineCounts);

        if (generatedFiles.Any(path => string.IsNullOrWhiteSpace(path)))
        {
            throw new ArgumentException("Generated files cannot contain null, empty, or whitespace values.", nameof(generatedFiles));
        }

        if (categoryExportLineCounts.Any(pair => pair.Value < 0))
        {
            throw new ArgumentException("Category export line counts cannot contain negative values.", nameof(categoryExportLineCounts));
        }

        if (uncategorizedExportLineCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(uncategorizedExportLineCount), "Uncategorized export line count cannot be negative.");
        }

        if (totalExportedCategoryLines < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalExportedCategoryLines), "Total exported category line count cannot be negative.");
        }

        OutputDirectory = outputDirectory.Trim();
        GeneratedFiles = generatedFiles
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .ToArray();

        CategoryExportLineCounts = new ReadOnlyDictionary<CategoryKey, int>(
            new Dictionary<CategoryKey, int>(categoryExportLineCounts));

        UncategorizedExportLineCount = uncategorizedExportLineCount;
        TotalExportedCategoryLines = totalExportedCategoryLines;
        OutputsSorted = outputsSorted;
        OutputsDeduplicated = outputsDeduplicated;
    }

    public string OutputDirectory { get; }

    public IReadOnlyList<string> GeneratedFiles { get; }

    public IReadOnlyDictionary<CategoryKey, int> CategoryExportLineCounts { get; }

    public int UncategorizedExportLineCount { get; }

    public int TotalExportedCategoryLines { get; }

    public bool OutputsSorted { get; }

    public bool OutputsDeduplicated { get; }
}
