namespace DirectoryMonitor.Models
{
    public record AnalysisResponse(
        AnalysisResult Result,
        bool IsFirstRun,
        string? ErrorMessage
    );
}
