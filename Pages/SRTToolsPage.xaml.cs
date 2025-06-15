using Microsoft.Maui.Controls;
using LocalizationTabii.PageModels;
using LocalizationTabii.Components;
using LocalizationTabii.ComponentModel;

namespace LocalizationTabii.Pages
{
    public partial class SRTToolsPage : ContentPage
    {
        private readonly SRTToolsPageModel _viewModel;
        
        public SRTToolsPage(SRTToolsPageModel model)
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
            
            if (ChooseSRTOperationComponent != null)
            {
                // ChooseSRTOperationComponent'in ViewModel'ine erişim
                if (ChooseSRTOperationComponent.BindingContext is ChooseSRTOperationViewModel chooseSRTOperationViewModel)
                {
                    chooseSRTOperationViewModel.GoBackRequested += OnGoBackRequested;
                    chooseSRTOperationViewModel.OperationSelected += OnOperationSelected;
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
            // FileDragDropComponent'i gizle, ChooseSRTOperationComponent'i göster
            FileDropComponent.IsVisible = false;
            ChooseSRTOperationComponent.IsVisible = true;
            
            // Dosya bilgilerini ChooseSRTOperationComponent'e aktar
            ChooseSRTOperationComponent.SetFileInfo(e.FileName, e.FileSize);
        }

        private void OnGoBackRequested(object? sender, EventArgs e)
        {
            // ChooseSRTOperationComponent'i gizle, FileDragDropComponent'i göster
            ChooseSRTOperationComponent.IsVisible = false;
            FileDropComponent.IsVisible = true;
        }

        private async void OnOperationSelected(object? sender, SRTOperationSelectedEventArgs e)
        {
            // Burada seçilen operasyona göre işlem yapılacak
            await DisplayAlert("İşlem Seçildi", $"{e.OperationName} işlemi seçildi", "Tamam");
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
            
            if (ChooseSRTOperationComponent != null)
            {
                // ChooseSRTOperationComponent'in ViewModel'inden event'leri temizle
                if (ChooseSRTOperationComponent.BindingContext is ChooseSRTOperationViewModel chooseSRTOperationViewModel)
                {
                    chooseSRTOperationViewModel.GoBackRequested -= OnGoBackRequested;
                    chooseSRTOperationViewModel.OperationSelected -= OnOperationSelected;
                }
            }
        }
    }
} 