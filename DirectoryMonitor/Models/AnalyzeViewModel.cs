using System.ComponentModel.DataAnnotations;

namespace DirectoryMonitor.Models
{
    public class AnalyzeViewModel
    {
        [Required(ErrorMessage = "Zadejte cestu k adresáři.")]
        public string DirectoryPath { get; set; } = string.Empty;

        public AnalysisResult? Result { get; set; }
    }
}
