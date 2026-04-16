using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bragi.Application.Configuration;
using Bragi.Application.Contracts;
using Bragi.Domain.Enums;
using Bragi.Domain.Models;
using Bragi.Domain.Results;
using Bragi.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Bragi.Infrastructure.Extraction;

public sealed class SubjectExtractionService : ISubjectExtractionService
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private readonly ILogger<SubjectExtractionService> _logger;

    public SubjectExtractionService(ILogger<SubjectExtractionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ExtractionResult> ExtractFromPlainTextAsync(
        string sourceFilePath,
        string textContent,
        InputOptions inputOptions,
        BehaviorOptions behaviorOptions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentException("Source file path cannot be null, empty, or whitespace.", nameof(sourceFilePath));
        }

        ArgumentNullException.ThrowIfNull(textContent);
        ArgumentNullException.ThrowIfNull(inputOptions);
        ArgumentNullException.ThrowIfNull(behaviorOptions);

        _logger.LogInformation(
            "Starting plain text subject extraction. SourceFilePath={SourceFilePath}",
            sourceFilePath);

        var extractedSubjects = new List<ExtractedSubject>();
        var seenSubjects = new HashSet<string>(StringComparer.Ordinal);
        var totalRecordsRead = 0;
        var blankOrIgnoredCount = 0;
        var duplicateCount = 0;
        var sequenceNumber = 1;

        using var reader = new StringReader(textContent);

        string? line;
        var sourceRowNumber = 0;

        while ((line = reader.ReadLine()) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            sourceRowNumber++;
            totalRecordsRead++;

            var preparedOriginal = PrepareOriginalSubject(line, behaviorOptions);

            if (string.IsNullOrWhiteSpace(preparedOriginal))
            {
                if (behaviorOptions.IgnoreBlankSubjects)
                {
                    blankOrIgnoredCount++;
                    continue;
                }

                blankOrIgnoredCount++;
                continue;
            }

            var normalized = PrepareNormalizedSubject(preparedOriginal, behaviorOptions);

            if (!seenSubjects.Add(normalized))
            {
                duplicateCount++;
            }

            var entry = new SubjectEntry(
                new SubjectText(preparedOriginal),
                new NormalizedSubjectText(normalized),
                sourceFilePath,
                sourceRowNumber,
                null,
                null,
                InputFileKind.PlainText);

            extractedSubjects.Add(new ExtractedSubject(entry, sequenceNumber));
            sequenceNumber++;
        }

        _logger.LogInformation(
            "Extracted {ExtractedCount} subjects from plain text input {SourceFilePath}. Blank ignored: {BlankCount}. Duplicates observed: {DuplicateCount}.",
            extractedSubjects.Count,
            sourceFilePath,
            blankOrIgnoredCount,
            duplicateCount);

        return Task.FromResult(new ExtractionResult(
            sourceFilePath,
            InputFileKind.PlainText,
            extractedSubjects,
            totalRecordsRead,
            blankOrIgnoredCount,
            duplicateCount,
            parseWarningCount: 0));
    }

    public Task<ExtractionResult> ExtractFromCsvAsync(
        string sourceFilePath,
        IReadOnlyList<IReadOnlyDictionary<string, string?>> rows,
        CsvColumns csvColumns,
        InputOptions inputOptions,
        BehaviorOptions behaviorOptions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new ArgumentException("Source file path cannot be null, empty, or whitespace.", nameof(sourceFilePath));
        }

        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(csvColumns);
        ArgumentNullException.ThrowIfNull(inputOptions);
        ArgumentNullException.ThrowIfNull(behaviorOptions);

        if (string.IsNullOrWhiteSpace(csvColumns.SubjectColumnName))
        {
            throw new InvalidOperationException("CSV subject column name is missing.");
        }

        if (rows.Count > 0 && !rows[0].ContainsKey(csvColumns.SubjectColumnName))
        {
            throw new InvalidOperationException(
                $"Configured CSV subject column '{csvColumns.SubjectColumnName}' was not found in the input rows.");
        }

        _logger.LogInformation(
            "Starting CSV subject extraction. SourceFilePath={SourceFilePath} SubjectColumnName={SubjectColumnName}",
            sourceFilePath,
            csvColumns.SubjectColumnName);

        var extractedSubjects = new List<ExtractedSubject>();
        var seenSubjects = new HashSet<string>(StringComparer.Ordinal);
        var totalRecordsRead = rows.Count;
        var blankOrIgnoredCount = 0;
        var duplicateCount = 0;
        var parseWarningCount = 0;
        var sequenceNumber = 1;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = rows[rowIndex];
            var sourceRowNumber = rowIndex + 2;

            if (!row.TryGetValue(csvColumns.SubjectColumnName, out var rawSubjectValue))
            {
                parseWarningCount++;
                _logger.LogWarning(
                    "CSV row {SourceRowNumber} in {SourceFilePath} is missing configured subject column '{SubjectColumnName}'.",
                    sourceRowNumber,
                    sourceFilePath,
                    csvColumns.SubjectColumnName);
                continue;
            }

            var sourceTitle = inputOptions.CaptureCsvSourceTitle
                ? GetOptionalValue(row, csvColumns.TitleColumnName)
                : null;

            var sourceRecordId = inputOptions.CaptureCsvSourceRecordId
                ? GetOptionalValue(row, csvColumns.RecordIdColumnName)
                : null;

            var subjectValues = ExtractSubjectValues(
                rawSubjectValue,
                csvColumns.SubjectColumnContainsJsonArray,
                sourceFilePath,
                sourceRowNumber,
                ref parseWarningCount);

            if (subjectValues.Count == 0)
            {
                blankOrIgnoredCount++;
                continue;
            }

            foreach (var subjectValue in subjectValues)
            {
                var preparedOriginal = PrepareOriginalSubject(subjectValue, behaviorOptions);

                if (string.IsNullOrWhiteSpace(preparedOriginal))
                {
                    blankOrIgnoredCount++;
                    continue;
                }

                var normalized = PrepareNormalizedSubject(preparedOriginal, behaviorOptions);

                if (!seenSubjects.Add(normalized))
                {
                    duplicateCount++;
                }

                var entry = new SubjectEntry(
                    new SubjectText(preparedOriginal),
                    new NormalizedSubjectText(normalized),
                    sourceFilePath,
                    sourceRowNumber,
                    sourceTitle,
                    sourceRecordId,
                    InputFileKind.Csv);

                extractedSubjects.Add(new ExtractedSubject(entry, sequenceNumber));
                sequenceNumber++;
            }
        }

        _logger.LogInformation(
            "Extracted {ExtractedCount} subjects from CSV input {SourceFilePath}. Parse warnings: {ParseWarningCount}. Blank ignored: {BlankCount}. Duplicates observed: {DuplicateCount}.",
            extractedSubjects.Count,
            sourceFilePath,
            parseWarningCount,
            blankOrIgnoredCount,
            duplicateCount);

        return Task.FromResult(new ExtractionResult(
            sourceFilePath,
            InputFileKind.Csv,
            extractedSubjects,
            totalRecordsRead,
            blankOrIgnoredCount,
            duplicateCount,
            parseWarningCount));
    }

    private IReadOnlyList<string> ExtractSubjectValues(
        string? rawSubjectValue,
        bool subjectColumnContainsJsonArray,
        string sourceFilePath,
        int sourceRowNumber,
        ref int parseWarningCount)
    {
        if (string.IsNullOrWhiteSpace(rawSubjectValue))
        {
            return [];
        }

        var trimmedValue = rawSubjectValue.Trim();

        if (subjectColumnContainsJsonArray)
        {
            if (TryParseJsonSubjects(trimmedValue, out var jsonSubjects))
            {
                return jsonSubjects;
            }

            if (LooksLikeStructuredList(trimmedValue))
            {
                parseWarningCount++;
                _logger.LogWarning(
                    "Subject payload in row {SourceRowNumber} of {SourceFilePath} looked like a structured list but could not be parsed cleanly. Falling back to text splitting.",
                    sourceRowNumber,
                    sourceFilePath);
            }
        }

        if (LooksLikeBracketedList(trimmedValue))
        {
            var innerValue = trimmedValue[1..^1];
            var commaSplitSubjects = SplitAndClean(innerValue.Split(',', StringSplitOptions.None));

            if (commaSplitSubjects.Count > 1)
            {
                return commaSplitSubjects;
            }
        }

        var fallbackSubjects = SplitFallbackSubjectList(trimmedValue);

        if (fallbackSubjects.Count > 0)
        {
            return fallbackSubjects;
        }

        return [Unquote(trimmedValue)];
    }

    private static bool TryParseJsonSubjects(string value, out IReadOnlyList<string> subjects)
    {
        subjects = [];

        try
        {
            using var document = JsonDocument.Parse(value);

            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                var values = new List<string>();

                foreach (var element in document.RootElement.EnumerateArray())
                {
                    var candidate = element.ValueKind == JsonValueKind.String
                        ? element.GetString()
                        : element.ToString();

                    if (!string.IsNullOrWhiteSpace(candidate))
                    {
                        values.Add(candidate.Trim());
                    }
                }

                subjects = values;
                return true;
            }

            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                var singleValue = document.RootElement.GetString();

                if (!string.IsNullOrWhiteSpace(singleValue))
                {
                    subjects = [singleValue.Trim()];
                    return true;
                }
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool LooksLikeStructuredList(string value)
    {
        return value.StartsWith('[') && value.EndsWith(']');
    }

    private static bool LooksLikeBracketedList(string value)
    {
        return value.Length >= 2 &&
               value.StartsWith('[') &&
               value.EndsWith(']');
    }

    private static IReadOnlyList<string> SplitFallbackSubjectList(string value)
    {
        if (value.Contains('\n') || value.Contains('\r'))
        {
            return SplitAndClean(value
                .Split(["\r\n", "\n", "\r"], StringSplitOptions.None));
        }

        if (value.Contains(';'))
        {
            return SplitAndClean(value.Split(';', StringSplitOptions.None));
        }

        if (value.Contains('|'))
        {
            return SplitAndClean(value.Split('|', StringSplitOptions.None));
        }

        return [Unquote(value)];
    }

    private static IReadOnlyList<string> SplitAndClean(IEnumerable<string> values)
    {
        return values
            .Select(Unquote)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray();
    }

    private static string Unquote(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmedValue = value.Trim();

        if (trimmedValue.Length >= 2)
        {
            if ((trimmedValue.StartsWith('"') && trimmedValue.EndsWith('"')) ||
                (trimmedValue.StartsWith('\'') && trimmedValue.EndsWith('\'')))
            {
                return trimmedValue[1..^1].Trim();
            }
        }

        return trimmedValue;
    }

    private static string? GetOptionalValue(
        IReadOnlyDictionary<string, string?> row,
        string? columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return null;
        }

        return row.TryGetValue(columnName, out var value)
            ? value
            : null;
    }

    private static string PrepareOriginalSubject(
        string? value,
        BehaviorOptions behaviorOptions)
    {
        ArgumentNullException.ThrowIfNull(behaviorOptions);

        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var preparedValue = value;

        if (behaviorOptions.NormalizeWhitespace)
        {
            preparedValue = WhitespaceRegex.Replace(preparedValue, " ");
        }

        if (behaviorOptions.TrimSubjects)
        {
            preparedValue = preparedValue.Trim();
        }

        return preparedValue;
    }

    private static string PrepareNormalizedSubject(
        string originalValue,
        BehaviorOptions behaviorOptions)
    {
        ArgumentNullException.ThrowIfNull(behaviorOptions);

        var normalizedValue = originalValue;

        if (behaviorOptions.NormalizeWhitespace)
        {
            normalizedValue = WhitespaceRegex.Replace(normalizedValue, " ");
        }

        normalizedValue = normalizedValue.Trim();

        if (behaviorOptions.CaseInsensitiveMatching)
        {
            normalizedValue = normalizedValue.ToLowerInvariant();
        }

        return normalizedValue;
    }
}
