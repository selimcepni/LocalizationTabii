using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Components;
using LocalizationTabii.Models;

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

        [ObservableProperty]
        private Prompt? selectedPrompt;

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
                "gpt-4" => "GPT-4",
                "gpt-4o" => "GPT-4o",
                "gpt-4o-mini" => "GPT-4o Mini",
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
            try
            {
                var popup = new ChoosePromptPopup();
                var result = await popup.ShowAsync();
                
                if (result != null)
                {
                    SelectedPrompt = result;
                    
                    // Prompt seçildi, event'i fırlat
                    PromptSelected?.Invoke(this, new PromptSelectedEventArgs(
                        result.Title, 
                        SelectedFileName, 
                        FileSize, 
                        SelectedModel));
                }
            }
            catch (Exception ex)
            {
                await Application.Current?.MainPage?.DisplayAlert("Hata", $"Prompt seçimi sırasında hata: {ex.Message}", "Tamam");
            }
        }
    }

    public class PromptSelectedEventArgs : EventArgs
    {
        public string PromptTitle { get; }
        public string FileName { get; }
        public string FileSize { get; }
        public string SelectedModel { get; }

        public PromptSelectedEventArgs(string promptTitle, string fileName, string fileSize, string selectedModel)
        {
            PromptTitle = promptTitle;
            FileName = fileName;
            FileSize = fileSize;
            SelectedModel = selectedModel;
        }
    }
} 