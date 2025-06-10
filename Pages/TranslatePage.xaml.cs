using Microsoft.Maui.Controls;
using LocalizationTabii.PageModels;
using LocalizationTabii.Components;
using LocalizationTabii.ComponentModel;

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

        private async void OnModelSelected(object? sender, ModelSelectedEventArgs e)
        {
            // Model seçildi, çeviri işlemini başlat
            await DisplayAlert("Model Seçildi", 
                $"Seçilen Model: {e.SelectedModel}\nDosya: {e.FileName}\nBoyut: {e.FileSize}", 
                "Tamam");
            
            // Burada çeviri işlemini başlatabilirsiniz
            // Örneğin: await _viewModel.StartTranslation(e.SelectedModel, e.FileName);
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
        }
    }
}