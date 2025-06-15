using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace LocalizationTabii.ComponentModel
{
    public partial class SRTResultViewModel : ObservableObject
    {
        [ObservableProperty]
        private string operationName = string.Empty;

        [ObservableProperty]
        private string inputFileName = string.Empty;

        [ObservableProperty]
        private string outputFileName = string.Empty;

        [ObservableProperty]
        private string resultMessage = "SRT dosyanız başarıyla işlendi ve temizlendi.";

        [ObservableProperty]
        private string processingDuration = string.Empty;

        [ObservableProperty]
        private string inputFileSize = string.Empty;

        [ObservableProperty]
        private string outputFileSize = string.Empty;

        [ObservableProperty]
        private string cleanedLinesCount = "0";

        public event EventHandler? ViewFileRequested;
        public event EventHandler? SaveFileRequested;
        public event EventHandler? NewOperationRequested;
        public event EventHandler? GoToHomeRequested;

        public SRTResultViewModel()
        {
        }

        public void Initialize(string operationName, string inputFileName, string outputFileName, 
                             string message, TimeSpan processingDuration, 
                             string inputFileSize, string outputFileSize, int cleanedLines)
        {
            OperationName = operationName;
            InputFileName = inputFileName;
            OutputFileName = outputFileName;
            ResultMessage = message;
            ProcessingDuration = FormatDuration(processingDuration);
            InputFileSize = inputFileSize;
            OutputFileSize = outputFileSize;
            CleanedLinesCount = cleanedLines.ToString();
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMinutes >= 1)
                return $"{duration.Minutes}m {duration.Seconds}s";
            else if (duration.TotalSeconds >= 1)
                return $"{duration.TotalSeconds:F1}s";
            else
                return $"{duration.TotalMilliseconds:F0}ms";
        }

        [RelayCommand]
        private void ViewFile()
        {
            ViewFileRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void SaveFile()
        {
            SaveFileRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void NewOperation()
        {
            NewOperationRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void GoToHome()
        {
            GoToHomeRequested?.Invoke(this, EventArgs.Empty);
        }
    }
} 