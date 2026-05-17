namespace JobTracker.Core.Interfaces;

public interface IResumeTextExtractor
{
    Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default);
}
