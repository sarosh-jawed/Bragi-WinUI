using Bragi.Application.Configuration;
using Bragi.Domain.Results;
using Bragi.Infrastructure.Categorization;
using Bragi.Infrastructure.Configuration;
using Bragi.Infrastructure.Extraction;
using Bragi.Infrastructure.Export;
using Bragi.Infrastructure.Ingestion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bragi.Tests.Regression;

public sealed class DummyCsvRegressionTests
{
    [Fact]
    public async Task DummyFixture_WithActualAppConfig_MatchesExpectedExtractionAndCategorizationTotals()
    {
        var config = LoadActualAppConfig();
        var fixturePath = GetFixturePath("bragi_dummy_lcc_instance_items.csv");

        var inputIngestService = new InputIngestService(NullLogger<InputIngestService>.Instance);
        var subjectExtractionService = new SubjectExtractionService(NullLogger<SubjectExtractionService>.Instance);

        var subjectNormalizationHelper = new SubjectNormalizationHelper();
        var keywordMatcher = new KeywordMatcher(subjectNormalizationHelper);
        var exclusionMatcher = new ExclusionMatcher(keywordMatcher);

        var categorizationService = new CategorizationService(
            NullLogger<CategorizationService>.Instance,
            subjectNormalizationHelper,
            keywordMatcher,
            exclusionMatcher);

        var inputKind = await inputIngestService.DetectInputFileKindAsync(fixturePath, config.InputOptions);
        var rows = await inputIngestService.ReadCsvRowsAsync(fixturePath);

        var extractionResult = await subjectExtractionService.ExtractFromCsvAsync(
            fixturePath,
            rows,
            config.CsvColumns,
            config.InputOptions,
            config.BehaviorOptions);

        var categorizationResult = await categorizationService.CategorizeAsync(
            extractionResult,
            config.CategoryRules,
            config.BehaviorOptions);

        Assert.Equal(Bragi.Domain.Enums.InputFileKind.Csv, inputKind);
        Assert.Equal(30, extractionResult.TotalRecordsRead);
        Assert.Equal(80, extractionResult.ExtractedCount);
        Assert.Equal(1, extractionResult.BlankOrIgnoredCount);
        Assert.Equal(10, extractionResult.DuplicateCount);
        Assert.Equal(0, extractionResult.ParseWarningCount);

        Assert.Equal(77, categorizationResult.TotalAssignments);
        Assert.Equal(6, categorizationResult.UncategorizedSubjectCount);

        var multiMatchCount = categorizationResult.CategorizedSubjects.Count(subject => subject.Matches.Count > 1);
        Assert.Equal(3, multiMatchCount);
    }

    [Fact]
    public async Task DummyFixture_WithActualAppConfig_ExportsExpectedFileSet()
    {
        var config = LoadActualAppConfig();
        var fixturePath = GetFixturePath("bragi_dummy_lcc_instance_items.csv");
        var tempOutputRoot = Path.Combine(Path.GetTempPath(), $"bragi-regression-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempOutputRoot);

            var inputIngestService = new InputIngestService(NullLogger<InputIngestService>.Instance);
            var subjectExtractionService = new SubjectExtractionService(NullLogger<SubjectExtractionService>.Instance);

            var subjectNormalizationHelper = new SubjectNormalizationHelper();
            var keywordMatcher = new KeywordMatcher(subjectNormalizationHelper);
            var exclusionMatcher = new ExclusionMatcher(keywordMatcher);

            var categorizationService = new CategorizationService(
                NullLogger<CategorizationService>.Instance,
                subjectNormalizationHelper,
                keywordMatcher,
                exclusionMatcher);

            var exportService = new TextExportService(
                NullLogger<TextExportService>.Instance,
                new TextBodyBuilder(),
                config);

            var rows = await inputIngestService.ReadCsvRowsAsync(fixturePath);

            var extractionResult = await subjectExtractionService.ExtractFromCsvAsync(
                fixturePath,
                rows,
                config.CsvColumns,
                config.InputOptions,
                config.BehaviorOptions);

            var categorizationResult = await categorizationService.CategorizeAsync(
                extractionResult,
                config.CategoryRules,
                config.BehaviorOptions);

            var outputOptions = config.Output with
            {
                RootPath = tempOutputRoot,
                CreateMonthlySubfolder = false
            };

            var exportResult = await exportService.ExportAsync(
                            categorizationResult,
                            new DateTimeOffset(2026, 4, 1, 12, 0, 1, TimeSpan.Zero),
                            outputOptions,
                            config.TextTemplate,
                            config.CategoryRules);

            var enabledRules = config.CategoryRules.Where(rule => rule.Enabled).ToArray();

            foreach (var rule in enabledRules)
            {
                var filePath = Path.Combine(tempOutputRoot, rule.OutputFileName);
                Assert.True(File.Exists(filePath), $"Expected output file to exist: {filePath}");
            }

            var uncategorizedPath = Path.Combine(tempOutputRoot, outputOptions.UncategorizedFileName);

            Assert.True(File.Exists(uncategorizedPath));
            Assert.True(exportResult.GeneratedFiles.Count >= enabledRules.Length + 1);
            Assert.Equal(exportResult.TotalExportedCategoryLines, exportResult.CategoryExportLineCounts.Values.Sum());
            Assert.True(exportResult.OutputsSorted);
            Assert.True(exportResult.OutputsDeduplicated);
        }
        finally
        {
            if (Directory.Exists(tempOutputRoot))
            {
                Directory.Delete(tempOutputRoot, recursive: true);
            }
        }
    }

    private static BragiConfig LoadActualAppConfig()
    {
        var bragiRoot = FindBragiRoot();
        var configPath = Path.Combine(bragiRoot, "Bragi.App.WinUI", "config.json");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .Build();

        var loader = new BragiConfigLoader(configuration, new PathTokenResolver());
        return loader.Load();
    }

    private static string GetFixturePath(string fileName)
    {
        var bragiRoot = FindBragiRoot();
        return Path.Combine(bragiRoot, "Bragi.Tests", "Fixtures", fileName);
    }

    private static string FindBragiRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "Bragi.sln");

            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Bragi solution root from the test output directory.");
    }
}
