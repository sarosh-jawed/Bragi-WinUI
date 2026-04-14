using Bragi.Application.Configuration;
using Bragi.Domain.Enums;
using Bragi.Domain.Models;
using Bragi.Domain.Results;
using Bragi.Domain.ValueObjects;
using Bragi.Infrastructure.Categorization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bragi.Tests.Categorization;

public sealed class CategorizationServiceTests
{
    [Fact]
    public async Task CategorizeAsync_AllowsMultiMatch_AndNormalizesCasePunctuationAndWhitespace()
    {
        var service = CreateService();

        var extractionResult = CreateExtractionResult(
            CreateExtractedSubject(1, "  ART,   history!!  ", 2));

        var categoryRules = new[]
        {
            CreateRule("art", "ArtSubjects.txt", includeKeywords: ["art"], sortOrder: 10),
            CreateRule("history", "HistorySubjects.txt", includeKeywords: ["history"], sortOrder: 20)
        };

        var result = await service.CategorizeAsync(
            extractionResult,
            categoryRules,
            new BehaviorOptions { AllowMultiMatch = true });

        Assert.Equal(1, result.CategorizedSubjectCount);
        Assert.Equal(0, result.UncategorizedSubjectCount);
        Assert.Equal(2, result.TotalAssignments);

        var categorizedSubject = result.CategorizedSubjects.Single();
        Assert.Equal(2, categorizedSubject.AssignmentCount);
        Assert.Contains(categorizedSubject.Matches, match => match.CategoryKey.Value == "art");
        Assert.Contains(categorizedSubject.Matches, match => match.CategoryKey.Value == "history");
        Assert.Contains(categorizedSubject.Matches, match => match.Reason == "Matched include keyword: art");
        Assert.Contains(categorizedSubject.Matches, match => match.Reason == "Matched include keyword: history");
    }

    [Fact]
    public async Task CategorizeAsync_RoutesToUncategorized_WhenNoConfiguredCategoryMatches()
    {
        var service = CreateService();

        var extractionResult = CreateExtractionResult(
            CreateExtractedSubject(1, "Astronomy", 2));

        var categoryRules = new[]
        {
            CreateRule("art", "ArtSubjects.txt", includeKeywords: ["art"])
        };

        var result = await service.CategorizeAsync(
            extractionResult,
            categoryRules,
            new BehaviorOptions());

        Assert.Equal(0, result.CategorizedSubjectCount);
        Assert.Equal(1, result.UncategorizedSubjectCount);
        Assert.Equal("No configured category matched.", result.UncategorizedSubjects[0].Reason);
    }

    [Fact]
    public async Task CategorizeAsync_RespectsFictionExclusion()
    {
        var service = CreateService();

        var extractionResult = CreateExtractionResult(
            CreateExtractedSubject(1, "Adventure fiction stories", 2));

        var categoryRules = new[]
        {
            CreateRule(
                "adventure",
                "AdventureSubjects.txt",
                includeKeywords: ["adventure"],
                disableForFiction: true)
        };

        var result = await service.CategorizeAsync(
            extractionResult,
            categoryRules,
            new BehaviorOptions());

        Assert.Equal(0, result.CategorizedSubjectCount);
        Assert.Equal(1, result.UncategorizedSubjectCount);
        Assert.Equal("Excluded because subject contains fiction.", result.UncategorizedSubjects[0].Reason);
    }

    [Fact]
    public async Task CategorizeAsync_RespectsJuvenileExclusion()
    {
        var service = CreateService();

        var extractionResult = CreateExtractionResult(
            CreateExtractedSubject(1, "Juvenile art appreciation", 2));

        var categoryRules = new[]
        {
            CreateRule(
                "art",
                "ArtSubjects.txt",
                includeKeywords: ["art"],
                disableForJuvenile: true)
        };

        var result = await service.CategorizeAsync(
            extractionResult,
            categoryRules,
            new BehaviorOptions());

        Assert.Equal(0, result.CategorizedSubjectCount);
        Assert.Equal(1, result.UncategorizedSubjectCount);
        Assert.Equal("Excluded because subject contains juvenile.", result.UncategorizedSubjects[0].Reason);
    }

