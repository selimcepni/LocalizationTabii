namespace LocalizationTabii.Models;

/// <summary>
/// Bir SRT dosyasındaki tek bir altyazı bloğunu temsil eder.
/// Bu yapı, veriyi güvenli ve yapısal bir şekilde tutar.
/// </summary>
public class SubtitleEntry
{  
    public int Sequence { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public string? TranslatedText { get; set; }
    public TranslationStatus Status { get; set; } = TranslationStatus.NotTranslated;
    public string? ErrorMessage { get; set; }   
    public string? Reference { get; set; }
    
    public TimeSpan Duration => EndTime - StartTime;
    public string Timecode => 
        $"{StartTime:hh\\:mm\\:ss\\,fff} --> {EndTime:hh\\:mm\\:ss\\,fff}";
} 