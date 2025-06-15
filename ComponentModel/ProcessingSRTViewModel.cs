using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LocalizationTabii.ComponentModel
{
    public partial class ProcessingSRTViewModel : ObservableObject
    {
        [ObservableProperty]
        private string operationName = string.Empty;

        [ObservableProperty]
        private string fileName = string.Empty;

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

        public event EventHandler<SRTProcessingCompletedEventArgs>? ProcessingCompleted;
        public event EventHandler? ProcessingCancelled;

        public ProcessingSRTViewModel()
        {
            InitializeSteps();
        }

        public void Initialize(string operationName, string fileName)
        {
            OperationName = operationName;
            FileName = fileName;
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
                            OperationName, FileName, outputFileName, "İşlem başarıyla tamamlandı"));
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
                    
                    System.Diagnostics.Debug.WriteLine($"❌ ProcessingSRTViewModel hatası: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
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
                
                // Simüle edilmiş işlem süresi
                var processingTime = GetStepProcessingTime(i);
                var steps = 20; // Her adım için 20 alt adım
                
                for (int j = 0; j < steps; j++)
                {
                    token.ThrowIfCancellationRequested();
                    
                    await Task.Delay(processingTime / steps, token);
                    
                    var stepProgress = (double)(j + 1) / steps / ProcessingSteps.Count * 100;
                    ProgressValue = baseProgress + stepProgress;
                }
                
                // Adımı tamamla
                step.Status = ProcessingStepStatus.Completed;
                stepDuration = DateTime.Now - startTime;
                step.Duration = $"{stepDuration.TotalSeconds:F1}s";
                step.ShowDuration = true;
            }
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

        public SRTProcessingCompletedEventArgs(string operationName, string inputFileName, string outputFileName, string message)
        {
            OperationName = operationName;
            InputFileName = inputFileName;
            OutputFileName = outputFileName;
            Message = message;
        }
    }
} 