    [Fact]
    public async Task CategorizeAsync_RoutesToUncategorized_WhenSubjectBecomesBlankAfterNormalization()
    {
        var service = CreateService();

        var extractionResult = CreateExtractionResult(
            CreateExtractedSubject(1, "***", 2));

        var categoryRules = new[]
        {
            CreateRule("art", "ArtSubjects.txt", includeKeywords: ["art"])
        };

        var result = await service.CategorizeAsync(
            extractionResult,
            categoryRules,
            new BehaviorOptions());

        Assert.Equal(0, result.CategorizedSubjectCount);
        Assert.Equal(1, result.UncategorizedSubjectCount);
        Assert.Equal("Subject is blank after normalization.", result.UncategorizedSubjects[0].Reason);
    }

    [Fact]
    public async Task CategorizeAsync_DisablesMultiMatch_Deterministically_AndCountsDuplicateOccurrences()
    {
        var service = CreateService();

        var extractionResult = CreateExtractionResult(
            CreateExtractedSubject(1, "Art business", 2),
            CreateExtractedSubject(2, "Art business", 3));

        var categoryRules = new[]
        {
            CreateRule("business", "BusinessSubjects.txt", includeKeywords: ["business"], sortOrder: 10),
            CreateRule("art", "ArtSubjects.txt", includeKeywords: ["art"], sortOrder: 20)
        };

        var result = await service.CategorizeAsync(
            extractionResult,
            categoryRules,
            new BehaviorOptions { AllowMultiMatch = false });

        Assert.Equal(2, result.CategorizedSubjectCount);
        Assert.Equal(0, result.UncategorizedSubjectCount);
        Assert.Equal(2, result.TotalAssignments);

        Assert.All(result.CategorizedSubjects, subject =>
        {
            Assert.Single(subject.Matches);
            Assert.Equal("business", subject.Matches[0].CategoryKey.Value);
        });

        Assert.True(result.CategoryCounts.TryGetValue(new CategoryKey("business"), out var businessCount));
        Assert.Equal(2, businessCount);
    }

    private static CategorizationService CreateService()
    {
        var normalizationHelper = new SubjectNormalizationHelper();
        var keywordMatcher = new KeywordMatcher(normalizationHelper);
        var exclusionMatcher = new ExclusionMatcher(keywordMatcher);

        return new CategorizationService(
            NullLogger<CategorizationService>.Instance,
            normalizationHelper,
            keywordMatcher,
            exclusionMatcher);
    }

    private static ExtractionResult CreateExtractionResult(params ExtractedSubject[] subjects)
    {
        return new ExtractionResult(
            "test-input.txt",
            InputFileKind.PlainText,
            subjects,
            totalRecordsRead: subjects.Length,
            blankOrIgnoredCount: 0,
            duplicateCount: 0,
            parseWarningCount: 0);
    }

    private static ExtractedSubject CreateExtractedSubject(
        int sequenceNumber,
        string originalSubject,
        int sourceRowNumber)
    {
        var entry = new SubjectEntry(
            new SubjectText(originalSubject),
            new NormalizedSubjectText(originalSubject.ToLowerInvariant()),
            "test-input.txt",
            sourceRowNumber,
            null,
            null,
            InputFileKind.PlainText);

        return new ExtractedSubject(entry, sequenceNumber);
    }

    private static CategoryRule CreateRule(
        string key,
        string outputFileName,
        IReadOnlyList<string> includeKeywords,
        int sortOrder = 0,
        bool disableForFiction = false,
        bool disableForJuvenile = false)
    {
        return new CategoryRule
        {
            Key = key,
            DisplayName = key,
            OutputFileName = outputFileName,
            IncludeKeywords = includeKeywords.ToList(),
            ExcludeKeywords = [],
            RequireAnyKeywords = [],
            DisableForFiction = disableForFiction,
            DisableForJuvenile = disableForJuvenile,
            IncludeMatchMode = CategoryMatchMode.Contains,
            ExcludeMatchMode = CategoryMatchMode.Contains,
            RequireAnyMatchMode = CategoryMatchMode.Contains,
            SortOrder = sortOrder,
            Enabled = true
        };
    }
}
