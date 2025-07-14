using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Services;
using LocalizationTabii.ComponentModel;
using LocalizationTabii.Models;
using System.Collections.ObjectModel;
using System.Text;

namespace LocalizationTabii.PageModels
{
    public partial class TranslatePageModel : ObservableObject
    {
        private readonly ModalErrorHandler _errorHandler;
        private readonly ISrtParser _srtParser;
        private readonly List<string> _sessionLogs; // Memory'de log tutmak için

        [ObservableProperty]
        private FileResult? selectedFile;

        [ObservableProperty]
        private string selectedFileName = string.Empty;

        [ObservableProperty]
        private bool isFileSelected;

        [ObservableProperty]
        private TranslationSession? currentSession;

        [ObservableProperty]
        private bool isSessionActive;

        [ObservableProperty]
        private string sessionStatus = string.Empty;

        public TranslatePageModel(ModalErrorHandler errorHandler, ISrtParser srtParser)
        {
            _errorHandler = errorHandler;
            _srtParser = srtParser;
            _sessionLogs = new List<string>();
            
            LogMessage("🚀 TranslatePageModel başlatıldı");
        }

        private void LogMessage(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {message}";
                _sessionLogs.Add(logEntry);
                Console.WriteLine($"📝 LOG: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔴 Log yazma hatası: {ex.Message}");
            }
        }

