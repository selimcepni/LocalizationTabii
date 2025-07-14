using System.Text.Json.Serialization;

namespace LocalizationTabii.Models
{
    public class AnalysisDocument
    {
        public int ID { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFilePath { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
        public string Content { get; set; } = string.Empty;
        
        [JsonIgnore]
        public int AnalysisProjectID { get; set; }
        
        public DocumentType Type { get; set; } = DocumentType.Text;
        
        public string FormattedFileSize
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                double len = FileSize;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }
        
        public override string ToString() => FileName;
    }
    
    public enum DocumentType
    {
        Text,
        Srt,
        Other
    }
} 