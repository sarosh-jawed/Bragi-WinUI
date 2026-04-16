using Bragi.Application.Configuration;
using Bragi.Domain.Enums;
using Bragi.Infrastructure.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bragi.Tests.Ingestion;

public sealed class InputIngestServiceTests
{
    [Fact]
    public async Task DetectInputFileKindAsync_ReturnsPlainText_ForTxtExtension()
    {
        var service = CreateService();

        var result = await service.DetectInputFileKindAsync(
            "C:\\Temp\\subjects.txt",
            new InputOptions());

        Assert.Equal(InputFileKind.PlainText, result);
    }

    [Fact]
    public async Task DetectInputFileKindAsync_ReturnsCsv_ForCsvExtension()
    {
        var service = CreateService();

        var result = await service.DetectInputFileKindAsync(
            "C:\\Temp\\subjects.csv",
            new InputOptions());

        Assert.Equal(InputFileKind.Csv, result);
    }

    [Fact]
    public async Task DetectInputFileKindAsync_ReturnsUnknown_WhenExtensionIsUnsupported_AndFallbackDisabled()
    {
        var service = CreateService();

        var result = await service.DetectInputFileKindAsync(
            "C:\\Temp\\subjects.abc",
            new InputOptions
            {
                TreatUnknownExtensionAsPlainText = false
            });

        Assert.Equal(InputFileKind.Unknown, result);
    }

    [Fact]
    public async Task DetectInputFileKindAsync_ReturnsPlainText_WhenExtensionIsUnsupported_AndFallbackEnabled()
    {
        var service = CreateService();

        var result = await service.DetectInputFileKindAsync(
            "C:\\Temp\\subjects.abc",
            new InputOptions
            {
                TreatUnknownExtensionAsPlainText = true
            });

        Assert.Equal(InputFileKind.PlainText, result);
    }

    [Fact]
    public async Task ReadCsvRowsAsync_ReadsRows_AndHandlesDuplicateHeaders()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");

        try
        {
            await File.WriteAllTextAsync(
                filePath,
                "name,name,subjects\r\n" +
                "A,B,Art\r\n" +
                "C,D,Biology\r\n");

            var service = CreateService();

            var rows = await service.ReadCsvRowsAsync(filePath);

            Assert.Equal(2, rows.Count);
            Assert.Equal("A", rows[0]["name"]);
            Assert.Equal("B", rows[0]["name__2"]);
            Assert.Equal("Art", rows[0]["subjects"]);
            Assert.Equal("D", rows[1]["name__2"]);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    private static InputIngestService CreateService()
    {
        return new InputIngestService(NullLogger<InputIngestService>.Instance);
    }
}
