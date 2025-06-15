using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LocalizationTabii.Services;

namespace LocalizationTabii.ComponentModel
{
    public partial class ProcessingSRTViewModel : ObservableObject
    {
        private readonly SRTCleaningService _srtCleaningService;
        
        [ObservableProperty]
        private string operationName = string.Empty;

        [ObservableProperty]
        private string fileName = string.Empty;
        
        [ObservableProperty]
        private string filePath = string.Empty;

        [ObservableProperty]
        private string statusMessage = "İşlem başlatılıyor...";

        [ObservableProperty]
        private double progressValue = 0;

        [ObservableProperty]
        private string progressText = "Hazırlanıyor...";

        [ObservableProperty]
        private bool showSteps = true;

        [ObservableProperty]
        private bool canCancel = true;

        public ObservableCollection<ProcessingStep> ProcessingSteps { get; } = new();

        private CancellationTokenSource? _cancellationTokenSource;
        private SRTCleaningResult? _cleaningResult;

        public event EventHandler<SRTProcessingCompletedEventArgs>? ProcessingCompleted;
        public event EventHandler? ProcessingCancelled;
        public event EventHandler<ProcessingFailedEventArgs>? ProcessingFailed;

        public ProcessingSRTViewModel()
        {
            _srtCleaningService = new SRTCleaningService();
            InitializeSteps();
        }

        public void Initialize(string operationName, string fileName, string filePath = "")
        {
            OperationName = operationName;
            FileName = fileName;
            FilePath = filePath;
            StatusMessage = "İşlem başlatılıyor...";
            ProgressValue = 0;
            ProgressText = "Hazırlanıyor...";
            CanCancel = true;
            
            // Adımları sıfırla
            foreach (var step in ProcessingSteps)
            {
                step.Status = ProcessingStepStatus.Pending;
                step.Duration = "";
                step.ShowDuration = false;
            }
        }

        private void InitializeSteps()
        {
            ProcessingSteps.Clear();
            ProcessingSteps.Add(new ProcessingStep("Dosya okunuyor", ProcessingStepStatus.Pending));
            ProcessingSteps.Add(new ProcessingStep("SRT formatı kontrol ediliyor", ProcessingStepStatus.Pending));
            ProcessingSteps.Add(new ProcessingStep("Betimleme metinleri temizleniyor", ProcessingStepStatus.Pending));
            ProcessingSteps.Add(new ProcessingStep("Çıktı dosyası oluşturuluyor", ProcessingStepStatus.Pending));
            ProcessingSteps.Add(new ProcessingStep("İşlem tamamlandı", ProcessingStepStatus.Pending));
        }

