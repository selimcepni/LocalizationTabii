using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace LocalizationTabii.ComponentModel
{
    public partial class ChoosePromptViewModel : ObservableObject
    {
        [ObservableProperty]
        private string selectedFileName = string.Empty;

        [ObservableProperty]
        private string fileSize = string.Empty;

        [ObservableProperty]
        private string selectedModel = string.Empty;

        [ObservableProperty]
        private string selectedModelDisplayName = string.Empty;

        public event EventHandler<PromptSelectedEventArgs>? PromptSelected;
        public event EventHandler? GoBackRequested;

        public ChoosePromptViewModel()
        {
        }

        public void SetFileAndModelInfo(string fileName, string fileSize, string selectedModel)
        {
            SelectedFileName = fileName;
            FileSize = fileSize;
            SelectedModel = selectedModel;
            
            // Model display name'ini belirle
            SelectedModelDisplayName = selectedModel switch
            {
                "gpt-4-turbo" => "GPT-4 Turbo",
                "gpt-3.5-turbo" => "GPT-3.5 Turbo",
                _ => selectedModel
            };
        }

        [RelayCommand]
        private void GoBack()
        {
            GoBackRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private async Task SelectPrompt()
        {
            // Şimdilik basit bir alert göster
            await Application.Current?.MainPage?.DisplayAlert("Prompt Seçimi", 
                $"Prompt seçim özelliği yakında eklenecek.\nDosya: {SelectedFileName}\nModel: {SelectedModelDisplayName}", 
                "Tamam");
            
            // İleride prompt seçimi event'ini fırlat
            // PromptSelected?.Invoke(this, new PromptSelectedEventArgs(...));
        }
    }

    public class PromptSelectedEventArgs : EventArgs
    {
        public string SelectedPrompt { get; }
        public string FileName { get; }
        public string FileSize { get; }
        public string SelectedModel { get; }

        public PromptSelectedEventArgs(string selectedPrompt, string fileName, string fileSize, string selectedModel)
        {
            SelectedPrompt = selectedPrompt;
            FileName = fileName;
            FileSize = fileSize;
            SelectedModel = selectedModel;
        }
    }
} 