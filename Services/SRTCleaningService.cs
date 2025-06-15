using System.Text;
using System.Text.RegularExpressions;

namespace LocalizationTabii.Services
{
    public class SRTCleaningService
    {
        private readonly Regex _timeCodeRegex = new Regex(@"^\d{2}:\d{2}:\d{2},\d{3} --> \d{2}:\d{2}:\d{2},\d{3}$");
        private readonly Regex _numberRegex = new Regex(@"^\d+$");
        private readonly Regex _parenthesesRegex = new Regex(@"\([^)]*\)");

        public async Task<SRTCleaningResult> CleanSRTFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("Dosya yolu boş veya null.", nameof(filePath));
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("SRT dosyası bulunamadı.", filePath);
            }

            var result = new SRTCleaningResult();
            
            string[] lines;
            try
            {
                lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new IOException($"Dosya okuma hatası: {ex.Message}", ex);
            }
            
            var cleanedEntries = new List<SRTEntry>();
            var currentEntry = new SRTEntry();
            var lineIndex = 0;
            
            result.OriginalLineCount = lines.Length;

            while (lineIndex < lines.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var line = lines[lineIndex].Trim();
                
                // Boş satırları atla
                if (string.IsNullOrWhiteSpace(line))
                {
                    lineIndex++;
                    continue;
                }
                
                // Numara satırı
                if (_numberRegex.IsMatch(line) && int.TryParse(line, out int number))
                {
                    currentEntry = new SRTEntry { Number = number };
                    lineIndex++;
                    
                    // Time code satırını oku
                    if (lineIndex < lines.Length && _timeCodeRegex.IsMatch(lines[lineIndex].Trim()))
                    {
                        currentEntry.TimeCode = lines[lineIndex].Trim();
                        lineIndex++;
                        
                        // Subtitle metinlerini oku
                        var subtitleLines = new List<string>();
                        while (lineIndex < lines.Length && 
                               !string.IsNullOrWhiteSpace(lines[lineIndex]) && 
                               !_numberRegex.IsMatch(lines[lineIndex].Trim()))
                        {
                            subtitleLines.Add(lines[lineIndex].Trim());
                            lineIndex++;
                        }
                        
                        currentEntry.OriginalText = string.Join("\n", subtitleLines);
                        
                        // Metni temizle
                        var cleanedText = CleanSubtitleText(currentEntry.OriginalText);
                        
                        // Eğer temizleme sonrası metin tamamen boşsa, bu entry'yi atla
                        if (!string.IsNullOrWhiteSpace(cleanedText))
                        {
                            currentEntry.CleanedText = cleanedText;
                            cleanedEntries.Add(currentEntry);
                        }
                        else
                        {
                            result.CleanedLinesCount++;
                        }
                    }
                }
                else
                {
                    lineIndex++;
                }
            }
            
            // Numaraları yeniden düzenle
            for (int i = 0; i < cleanedEntries.Count; i++)
            {
                cleanedEntries[i].Number = i + 1;
            }
            
            result.CleanedEntries = cleanedEntries;
            result.FinalLineCount = CalculateFinalLineCount(cleanedEntries);
            
            return result;
        }

        private string CleanSubtitleText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Parantez içindeki açıklamaları temizle
            var cleanedText = _parenthesesRegex.Replace(text, "").Trim();
            
            // Eğer sadece parantez içinde açıklama varsa ve geriye bir şey kalmadıysa
            if (string.IsNullOrWhiteSpace(cleanedText))
                return string.Empty;
            
            // Çoklu boşlukları tek boşluğa çevir
            cleanedText = Regex.Replace(cleanedText, @"\s+", " ").Trim();
            
            return cleanedText;
        }

        private int CalculateFinalLineCount(List<SRTEntry> entries)
        {
            int count = 0;
            foreach (var entry in entries)
            {
                count++; // Numara satırı
                count++; // Time code satırı
                count += entry.CleanedText.Split('\n').Length; // Subtitle satırları
                count++; // Boş satır
            }
            return count;
        }

        public async Task SaveCleanedSRTAsync(string outputPath, List<SRTEntry> entries, CancellationToken cancellationToken = default)
        {
            var sb = new StringBuilder();
            
            for (int i = 0; i < entries.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var entry = entries[i];
                
                sb.AppendLine(entry.Number.ToString());
                sb.AppendLine(entry.TimeCode);
                sb.AppendLine(entry.CleanedText);
                
                // Son entry değilse boş satır ekle
                if (i < entries.Count - 1)
                {
                    sb.AppendLine();
                }
            }
            
            await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8, cancellationToken);
        }
    }

    public class SRTCleaningResult
    {
        public List<SRTEntry> CleanedEntries { get; set; } = new();
        public int OriginalLineCount { get; set; }
        public int FinalLineCount { get; set; }
        public int CleanedLinesCount { get; set; }
    }

    public class SRTEntry
    {
        public int Number { get; set; }
        public string TimeCode { get; set; } = string.Empty;
        public string OriginalText { get; set; } = string.Empty;
        public string CleanedText { get; set; } = string.Empty;
    }
} 