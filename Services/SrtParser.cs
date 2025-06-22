using System.Text;
using System.Text.RegularExpressions;
using LocalizationTabii.Models;

namespace LocalizationTabii.Services;

/// <summary>
/// SRT formatındaki metinleri ayrıştıran (parse) ve birleştiren (serialize)
/// sağlam ve güvenilir bir yardımcı sınıftır.
/// </summary>
public static class SrtParser
{
    // Zaman kodunu yakalamak için kullanılan, derlenmiş ve optimize edilmiş Regex deseni.
    // Örnek: "00:00:20,123 --> 00:00:22,456"
    private static readonly Regex TimecodeRegex = 
        new Regex(@"(\d{2}:\d{2}:\d{2},\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2},\d{3})", RegexOptions.Compiled);

    /// <summary>
    /// Ham SRT metnini, hatalı bloklara karşı dayanıklı bir şekilde bir SubtitleEntry listesine dönüştürür.
    /// </summary>
    /// <param name="srtContent">SRT dosyasının tüm içeriği.</param>
    /// <returns>Her bir geçerli altyazı bloğu için bir SubtitleEntry içeren liste.</returns>
    public static List<SubtitleEntry> Parse(string srtContent)
    {
        if (string.IsNullOrWhiteSpace(srtContent))
        {
            return new List<SubtitleEntry>();
        }

        var entries = new List<SubtitleEntry>();
        
      
        var blocks = srtContent.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var block in blocks)
        {
            try
            {
                var lines = block.Trim().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

           
                if (lines.Length < 2) continue;

                if (!int.TryParse(lines[0].Trim(), out int sequence))
                {
                 
                    continue;
                }

                var timecodeMatch = TimecodeRegex.Match(lines[1]);
                if (!timecodeMatch.Success)
                {
                    
                    continue;
                }


                const string timeFormat = @"hh\:mm\:ss\,fff";
                var startTime = TimeSpan.ParseExact(timecodeMatch.Groups[1].Value, timeFormat, null);
                var endTime = TimeSpan.ParseExact(timecodeMatch.Groups[2].Value, timeFormat, null);

            
                var text = lines.Length > 2 ? string.Join(Environment.NewLine, lines.Skip(2)) : string.Empty;

                entries.Add(new SubtitleEntry
                {
                    Sequence = sequence,
                    StartTime = startTime,
                    EndTime = endTime,
                    OriginalText = text
                });
            }
            catch (Exception)
            {
                
                continue;
            }
        }

        return entries;
    }

    /// <summary>
    /// Bir SubtitleEntry listesini standart SRT formatında bir metne dönüştürür.
    /// </summary>
    /// <param name="entries">Çevrilmiş veya orijinal SubtitleEntry listesi.</param>
    /// <returns>Dosyaya yazılmaya hazır, tam bir SRT metni.</returns>
    public static string Serialize(List<SubtitleEntry> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        foreach (var entry in entries)
        {
            sb.AppendLine(entry.Sequence.ToString());
            sb.AppendLine(entry.Timecode);
            sb.AppendLine(string.IsNullOrEmpty(entry.TranslatedText) ? entry.OriginalText : entry.TranslatedText);
            sb.AppendLine(); 
        }

        return sb.ToString();
    }
}