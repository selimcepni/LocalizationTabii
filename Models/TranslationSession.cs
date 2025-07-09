using System.Collections.ObjectModel;

namespace LocalizationTabii.Models;

/// <summary>
/// Çeviri session'ı - Model, Prompt, SRT dosyası ve kullanıcı context'ini tutar.
/// Çeviri sürecinin tüm adımlarını yönetir.
/// </summary>
public class TranslationSession
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Temel çeviri bilgileri
    public ModelConfiguration? SelectedModel { get; set; }
    public Prompt? SelectedPrompt { get; set; }
    public string TargetLanguage { get; set; } = string.Empty;
    public string UserContext { get; set; } = string.Empty; // Kullanıcının sağladığı detay prompt
    
    // SRT dosyası bilgileri
    public string OriginalFileName { get; set; } = string.Empty;
    public FileResult? OriginalFile { get; set; }
    public ObservableCollection<SubtitleEntry> SubtitleEntries { get; set; } = new();
    
    // Session durumu
    public TranslationSessionStatus Status { get; set; } = TranslationSessionStatus.Initialized;
    public string? ErrorMessage { get; set; }
    
    // İlerleme bilgileri
    public int TotalSubtitles => SubtitleEntries.Count;
    public int CompletedSubtitles => SubtitleEntries.Count(s => s.Status == TranslationStatus.Translated);
    public int FailedSubtitles => SubtitleEntries.Count(s => s.Status == TranslationStatus.Failed);
    public int InProgressSubtitles => SubtitleEntries.Count(s => s.Status == TranslationStatus.InProgress);
    public double ProgressPercentage => TotalSubtitles > 0 ? (double)CompletedSubtitles / TotalSubtitles * 100 : 0;
    
    // Çeviri süreci bilgileri
    public DateTime? TranslationStartedAt { get; set; }
    public DateTime? TranslationCompletedAt { get; set; }
    public TimeSpan? TranslationDuration => TranslationCompletedAt - TranslationStartedAt;
    
    // Chunk işleme bilgileri (30-40 subtitle batch'leri için)
    public int ChunkSize { get; set; } = 35; // Varsayılan chunk boyutu
    public int TotalChunks => (int)Math.Ceiling((double)TotalSubtitles / ChunkSize);
    public int CompletedChunks { get; set; } = 0;
    
    // Yardımcı metotlar
    public bool IsReady => SelectedModel != null && SelectedPrompt != null && SubtitleEntries.Count > 0;
    
    public bool IsCompleted => Status == TranslationSessionStatus.Completed && 
                              CompletedSubtitles == TotalSubtitles && 
                              FailedSubtitles == 0;
    
    public bool HasErrors => FailedSubtitles > 0 || !string.IsNullOrEmpty(ErrorMessage);
    
    /// <summary>
    /// Session'ı başlatmaya hazır hale getirir
    /// </summary>
    public void PrepareForTranslation()
    {
        if (!IsReady)
            throw new InvalidOperationException("Session başlatılmaya hazır değil. Model, Prompt ve SRT dosyası gerekli.");
        
        // Target language'ı prompt'tan al
        if (!string.IsNullOrEmpty(SelectedPrompt?.Language))
        {
            TargetLanguage = SelectedPrompt.Language;
        }
        
        Status = TranslationSessionStatus.Ready;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Çeviri sürecini başlatır
    /// </summary>
    public void StartTranslation()
    {
        if (Status != TranslationSessionStatus.Ready)
            throw new InvalidOperationException("Session çeviriye hazır değil.");
        
        Status = TranslationSessionStatus.InProgress;
        TranslationStartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        // Tüm subtitle'ları NotTranslated durumuna al
        foreach (var entry in SubtitleEntries)
        {
            entry.Status = TranslationStatus.NotTranslated;
            entry.TranslatedText = null;
            entry.ErrorMessage = null;
        }
    }
    
    /// <summary>
    /// Çeviri sürecini tamamlar
    /// </summary>
    public void CompleteTranslation()
    {
        Status = TranslationSessionStatus.Completed;
        TranslationCompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Session'ı hata durumuna alır
    /// </summary>
    public void SetError(string errorMessage)
    {
        Status = TranslationSessionStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Belirli bir chunk'ı tamamlandı olarak işaretler
    /// </summary>
    public void CompleteChunk()
    {
        CompletedChunks++;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Session'ı sıfırlar (yeniden çeviri için)
    /// </summary>
    public void Reset()
    {
        Status = TranslationSessionStatus.Ready;
        TranslationStartedAt = null;
        TranslationCompletedAt = null;
        CompletedChunks = 0;
        ErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
        
        // Tüm subtitle'ları temizle
        foreach (var entry in SubtitleEntries)
        {
            entry.Status = TranslationStatus.NotTranslated;
            entry.TranslatedText = null;
            entry.ErrorMessage = null;
        }
    }
    
    /// <summary>
    /// Çeviri için kullanılacak tam prompt'u oluşturur
    /// </summary>
    public string BuildFullPrompt()
    {
        if (SelectedPrompt == null)
            return string.Empty;
        
        var basePrompt = SelectedPrompt.Content;
        
        // Kullanıcı context'i varsa ekle
        if (!string.IsNullOrWhiteSpace(UserContext))
        {
            basePrompt += $"\n\nEk Bağlam: {UserContext}";
        }
        
        // Hedef dil bilgisini ekle
        if (!string.IsNullOrWhiteSpace(TargetLanguage))
        {
            basePrompt += $"\n\nHedef Dil: {TargetLanguage}";
        }
        
        return basePrompt;
    }
}

/// <summary>
/// TranslationSession'ın durumunu temsil eder
/// </summary>
public enum TranslationSessionStatus
{
    Initialized,    // Session oluşturuldu
    Ready,          // Çeviriye hazır (model, prompt, srt seçildi)
    InProgress,     // Çeviri devam ediyor
    Paused,         // Çeviri durduruldu
    Completed,      // Çeviri tamamlandı
    Failed          // Çeviri hata aldı
}

/// <summary>
/// Chunk işleme bilgileri
/// </summary>
public class ChunkInfo
{
    public int ChunkIndex { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public List<SubtitleEntry> Entries { get; set; } = new();
    public ChunkStatus Status { get; set; } = ChunkStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// Chunk durumu
/// </summary>
public enum ChunkStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
} 