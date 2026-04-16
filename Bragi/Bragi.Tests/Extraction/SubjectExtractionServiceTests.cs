using Bragi.Application.Configuration;
using Bragi.Domain.Enums;
using Bragi.Infrastructure.Extraction;
using Bragi.Infrastructure.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bragi.Tests.Extraction;

public sealed class SubjectExtractionServiceTests
{
    [Fact]
    public async Task ExtractFromPlainTextAsync_ExtractsSubjects_IgnoresBlankLines_AndCountsDuplicates()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");

        try
        {
            await File.WriteAllTextAsync(
                filePath,
                "Art\r\n\r\nPainting\r\nArt\r\n");

            var inputIngestService = new InputIngestService(NullLogger<InputIngestService>.Instance);
            var subjectExtractionService = new SubjectExtractionService(NullLogger<SubjectExtractionService>.Instance);

            var detectedInputKind = await inputIngestService.DetectInputFileKindAsync(filePath, new InputOptions());
            var textContent = await inputIngestService.ReadAllTextAsync(filePath);

            var result = await subjectExtractionService.ExtractFromPlainTextAsync(
                filePath,
                textContent,
                new InputOptions(),
                new BehaviorOptions());

            Assert.Equal(InputFileKind.PlainText, detectedInputKind);
            Assert.Equal(InputFileKind.PlainText, result.InputFileKind);
            Assert.Equal(4, result.TotalRecordsRead);
            Assert.Equal(1, result.BlankOrIgnoredCount);
            Assert.Equal(1, result.DuplicateCount);
            Assert.Equal(0, result.ParseWarningCount);
            Assert.Equal(3, result.ExtractedCount);

            Assert.Equal(1, result.Subjects[0].Entry.SourceRowNumber);
            Assert.Equal(3, result.Subjects[1].Entry.SourceRowNumber);
            Assert.Equal(4, result.Subjects[2].Entry.SourceRowNumber);

            Assert.Equal("Art", result.Subjects[0].Entry.OriginalSubject.Value);
            Assert.Equal("art", result.Subjects[0].Entry.NormalizedSubject.Value);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task ExtractFromCsvAsync_SplitsSemicolonDelimitedSubjects_WhenJsonArrayModeIsDisabled()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");

        try
        {
            var csvContent =
                "instance.id,instance.title,instance.subjects\r\n" +
                "row-1,Title One,\"Art;Business;History\"\r\n";

            await File.WriteAllTextAsync(filePath, csvContent);

            var inputIngestService = new InputIngestService(NullLogger<InputIngestService>.Instance);
            var subjectExtractionService = new SubjectExtractionService(NullLogger<SubjectExtractionService>.Instance);

            var rows = await inputIngestService.ReadCsvRowsAsync(filePath);

            var result = await subjectExtractionService.ExtractFromCsvAsync(
                filePath,
                rows,
                new CsvColumns
                {
                    SubjectColumnName = "instance.subjects",
                    TitleColumnName = "instance.title",
                    RecordIdColumnName = "instance.id",
                    SubjectColumnContainsJsonArray = false
                },
                new InputOptions
                {
                    CaptureCsvSourceTitle = true,
                    CaptureCsvSourceRecordId = true
                },
                new BehaviorOptions());

            Assert.Equal(1, result.TotalRecordsRead);
            Assert.Equal(3, result.ExtractedCount);
            Assert.Equal("Art", result.Subjects[0].Entry.OriginalSubject.Value);
            Assert.Equal("Business", result.Subjects[1].Entry.OriginalSubject.Value);
            Assert.Equal("History", result.Subjects[2].Entry.OriginalSubject.Value);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task ExtractFromCsvAsync_ExtractsJsonArraySubjects_AndPreservesRowMetadata()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");

        try
        {
            var csvContent =
                "instance.id,instance.title,instance.subjects\r\n" +
                "row-1,Title One,\"[\"\"Art\"\",\"\"Biology\"\"]\"\r\n" +
                "row-2,Title Two,\"[\"\"Business\"\"]\"\r\n";

            await File.WriteAllTextAsync(filePath, csvContent);

            var inputIngestService = new InputIngestService(NullLogger<InputIngestService>.Instance);
            var subjectExtractionService = new SubjectExtractionService(NullLogger<SubjectExtractionService>.Instance);

            var detectedInputKind = await inputIngestService.DetectInputFileKindAsync(filePath, new InputOptions());
            var rows = await inputIngestService.ReadCsvRowsAsync(filePath);

            var result = await subjectExtractionService.ExtractFromCsvAsync(
                filePath,
                rows,
                new CsvColumns
                {
                    SubjectColumnName = "instance.subjects",
                    TitleColumnName = "instance.title",
                    RecordIdColumnName = "instance.id",
                    SubjectColumnContainsJsonArray = true
                },
                new InputOptions(),
                new BehaviorOptions());

            Assert.Equal(InputFileKind.Csv, detectedInputKind);
            Assert.Equal(InputFileKind.Csv, result.InputFileKind);
            Assert.Equal(2, result.TotalRecordsRead);
            Assert.Equal(0, result.BlankOrIgnoredCount);
            Assert.Equal(0, result.DuplicateCount);
            Assert.Equal(0, result.ParseWarningCount);
            Assert.Equal(3, result.ExtractedCount);

            Assert.Equal(2, result.Subjects[0].Entry.SourceRowNumber);
            Assert.Equal(2, result.Subjects[1].Entry.SourceRowNumber);
            Assert.Equal(3, result.Subjects[2].Entry.SourceRowNumber);

            Assert.Equal("Title One", result.Subjects[0].Entry.SourceTitle);
            Assert.Equal("row-1", result.Subjects[0].Entry.SourceRecordId);

            Assert.Equal("Art", result.Subjects[0].Entry.OriginalSubject.Value);
            Assert.Equal("Biology", result.Subjects[1].Entry.OriginalSubject.Value);
            Assert.Equal("Business", result.Subjects[2].Entry.OriginalSubject.Value);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

    }
}
