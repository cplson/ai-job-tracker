using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using JobTracker.Core.Interfaces;
using UglyToad.PdfPig;

namespace JobTracker.Core.Services;

public class ResumeTextExtractor : IResumeTextExtractor
{
    public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(filePath))
            return Task.FromResult(string.Empty);

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var text = extension switch
        {
            ".pdf" => ExtractFromPdf(filePath),
            ".docx" => ExtractFromDocx(filePath),
            ".txt" => File.ReadAllText(filePath, Encoding.UTF8),
            _ => string.Empty
        };

        return Task.FromResult(Normalize(text));
    }

    private static string ExtractFromPdf(string filePath)
    {
        var builder = new StringBuilder();

        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }

    private static string ExtractFromDocx(string filePath)
    {
        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            var line = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
            if (!string.IsNullOrWhiteSpace(line))
                builder.AppendLine(line);
        }

        return builder.ToString();
    }

    private static string Normalize(string text) =>
        string.Join('\n', text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0));
}
