using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace LocalizationTabii.ComponentModel
{
    public partial class ChooseSRTOperationViewModel : ObservableObject
    {
        [ObservableProperty]
        private string selectedFileName = string.Empty;

        [ObservableProperty]
        private string fileSize = string.Empty;

        [ObservableProperty]
        private bool isDescriptionCleaningSelected = true; // Varsayılan olarak betimleme temizliği seçili

        public event EventHandler<SRTOperationSelectedEventArgs>? OperationSelected;
        public event EventHandler? GoBackRequested;

        public ChooseSRTOperationViewModel()
        {
            // Betimleme Temizliği varsayılan olarak seçili
            IsDescriptionCleaningSelected = true;
        }

        public void SetFileInfo(string fileName, string fileSize)
        {
            SelectedFileName = fileName;
            FileSize = fileSize;
        }

        [RelayCommand]
        private void GoBack()
        {
            GoBackRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private async Task StartOperation()
        {
            var selectedOperation = "Betimleme Temizliği"; // Şimdilik tek seçenek
            
            // İşlem seçimi event'ini fırlat
            OperationSelected?.Invoke(this, new SRTOperationSelectedEventArgs(selectedOperation, SelectedFileName, FileSize));
        }
    }

    public class SRTOperationSelectedEventArgs : EventArgs
    {
        public string OperationName { get; }
        public string FileName { get; }
        public string FileSize { get; }

        public SRTOperationSelectedEventArgs(string operationName, string fileName, string fileSize)
        {
            OperationName = operationName;
            FileName = fileName;
            FileSize = fileSize;
        }
    }
} 