        private string GetSavePath()
        {
            try
            {
                // macOS sandbox uygulaması için Documents klasörünü kullan
                if (OperatingSystem.IsMacOS())
                {
                    var macDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    LogMessage($"🍎 macOS algılandı - Sandbox nedeniyle Documents klasörü kullanılıyor: {macDocumentsPath}");
                    
                    // Documents klasörü var mı kontrol et
                    if (Directory.Exists(macDocumentsPath))
                    {
                        return macDocumentsPath;
                    }
                    else
                    {
                        LogMessage("⚠️ Documents klasörü bulunamadı, standart yol deneniyor");
                    }
                }
                
                // Diğer platformlar için masaüstünü dene
                var standardDesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                
                if (!string.IsNullOrEmpty(standardDesktopPath) && Directory.Exists(standardDesktopPath))
                {
                    LogMessage($"💻 Masaüstü yolu: {standardDesktopPath}");
                    return standardDesktopPath;
                }
                
                // Masaüstü bulunamazsa Documents klasörünü kullan
                var fallbackDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (!string.IsNullOrEmpty(fallbackDocumentsPath) && Directory.Exists(fallbackDocumentsPath))
                {
                    LogMessage($"📁 Masaüstü bulunamadı, Documents klasörü kullanılıyor: {fallbackDocumentsPath}");
                    return fallbackDocumentsPath;
                }
                
                // Son çare olarak ev dizinini kullan
                LogMessage("⚠️ Hiçbir klasör bulunamadı, ev dizini kullanılıyor");
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Dosya yolu belirleme hatası: {ex.Message}");
                // Hata durumunda ev dizinini döndür
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
        }

        private async Task SaveLogsToFile()
        {
            try
            {
                // Dosya kaydetme yolu (macOS'te Documents, diğerlerinde masaüstü)
                var savePath = GetSavePath();
                var fileName = $"TranslationSession_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var filePath = Path.Combine(savePath, fileName);
                
                // Session bilgilerini header olarak ekle
                var headerInfo = new List<string>
                {
                    "==========================================",
                    "    LOCALIZATION TABII - ÇEVİRİ SESSION RAPORU",
                    "==========================================",
                    $"Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm:ss}",
                    ""
                };

                if (CurrentSession != null)
                {
                    headerInfo.AddRange(new[]
                    {
                        "SESSION BİLGİLERİ:",
                        $"Session ID: {CurrentSession.Id}",
                        $"Dosya Adı: {CurrentSession.OriginalFileName}",
                        $"Model: {CurrentSession.SelectedModel?.DisplayName ?? "Seçilmedi"}",
                        $"Provider: {CurrentSession.SelectedModel?.Provider ?? "Seçilmedi"}",
                        $"Prompt: {CurrentSession.SelectedPrompt?.Title ?? "Seçilmedi"}",
                        $"Hedef Dil: {CurrentSession.TargetLanguage}",
                        $"Altyazı Sayısı: {CurrentSession.TotalSubtitles}",
                        $"Chunk Sayısı: {CurrentSession.TotalChunks}",
                        $"Chunk Boyutu: {CurrentSession.ChunkSize}",
                        $"Session Durumu: {CurrentSession.Status}",
                        ""
                    });
                }

                headerInfo.AddRange(new[]
                {
                    "DETAYLI LOG:",
                    "=========================================="
                });

                // Tüm logları birleştir
                var allLogs = new List<string>();
                allLogs.AddRange(headerInfo);
                allLogs.AddRange(_sessionLogs);
                
                // Chunk detaylarını ekle
                await AddChunkDetailsToLogs(allLogs);
                
                allLogs.Add("");
                allLogs.Add("==========================================");
                allLogs.Add("Log dosyası oluşturuldu: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));

                // Dosyaya yaz
                await File.WriteAllLinesAsync(filePath, allLogs, Encoding.UTF8);
                
                LogMessage($"📁 Log dosyası kaydedildi: {fileName}");
                LogMessage($"📂 Tam yol: {filePath}");
                
                // Kullanıcıya bilgi ver
                var successMessage = OperatingSystem.IsMacOS() 
                    ? $"Session logları macOS Documents klasörüne kaydedildi:\n\n📄 {fileName}\n📂 {savePath}\n\n💡 Sandbox güvenliği nedeniyle Documents klasörü kullanıldı"
                    : $"Session logları kaydedildi:\n{fileName}";
                    
                _errorHandler?.ShowSuccess("Log Kaydedildi", successMessage);
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Log dosyası kaydetme hatası: {ex.Message}");
                _errorHandler?.ShowError("Log Kaydetme Hatası", ex.Message);
            }
        }

        private async Task AddChunkDetailsToLogs(List<string> logsList)
        {
            try
            {
                if (CurrentSession == null || CurrentSession.SubtitleEntries.Count == 0)
                {
                    logsList.Add("");
                    logsList.Add("CHUNK DETAYLARI:");
                    logsList.Add("==========================================");
                    logsList.Add("❌ Session'da altyazı verisi bulunamadı.");
                    return;
                }

                var chunks = GenerateChunks();
                
                logsList.Add("");
                logsList.Add("CHUNK DETAYLARI:");
                logsList.Add("==========================================");
                logsList.Add($"Toplam {chunks.Count} chunk oluşturuldu");
                logsList.Add($"Her chunk'ta maksimum {CurrentSession.ChunkSize} altyazı bulunuyor");
                logsList.Add("");

                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];
                    logsList.Add($"CHUNK {i + 1}:");
                    logsList.Add($"│ Altyazı Aralığı: {chunk.StartIndex + 1} - {chunk.EndIndex + 1}");
                    logsList.Add($"│ Toplam Altyazı: {chunk.Entries.Count}");
                    logsList.Add($"│ Süre Aralığı: {chunk.Entries.First().StartTime:hh\\:mm\\:ss} - {chunk.Entries.Last().EndTime:hh\\:mm\\:ss}");
                    logsList.Add($"│ Durum: {chunk.Status}");
                    logsList.Add("│");
                    logsList.Add("│ İÇERİK:");
                    
                    foreach (var subtitle in chunk.Entries)
                    {
                        logsList.Add($"│   [{subtitle.Sequence}] {subtitle.Timecode}");
                        logsList.Add($"│   │ Orijinal: {subtitle.OriginalText.Replace("\n", "\\n")}");
                        
                        if (!string.IsNullOrEmpty(subtitle.TranslatedText))
                        {
                            logsList.Add($"│   │ Çeviri: {subtitle.TranslatedText.Replace("\n", "\\n")}");
                        }
                        
                        logsList.Add("│   │");
                    }
                    
                    logsList.Add("│");
                    logsList.Add($"└── Chunk {i + 1} sonu");
                    logsList.Add("");
                    
                    // Her 5 chunk'ta bir ara ver (dosya boyutunu kontrol etmek için)
                    if ((i + 1) % 5 == 0)
                    {
                        logsList.Add($"--- {i + 1}. chunk'a kadar işlendi ---");
                        logsList.Add("");
                    }
                }
                
                logsList.Add("==========================================");
                logsList.Add($"✅ Tüm {chunks.Count} chunk detayı log'a eklendi");
                
                LogMessage($"📋 {chunks.Count} chunk detayı log dosyasına eklendi");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Chunk detayları ekleme hatası: {ex.Message}");
                logsList.Add("");
                logsList.Add("CHUNK DETAYLARI:");
                logsList.Add("==========================================");
                logsList.Add($"❌ Chunk detayları eklenirken hata oluştu: {ex.Message}");
            }
        }

