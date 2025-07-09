using LocalizationTabii.Models;

namespace LocalizationTabii.Services;

/// <summary>
/// SRT dosyası parse etme servisi interface'i
/// </summary>
public interface ISrtParser
{
    /// <summary>
    /// SRT dosyasını parse eder
    /// </summary>
    /// <param name="srtContent">SRT dosyası içeriği</param>
    /// <returns>SubtitleEntry listesi</returns>
    List<SubtitleEntry> Parse(string srtContent);
    
    /// <summary>
    /// SRT dosyasını stream'den async olarak parse eder
    /// </summary>
    /// <param name="stream">SRT dosyası stream'i</param>
    /// <returns>SubtitleEntry listesi</returns>
    Task<List<SubtitleEntry>> ParseAsync(Stream stream);
    
    /// <summary>
    /// SubtitleEntry listesini SRT formatına serialize eder
    /// </summary>
    /// <param name="entries">SubtitleEntry listesi</param>
    /// <returns>SRT formatında string</returns>
    string Serialize(List<SubtitleEntry> entries);
} 