using System.Text.Json.Serialization;

namespace LocalizationTabii.Models
{
    public class AnalysisProject
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        [JsonIgnore]
        public List<AnalysisDocument> Documents { get; set; } = new();
        
        public int DocumentCount => Documents.Count;
        
        public override string ToString() => Name;
    }
} 