        private List<ChunkInfo> GenerateChunks()
        {
            if (CurrentSession == null || CurrentSession.SubtitleEntries.Count == 0)
                return new List<ChunkInfo>();

            var chunks = new List<ChunkInfo>();
            var subtitles = CurrentSession.SubtitleEntries.ToList();
            var chunkSize = CurrentSession.ChunkSize;

            LogMessage($"🔧 Chunk oluşturma başlatıldı - {subtitles.Count} altyazı, chunk boyutu: {chunkSize}");

            for (int i = 0; i < subtitles.Count; i += chunkSize)
            {
                var endIndex = Math.Min(i + chunkSize - 1, subtitles.Count - 1);
                var chunkEntries = subtitles.Skip(i).Take(endIndex - i + 1).ToList();

                var chunk = new ChunkInfo
                {
                    ChunkIndex = chunks.Count + 1,
                    StartIndex = i,
                    EndIndex = endIndex,
                    Entries = chunkEntries,
                    Status = ChunkStatus.Pending,
                    ProcessedAt = null
                };

                chunks.Add(chunk);
                
                LogMessage($"📦 Chunk {chunk.ChunkIndex} oluşturuldu: {chunkEntries.Count} altyazı (indeks {i}-{endIndex})");
            }

            LogMessage($"✅ Toplam {chunks.Count} chunk oluşturuldu");
            return chunks;
        }

        public void HandleFileSelected(FileResult fileResult)
        {
            try
            {
                LogMessage($"📁 Dosya seçildi: {fileResult.FileName}");
                
                SelectedFile = fileResult;
                SelectedFileName = fileResult.FileName;
                IsFileSelected = true;
                
                // Yeni session oluştur
                CreateNewSession(fileResult);
                
                // SRT dosyasını parse et
                _ = ParseSrtFileAsync(fileResult);
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Dosya işleme hatası: {ex.Message}");
                _errorHandler?.ShowError("Dosya İşleme Hatası", ex.Message);
            }
        }

        private void CreateNewSession(FileResult fileResult)
        {
            CurrentSession = new TranslationSession
            {
                OriginalFile = fileResult,
                OriginalFileName = fileResult.FileName,
                Status = TranslationSessionStatus.Initialized
            };
            
            IsSessionActive = true;
            UpdateSessionStatus();
            
            LogMessage($"🎯 Session oluşturuldu - ID: {CurrentSession.Id}");
            LogSessionInfo();
        }

        private async Task ParseSrtFileAsync(FileResult fileResult)
        {
            try
            {
                if (CurrentSession == null) return;

                LogMessage("⚙️ SRT parse işlemi başlatıldı");
                CurrentSession.Status = TranslationSessionStatus.Initialized;
                UpdateSessionStatus();

                // SRT dosyasını parse et
                using var stream = await fileResult.OpenReadAsync();
                var subtitleEntries = await _srtParser.ParseAsync(stream);
                
                LogMessage($"✅ SRT parse tamamlandı - {subtitleEntries.Count} altyazı bloğu bulundu");
                
                // Session'a subtitle'ları ekle
                CurrentSession.SubtitleEntries.Clear();
                foreach (var entry in subtitleEntries)
                {
                    CurrentSession.SubtitleEntries.Add(entry);
                }

                // Parse edilen içeriği logla
                LogParsedSrtContent(subtitleEntries);

                // Başarı popup'ı kaldırıldı - session ready durumu yeterli feedback
                // _errorHandler?.ShowSuccess("Başarılı", 
                //     $"SRT dosyası başarıyla yüklendi. {subtitleEntries.Count} altyazı bloğu bulundu.");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ SRT parse hatası: {ex.Message}");
                CurrentSession?.SetError($"SRT parse hatası: {ex.Message}");
                _errorHandler?.ShowError("SRT Parse Hatası", ex.Message);
            }
            finally
            {
                UpdateSessionStatus();
                LogSessionInfo();
            }
        }

        private void LogParsedSrtContent(List<SubtitleEntry> entries)
        {
            LogMessage("📄 Parse edilen SRT içeriği:");
            LogMessage("=" + new string('=', 50));
            
            foreach (var entry in entries.Take(10)) // İlk 10 bloğu logla
            {
                LogMessage($"[{entry.Sequence}] {entry.Timecode}");
                LogMessage($"     Metin: {entry.OriginalText}");
                LogMessage($"     Süre: {entry.Duration.TotalSeconds:F1} saniye");
                LogMessage("     ---");
            }
            
            if (entries.Count > 10)
            {
                LogMessage($"... ve {entries.Count - 10} altyazı bloğu daha");
            }
            
            LogMessage("=" + new string('=', 50));
        }

        public void SetSelectedModel(ModelConfiguration model)
        {
            if (CurrentSession != null)
            {
                CurrentSession.SelectedModel = model;
                UpdateSessionStatus();
                
                LogMessage($"🤖 Model seçildi: {model.DisplayName ?? model.ModelId} ({model.Provider})");
                LogSessionInfo();
            }
        }

