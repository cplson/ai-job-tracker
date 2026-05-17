using FluentAssertions;
using JobTracker.Core.Services;

public class ResumeTextExtractorTests
{
    private readonly ResumeTextExtractor _extractor = new();

    [Fact]
    public async Task ExtractTextAsync_ShouldReadPlainTextFile()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
        await File.WriteAllTextAsync(tempPath, "Software Engineer\nC# and React");

        try
        {
            var result = await _extractor.ExtractTextAsync(tempPath);
            result.Should().Contain("Software Engineer");
            result.Should().Contain("C# and React");
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ExtractTextAsync_ShouldReturnEmpty_ForUnsupportedExtension()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        await File.WriteAllBytesAsync(tempPath, [0x50, 0x4b, 0x03, 0x04]);

        try
        {
            var result = await _extractor.ExtractTextAsync(tempPath);
            result.Should().BeEmpty();
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
