using Bragi.Application.Configuration;
using Bragi.Application.Contracts;
using Bragi.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;

namespace Bragi.Infrastructure.Ingestion;

public sealed class InputIngestService : IInputIngestService
{
    private readonly ILogger<InputIngestService> _logger;

    public InputIngestService(ILogger<InputIngestService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<InputFileKind> DetectInputFileKindAsync(
        string sourceFilePath,
        InputOptions inputOptions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentException("Source file path cannot be null, empty, or whitespace.", nameof(sourceFilePath));
        }

        ArgumentNullException.ThrowIfNull(inputOptions);

        var extension = Path.GetExtension(sourceFilePath);

        if (!string.IsNullOrWhiteSpace(extension))
        {
            if (inputOptions.PlainTextExtensions.Any(value =>
                    string.Equals(NormalizeExtension(value), extension, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation(
                    "Detected plain text input from extension. SourceFilePath={SourceFilePath} Extension={Extension}",
                    sourceFilePath,
                    extension);

                return Task.FromResult(InputFileKind.PlainText);
            }

            if (inputOptions.CsvExtensions.Any(value =>
                    string.Equals(NormalizeExtension(value), extension, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation(
                    "Detected CSV input from extension. SourceFilePath={SourceFilePath} Extension={Extension}",
                    sourceFilePath,
                    extension);

                return Task.FromResult(InputFileKind.Csv);
            }
        }

        if (inputOptions.TreatUnknownExtensionAsPlainText)
        {
            _logger.LogWarning(
                "Unknown extension treated as plain text. SourceFilePath={SourceFilePath} Extension={Extension}",
                sourceFilePath,
                extension);

            return Task.FromResult(InputFileKind.PlainText);
        }

        _logger.LogWarning(
            "Unable to detect supported input kind. SourceFilePath={SourceFilePath} Extension={Extension}",
            sourceFilePath,
            extension);

        return Task.FromResult(InputFileKind.Unknown);
    }

    public async Task<string> ReadAllTextAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default)
    {
        EnsureReadableFile(sourceFilePath);

        _logger.LogInformation("Reading plain text input from {SourceFilePath}.", sourceFilePath);

        return await File.ReadAllTextAsync(sourceFilePath, cancellationToken);
    }

    public Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ReadCsvRowsAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default)
    {
        EnsureReadableFile(sourceFilePath);

        _logger.LogInformation("Reading CSV input from {SourceFilePath}.", sourceFilePath);

        using var parser = new TextFieldParser(sourceFilePath);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = false;

        if (parser.EndOfData)
        {
            throw new InvalidOperationException("CSV file does not contain a header row.");
        }

        var rawHeaders = parser.ReadFields();

        if (rawHeaders is null || rawHeaders.Length == 0)
        {
            throw new InvalidOperationException("CSV header row could not be read.");
        }

        var headers = BuildHeaders(rawHeaders);
        var rows = new List<IReadOnlyDictionary<string, string?>>();
        var csvRowNumber = 2;

        while (!parser.EndOfData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fields = parser.ReadFields();

                if (fields is null)
                {
                    _logger.LogWarning(
                        "Skipping CSV row {CsvRowNumber} in {SourceFilePath} because it could not be read.",
                        csvRowNumber,
                        sourceFilePath);

                    continue;
                }

                rows.Add(BuildRow(headers, fields));
            }
            catch (MalformedLineException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Skipping malformed CSV row {CsvRowNumber} in {SourceFilePath}.",
                    csvRowNumber,
                    sourceFilePath);
            }
            finally
            {
                csvRowNumber++;
            }
        }

        _logger.LogInformation(
            "Read {RowCount} CSV data rows from {SourceFilePath}.",
            rows.Count,
            sourceFilePath);

        return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, string?>>>(rows);
    }

    private static string NormalizeExtension(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmedValue = value.Trim();
        return trimmedValue.StartsWith('.') ? trimmedValue : $".{trimmedValue}";
    }

    private static IReadOnlyList<string> BuildHeaders(string[] rawHeaders)
    {
        var headers = new List<string>(rawHeaders.Length);
        var duplicateTracker = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rawHeaders.Length; i++)
        {
            var header = string.IsNullOrWhiteSpace(rawHeaders[i])
                ? $"Column{i + 1}"
                : rawHeaders[i].Trim();

            if (duplicateTracker.TryGetValue(header, out var currentCount))
            {
                currentCount++;
                duplicateTracker[header] = currentCount;
                header = $"{header}__{currentCount}";
            }
            else
            {
                duplicateTracker[header] = 1;
            }

            headers.Add(header);
        }

        return headers;
    }

    private static IReadOnlyDictionary<string, string?> BuildRow(
        IReadOnlyList<string> headers,
        IReadOnlyList<string> fields)
    {
        var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < headers.Count; i++)
        {
            row[headers[i]] = i < fields.Count ? fields[i] : null;
        }

        return row;
    }

    private static void EnsureReadableFile(string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentException("Source file path cannot be null, empty, or whitespace.", nameof(sourceFilePath));
        }

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Source file was not found.", sourceFilePath);
        }
    }
}