        public void SetSelectedPrompt(Prompt prompt)
        {
            if (CurrentSession != null)
            {
                CurrentSession.SelectedPrompt = prompt;
                UpdateSessionStatus();
                
                LogMessage($"💬 Prompt seçildi: {prompt.Title} (Dil: {prompt.Language})");
                LogMessage($"     Kategori: {prompt.Category}");
                LogMessage($"     İçerik: {prompt.Content.Substring(0, Math.Min(100, prompt.Content.Length))}...");
                LogSessionInfo();
            }
        }

        public void SetUserContext(string userContext)
        {
            if (CurrentSession != null)
            {
                CurrentSession.UserContext = userContext;
                UpdateSessionStatus();
                
                if (!string.IsNullOrWhiteSpace(userContext))
                {
                    LogMessage($"📝 Kullanıcı context'i eklendi: {userContext}");
                    LogSessionInfo();
                }
            }
        }

        private void UpdateSessionStatus()
        {
            if (CurrentSession == null)
            {
                SessionStatus = "Session yok";
                return;
            }

            SessionStatus = CurrentSession.Status switch
            {
                TranslationSessionStatus.Initialized => "Session başlatıldı - SRT dosyası yükleniyor...",
                TranslationSessionStatus.Ready => "Çeviriye hazır",
                TranslationSessionStatus.InProgress => $"Çeviri devam ediyor ({CurrentSession.ProgressPercentage:F1}%)",
                TranslationSessionStatus.Paused => "Çeviri duraklatıldı",
                TranslationSessionStatus.Completed => "Çeviri tamamlandı",
                TranslationSessionStatus.Failed => $"Hata: {CurrentSession.ErrorMessage}",
                _ => "Bilinmeyen durum"
            };
        }

        private void LogSessionInfo()
        {
            if (CurrentSession == null) return;
            
            LogMessage("📊 Session Bilgileri:");
            LogMessage($"     Session ID: {CurrentSession.Id}");
            LogMessage($"     Durum: {CurrentSession.Status}");
            LogMessage($"     Dosya: {CurrentSession.OriginalFileName}");
            LogMessage($"     Model: {CurrentSession.SelectedModel?.DisplayName ?? "Seçilmedi"}");
            LogMessage($"     Prompt: {CurrentSession.SelectedPrompt?.Title ?? "Seçilmedi"}");
            LogMessage($"     Hedef Dil: {CurrentSession.TargetLanguage}");
            LogMessage($"     Altyazı Sayısı: {CurrentSession.TotalSubtitles}");
            LogMessage($"     Chunk Sayısı: {CurrentSession.TotalChunks}");
            LogMessage($"     Chunk Boyutu: {CurrentSession.ChunkSize}");
            LogMessage($"     Hazır mı: {(CurrentSession.IsReady ? "✅ Evet" : "❌ Hayır")}");
            
            if (!string.IsNullOrEmpty(CurrentSession.UserContext))
            {
                LogMessage($"     Kullanıcı Context'i: {CurrentSession.UserContext}");
            }
            
            if (!string.IsNullOrEmpty(CurrentSession.ErrorMessage))
            {
                LogMessage($"     Hata: {CurrentSession.ErrorMessage}");
            }
            
            LogMessage("     " + new string('-', 40));
        }

