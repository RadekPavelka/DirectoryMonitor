namespace DirectoryMonitor.Models
{
    public class DirectorySnapshot
    {
        public string RootPath { get; set; } = string.Empty;

        public List<FileMetadata> Files { get; set; } = new();

        public List<string> Directories { get; set; } = new();
    }
}
