using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace LocalizationTabii.ComponentModel
{
    public partial class ChooseModelViewModel : ObservableObject
    {
        [ObservableProperty]
        private string selectedFileName = string.Empty;

        [ObservableProperty]
        private string fileSize = string.Empty;

        [ObservableProperty]
        private bool isGpt4Selected = true; // Varsayılan olarak GPT-4 seçili

        [ObservableProperty]
        private bool isGpt35Selected = false;

        public event EventHandler<ModelSelectedEventArgs>? ModelSelected;
        public event EventHandler? GoBackRequested;

        public ChooseModelViewModel()
        {
            // GPT-4 varsayılan olarak seçili
            IsGpt4Selected = true;
        }

        public void SetFileInfo(string fileName, string fileSize)
        {
            SelectedFileName = fileName;
            FileSize = fileSize;
        }

        partial void OnIsGpt4SelectedChanged(bool value)
        {
            if (value)
            {
                IsGpt35Selected = false;
            }
        }

        partial void OnIsGpt35SelectedChanged(bool value)
        {
            if (value)
            {
                IsGpt4Selected = false;
            }
        }

        [RelayCommand]
        private void GoBack()
        {
            GoBackRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private async Task StartTranslation()
        {
            var selectedModel = IsGpt4Selected ? "gpt-4-turbo" : "gpt-3.5-turbo";
            
            // Model seçimi event'ini fırlat
            ModelSelected?.Invoke(this, new ModelSelectedEventArgs(selectedModel, SelectedFileName, FileSize));
            
     
        }
    }

    public class ModelSelectedEventArgs : EventArgs
    {
        public string SelectedModel { get; }
        public string FileName { get; }
        public string FileSize { get; }

        public ModelSelectedEventArgs(string selectedModel, string fileName, string fileSize)
        {
            SelectedModel = selectedModel;
            FileName = fileName;
            FileSize = fileSize;
        }
    }
} 