        [RelayCommand]
        private void PrepareForTranslation()
        {
            try
            {
                LogMessage("🔧 Çeviri hazırlığı başlatıldı");
                CurrentSession?.PrepareForTranslation();
                UpdateSessionStatus();
                LogSessionInfo();
                
                if (CurrentSession?.IsReady == true)
                {
                    LogMessage("✅ Session çeviriye hazır hale getirildi");
                    LogMessage($"🎯 Tam Prompt: {CurrentSession.BuildFullPrompt()}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Çeviri hazırlığı hatası: {ex.Message}");
                _errorHandler?.ShowError("Hazırlık Hatası", ex.Message);
            }
        }

        [RelayCommand]
        private async Task StartTranslationAsync()
        {
            try
            {
                if (CurrentSession == null || !CurrentSession.IsReady)
                {
                    LogMessage("❌ Session çeviriye hazır değil - başlatılamıyor");
                    _errorHandler?.ShowError("Session Hatası", "Session çeviriye hazır değil.");
                    return;
                }

                LogMessage("🚀 Çeviri süreci başlatıldı");
                CurrentSession.StartTranslation();
                UpdateSessionStatus();
                LogSessionInfo();

                // Chunk'ları oluştur ve log'la
                LogMessage("📦 Chunk'lar oluşturuluyor...");
                var chunks = GenerateChunks();
                
                if (chunks.Count > 0)
                {
                    LogMessage($"✅ {chunks.Count} chunk başarıyla oluşturuldu");
                    LogMessage($"📊 Chunk özeti:");
                    LogMessage($"     • Toplam altyazı sayısı: {CurrentSession.TotalSubtitles}");
                    LogMessage($"     • Chunk boyutu: {CurrentSession.ChunkSize}");
                    LogMessage($"     • Oluşturulan chunk sayısı: {chunks.Count}");
                    
                    // Her chunk'ın özetini logla
                    for (int i = 0; i < Math.Min(chunks.Count, 3); i++) // İlk 3 chunk'ı özet olarak göster
                    {
                        var chunk = chunks[i];
                        LogMessage($"     📦 Chunk {chunk.ChunkIndex}: {chunk.Entries.Count} altyazı " +
                                 $"(sıra {chunk.StartIndex + 1}-{chunk.EndIndex + 1})");
                    }
                    
                    if (chunks.Count > 3)
                    {
                        LogMessage($"     ... ve {chunks.Count - 3} chunk daha");
                    }
                }
                else
                {
                    LogMessage("⚠️ Hiç chunk oluşturulamadı - altyazı verisi bulunamadı");
                }

                // TODO: Burada TranslationCoordinator'ı çağıracağız
                // await _translationCoordinator.StartTranslationAsync(CurrentSession);
                
                LogMessage("⏳ Çeviri süreci başlatma işlemi tamamlandı (henüz LLM çağrısı yok)");
                LogMessage("📁 Session logları ve chunk detayları masaüstüne kaydediliyor...");
                
                // Logları dosyaya kaydet
                await SaveLogsToFile();
                
                _errorHandler?.ShowSuccess("Başarılı", 
                    $"Çeviri süreci başlatıldı!\n\n" +
                    $"• {chunks.Count} chunk oluşturuldu\n" +
                    $"• Detaylı log dosyası masaüstüne kaydedildi\n" +
                    $"• Chunk içerikleri log dosyasında görüntülenebilir");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Çeviri başlatma hatası: {ex.Message}");
                CurrentSession?.SetError($"Çeviri başlatma hatası: {ex.Message}");
                _errorHandler?.ShowError("Çeviri Hatası", ex.Message);
                UpdateSessionStatus();
            }
        }

        [RelayCommand]
        private void ResetSession()
        {
            try
            {
                LogMessage("🔄 Session reset işlemi başlatıldı");
                CurrentSession?.Reset();
                UpdateSessionStatus();
                LogSessionInfo();
                LogMessage("✅ Session başarıyla resetlendi");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Reset hatası: {ex.Message}");
                _errorHandler?.ShowError("Reset Hatası", ex.Message);
            }
        }

        [RelayCommand]
        private void ClearSession()
        {
            LogMessage("🗑️ Session temizleme işlemi başlatıldı");
            
            if (CurrentSession != null)
            {
                LogMessage($"     Temizlenen Session ID: {CurrentSession.Id}");
            }
            
            CurrentSession = null;
            SelectedFile = null;
            SelectedFileName = string.Empty;
            IsFileSelected = false;
            IsSessionActive = false;
            SessionStatus = string.Empty;
            
            LogMessage("✅ Session başarıyla temizlendi");
            
            // Logları da temizle
            _sessionLogs.Clear();
            LogMessage("🚀 Yeni session için hazır");
        }

        // Session durumu kontrol metodları
        public bool CanPrepareForTranslation => CurrentSession?.SubtitleEntries.Count > 0;
        public bool CanStartTranslation => CurrentSession?.IsReady == true;
        public bool CanResetSession => CurrentSession != null && CurrentSession.Status != TranslationSessionStatus.Initialized;

        // Session bilgileri için property'ler
        public string SessionInfo
        {
            get
            {
                if (CurrentSession == null) return "Session yok";
                
                return $"Model: {CurrentSession.SelectedModel?.DisplayName ?? "Seçilmedi"}\n" +
                       $"Prompt: {CurrentSession.SelectedPrompt?.Title ?? "Seçilmedi"}\n" +
                       $"Hedef Dil: {CurrentSession.TargetLanguage}\n" +
                       $"Altyazı Sayısı: {CurrentSession.TotalSubtitles}\n" +
                       $"Chunk Sayısı: {CurrentSession.TotalChunks}";
            }
        }

        // Debug için log sayısını görmek
        public int LogCount => _sessionLogs.Count;
    }
}