        public async Task StartProcessingAsync()
        {
            _cancellationTokenSource?.Cancel(); // Önceki işlemi iptal et
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                StatusMessage = "İşlem başlatılıyor...";
                ProgressValue = 0;
                ProgressText = "Hazırlanıyor...";
                CanCancel = true;
                
                await ProcessStepsAsync(token);
                
                if (!token.IsCancellationRequested)
                {
                    StatusMessage = "İşlem başarıyla tamamlandı!";
                    ProgressValue = 100;
                    ProgressText = "Tamamlandı";
                    CanCancel = false;
                    
                    // UI thread'de event'i güvenli şekilde tetikle
                    await Task.Delay(200); // Son adımın görünmesi için kısa bekleme
                    
                    var outputFileName = GenerateOutputFileName();
                    
                    // Event'i ana thread'de tetikle
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("🎉 ProcessingCompleted event tetikleniyor: " + outputFileName);
                        ProcessingCompleted?.Invoke(this, new SRTProcessingCompletedEventArgs(
                            OperationName, FileName, outputFileName, "İşlem başarıyla tamamlandı", _cleaningResult));
                        System.Diagnostics.Debug.WriteLine("🎉 ProcessingCompleted event tetiklendi");
                    });
                }
            }
            catch (OperationCanceledException)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    StatusMessage = "İşlem iptal edildi";
                    ProgressText = "İptal edildi";
                    CanCancel = false;
                    ProcessingCancelled?.Invoke(this, EventArgs.Empty);
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    StatusMessage = $"Hata: {ex.Message}";
                    ProgressText = "Hata oluştu";
                    CanCancel = false;
                    
                    // Daha detaylı hata bilgisi
                    var detailedError = $"ProcessingSRTViewModel Hatası:\n" +
                                      $"Message: {ex.Message}\n" +
                                      $"InnerException: {ex.InnerException?.Message}\n" +
                                      $"StackTrace: {ex.StackTrace}\n" +
                                      $"FileName: {FileName}\n" +
                                      $"FilePath: {FilePath}\n" +
                                      $"OperationName: {OperationName}";
                    
                    System.Diagnostics.Debug.WriteLine($"❌ {detailedError}");
                    
                    // Hata event'i tetikle
                    ProcessingFailed?.Invoke(this, new ProcessingFailedEventArgs(ex, detailedError));
                });
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async Task ProcessStepsAsync(CancellationToken token)
        {
            var stepDuration = TimeSpan.Zero;
            
            for (int i = 0; i < ProcessingSteps.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                
                var step = ProcessingSteps[i];
                var startTime = DateTime.Now;
                
                // Adımı başlat
                step.Status = ProcessingStepStatus.InProgress;
                StatusMessage = step.StepName;
                ProgressText = $"Adım {i + 1}/{ProcessingSteps.Count}: {step.StepName}";
                
                // İlerleme değerini güncelle
                var baseProgress = (double)i / ProcessingSteps.Count * 100;
                ProgressValue = baseProgress;
                
                try
                {
                    // Gerçek işlem adımları
                    switch (i)
                    {
                        case 0: // Dosya okunuyor
                            await ProcessStep_ReadFile(token);
                            break;
                        case 1: // SRT formatı kontrol ediliyor
                            await ProcessStep_ValidateFormat(token);
                            break;
                        case 2: // Betimleme metinleri temizleniyor
                            await ProcessStep_CleanSubtitles(token);
                            break;
                        case 3: // Çıktı dosyası oluşturuluyor
                            await ProcessStep_SaveOutput(token);
                            break;
                        case 4: // İşlem tamamlandı
                            await ProcessStep_Finalize(token);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    step.Status = ProcessingStepStatus.Failed;
                    throw new Exception($"Adım {i + 1} ({step.StepName}) başarısız: {ex.Message}", ex);
                }
                
                // Adımı tamamla
                step.Status = ProcessingStepStatus.Completed;
                stepDuration = DateTime.Now - startTime;
                step.Duration = $"{stepDuration.TotalSeconds:F1}s";
                step.ShowDuration = true;
                
                ProgressValue = (double)(i + 1) / ProcessingSteps.Count * 100;
            }
        }

        private async Task ProcessStep_ReadFile(CancellationToken token)
        {
            await Task.Delay(500, token); // UI için kısa gecikme
            
            if (string.IsNullOrEmpty(FilePath))
            {
                throw new ArgumentException("Dosya yolu belirtilmedi. FilePath boş veya null.");
            }
            
            if (!File.Exists(FilePath))
            {
                throw new FileNotFoundException($"SRT dosyası bulunamadı. Aranan yol: '{FilePath}'");
            }
            
            // Dosya okuma izni kontrolü
            try
            {
                using var stream = File.OpenRead(FilePath);
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException($"Dosya okuma izni yok veya dosya kullanımda: {ex.Message}");
            }
        }

        private async Task ProcessStep_ValidateFormat(CancellationToken token)
        {
            await Task.Delay(300, token); // UI için kısa gecikme
            
            var extension = Path.GetExtension(FilePath).ToLowerInvariant();
            if (extension != ".srt")
            {
                throw new InvalidOperationException("Dosya geçerli bir SRT dosyası değil.");
            }
        }

        private async Task ProcessStep_CleanSubtitles(CancellationToken token)
        {
            _cleaningResult = await _srtCleaningService.CleanSRTFileAsync(FilePath, token);
        }

        private async Task ProcessStep_SaveOutput(CancellationToken token)
        {
            if (_cleaningResult == null)
                throw new InvalidOperationException("Temizleme sonucu bulunamadı.");

            var outputPath = GenerateOutputFilePath();
            
            // Dizin yazma izni kontrolü
            try
            {
                var tempTestFile = Path.Combine(Path.GetDirectoryName(outputPath) ?? "", "test_write_permission.tmp");
                await File.WriteAllTextAsync(tempTestFile, "test", token);
                File.Delete(tempTestFile);
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException($"Çıktı dizinine yazma izni yok: {ex.Message}");
            }

            await _srtCleaningService.SaveCleanedSRTAsync(outputPath, _cleaningResult.CleanedEntries, token);
        }

        private async Task ProcessStep_Finalize(CancellationToken token)
        {
            await Task.Delay(200, token); // UI için kısa gecikme
            // Son kontroller burada yapılabilir
        }

        private int GetStepProcessingTime(int stepIndex)
        {
            // Her adım için farklı işlem süreleri (milisaniye)
            return stepIndex switch
            {
                0 => 1000, // Dosya okuma
                1 => 800,  // Format kontrolü
                2 => 2000, // Betimleme temizliği
                3 => 1200, // Çıktı oluşturma
                4 => 500,  // Tamamlama
                _ => 1000
            };
        }

        private string GenerateOutputFileName()
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(FileName);
            var extension = Path.GetExtension(FileName);
            return $"{nameWithoutExtension}_temizlendi{extension}";
        }

        private string GenerateOutputFilePath()
        {
            // macOS'ta Desktop klasörüne yazma izni olmayabilir, o yüzden temp klasör kullan
            var tempDirectory = Path.GetTempPath();
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(FilePath);
            var extension = Path.GetExtension(FilePath);
            var outputPath = Path.Combine(tempDirectory, $"{nameWithoutExtension}_temizlendi{extension}");
            
            return outputPath;
        }

        [RelayCommand]
        private void Cancel()
        {
            try
            {
                if (_cancellationTokenSource?.IsCancellationRequested == false)
                {
                    _cancellationTokenSource.Cancel();
                    CanCancel = false;
                    StatusMessage = "İşlem iptal ediliyor...";
                    ProgressText = "İptal ediliyor...";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Cancel hatası: {ex.Message}");
                CanCancel = false;
            }
        }
    }

    public partial class ProcessingStep : ObservableObject
    {
        [ObservableProperty]
        private string stepName;

        [ObservableProperty]
        private ProcessingStepStatus status;

        [ObservableProperty]
        private string duration = "";

        [ObservableProperty]
        private bool showDuration = false;

        public string StatusIcon => Status switch
        {
            ProcessingStepStatus.Pending => "⏳",
            ProcessingStepStatus.InProgress => "⚡",
            ProcessingStepStatus.Completed => "✓",
            ProcessingStepStatus.Failed => "✗",
            _ => "⏳"
        };

        public Color StatusColor => Status switch
        {
            ProcessingStepStatus.Pending => Colors.Gray,
            ProcessingStepStatus.InProgress => Colors.Orange,
            ProcessingStepStatus.Completed => Colors.Green,
            ProcessingStepStatus.Failed => Colors.Red,
            _ => Colors.Gray
        };

        public ProcessingStep(string stepName, ProcessingStepStatus status = ProcessingStepStatus.Pending)
        {
            StepName = stepName;
            Status = status;
        }
    }

    public enum ProcessingStepStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }

    public class SRTProcessingCompletedEventArgs : EventArgs
    {
        public string OperationName { get; }
        public string InputFileName { get; }
        public string OutputFileName { get; }
        public string Message { get; }
        public SRTCleaningResult? CleaningResult { get; }

        public SRTProcessingCompletedEventArgs(string operationName, string inputFileName, string outputFileName, string message, SRTCleaningResult? cleaningResult = null)
        {
            OperationName = operationName;
            InputFileName = inputFileName;
            OutputFileName = outputFileName;
            Message = message;
            CleaningResult = cleaningResult;
        }
    }

    public class ProcessingFailedEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string DetailedMessage { get; }

        public ProcessingFailedEventArgs(Exception exception, string detailedMessage)
        {
            Exception = exception;
            DetailedMessage = detailedMessage;
        }
    }
} 