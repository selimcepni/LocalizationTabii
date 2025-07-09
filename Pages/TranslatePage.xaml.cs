using Microsoft.Maui.Controls;
using LocalizationTabii.PageModels;
using LocalizationTabii.Components;
using LocalizationTabii.ComponentModel;
using LocalizationTabii.Models;

namespace LocalizationTabii.Pages
{
    public partial class TranslatePage : ContentPage
    {
        private readonly TranslatePageModel _viewModel;
        
        public TranslatePage(TranslatePageModel model)
        {
            InitializeComponent();
            _viewModel = model;
            BindingContext = _viewModel;
            
            if (FileDropComponent != null)
            {
                FileDropComponent.FileSelected += OnFileSelected;
                
                // FileDragDropComponent'in ViewModel'ine erişim
                if (FileDropComponent.BindingContext is FileDragDropViewModel fileDragViewModel)
                {
                    fileDragViewModel.ContinueRequested += OnContinueRequested;
                }
            }
            
            if (ChooseModelComponent != null)
            {
                // ChooseModelComponent'in ViewModel'ine erişim
                if (ChooseModelComponent.BindingContext is ChooseModelViewModel chooseModelViewModel)
                {
                    chooseModelViewModel.GoBackRequested += OnGoBackRequested;
                    chooseModelViewModel.ModelSelected += OnModelSelected;
                }
            }
            
            if (ChoosePromptComponent != null)
            {
                // ChoosePromptComponent'in ViewModel'ine erişim
                if (ChoosePromptComponent.BindingContext is ChoosePromptViewModel choosePromptViewModel)
                {
                    choosePromptViewModel.GoBackRequested += OnPromptGoBackRequested;
                    choosePromptViewModel.PromptSelected += OnPromptSelected;
                }
            }
        }

        private async void OnFileSelected(object? sender, FileSelectedEventArgs e)
        {
            try
            {
                _viewModel.HandleFileSelected(e.FileResult);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Dosya işleme hatası: {ex.Message}", "Tamam");
            }
        }

        private void OnContinueRequested(object? sender, ContinueEventArgs e)
        {
            // FileDragDropComponent'i gizle, ChooseModelComponent'i göster
            FileDropComponent.IsVisible = false;
            ChooseModelComponent.IsVisible = true;
            
            // Dosya bilgilerini ChooseModelComponent'e aktar
            ChooseModelComponent.SetFileInfo(e.FileName, e.FileSize);
        }

        private void OnGoBackRequested(object? sender, EventArgs e)
        {
            // ChooseModelComponent'i gizle, FileDragDropComponent'i göster
            ChooseModelComponent.IsVisible = false;
            FileDropComponent.IsVisible = true;
        }

        private void OnModelSelected(object? sender, ModelSelectedEventArgs e)
        {
            // String'den ModelConfiguration oluştur
            var modelConfig = new ModelConfiguration
            {
                Identifier = e.SelectedModel,
                Provider = GetProviderFromModel(e.SelectedModel),
                ModelId = e.SelectedModel,
                DisplayName = GetDisplayNameFromModel(e.SelectedModel)
            };
            
            // Session'a seçilen modeli set et
            _viewModel.SetSelectedModel(modelConfig);
            
            // ChooseModelComponent'i gizle, ChoosePromptComponent'i göster
            ChooseModelComponent.IsVisible = false;
            ChoosePromptComponent.IsVisible = true;
            
            // Dosya ve model bilgilerini ChoosePromptComponent'e aktar
            ChoosePromptComponent.SetFileAndModelInfo(e.FileName, e.FileSize, e.SelectedModel);
        }

        private void OnPromptGoBackRequested(object? sender, EventArgs e)
        {
            // ChoosePromptComponent'i gizle, ChooseModelComponent'i göster
            ChoosePromptComponent.IsVisible = false;
            ChooseModelComponent.IsVisible = true;
        }

        private async void OnPromptSelected(object? sender, PromptSelectedEventArgs e)
        {
            try
            {
                // String'den Prompt objesi oluştur (basit versiyonu)
                var prompt = new Prompt
                {
                    Title = e.PromptTitle,
                    Content = "Dummy prompt content", // TODO: Gerçek prompt'u al
                    Language = "Turkish", // TODO: Gerçek dili al
                    Category = "Translation", // TODO: Gerçek kategoriyi al
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                
                // Session'a seçilen prompt'u set et
                _viewModel.SetSelectedPrompt(prompt);
                
                // Kullanıcı context'i yoksa boş string
                _viewModel.SetUserContext(string.Empty);
                
                // Session'ı çeviriye hazır hale getir
                _viewModel.PrepareForTranslationCommand.Execute(null);
                
                // Kullanıcıya çeviriyi başlatma seçeneği sun
                var shouldStart = await DisplayAlert("Çeviri Hazır", 
                    $"Session çeviriye hazır:\n\n{_viewModel.SessionInfo}\n\nÇeviriyi başlatmak istiyor musunuz?", 
                    "Başlat", "İptal");
                
                if (shouldStart)
                {
                    await _viewModel.StartTranslationCommand.ExecuteAsync(null);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Prompt işleme hatası: {ex.Message}", "Tamam");
            }
        }

        private string GetProviderFromModel(string modelId)
        {
            return modelId switch
            {
                "gpt-4-turbo" or "gpt-3.5-turbo" or "gpt-4" or "gpt-4o" or "gpt-4o-mini" => "OpenAI",
                _ => "Unknown"
            };
        }

        private string GetDisplayNameFromModel(string modelId)
        {
            return modelId switch
            {
                "gpt-4-turbo" => "GPT-4 Turbo",
                "gpt-3.5-turbo" => "GPT-3.5 Turbo", 
                "gpt-4" => "GPT-4",
                "gpt-4o" => "GPT-4o",
                "gpt-4o-mini" => "GPT-4o Mini",
                _ => modelId
            };
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            if (FileDropComponent != null)
            {
                FileDropComponent.FileSelected -= OnFileSelected;
                
                // FileDragDropComponent'in ViewModel'inden event'leri temizle
                if (FileDropComponent.BindingContext is FileDragDropViewModel fileDragViewModel)
                {
                    fileDragViewModel.ContinueRequested -= OnContinueRequested;
                }
            }
            
            if (ChooseModelComponent != null)
            {
                // ChooseModelComponent'in ViewModel'inden event'leri temizle
                if (ChooseModelComponent.BindingContext is ChooseModelViewModel chooseModelViewModel)
                {
                    chooseModelViewModel.GoBackRequested -= OnGoBackRequested;
                    chooseModelViewModel.ModelSelected -= OnModelSelected;
                }
            }
            
            if (ChoosePromptComponent != null)
            {
                // ChoosePromptComponent'in ViewModel'inden event'leri temizle
                if (ChoosePromptComponent.BindingContext is ChoosePromptViewModel choosePromptViewModel)
                {
                    choosePromptViewModel.GoBackRequested -= OnPromptGoBackRequested;
                    choosePromptViewModel.PromptSelected -= OnPromptSelected;
                }
            }
        }
    }
}