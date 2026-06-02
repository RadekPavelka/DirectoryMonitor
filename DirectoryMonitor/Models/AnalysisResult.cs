namespace DirectoryMonitor.Models
{
    public class AnalysisResult
    {
        public List<string> NewFiles { get; set; } = new();

        public List<string> ChangedFiles { get; set; } = new();

        public List<string> DeletedFiles { get; set; } = new();

        public List<string> DeletedDirectories { get; set; } = new();

        public Dictionary<string, int> FileVersions { get; set; } = new();
    }
}
