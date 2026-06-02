namespace DirectoryMonitor.Models
{
    public class FileMetadata
    {
        public string RelativePath { get; set; } = string.Empty;

        public string Hash { get; set; } = string.Empty;

        public int Version { get; set; }
    }
}
