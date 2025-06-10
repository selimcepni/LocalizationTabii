using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Services;
using LocalizationTabii.ComponentModel;

namespace LocalizationTabii.PageModels
{
    public partial class TranslatePageModel : ObservableObject
    {
        private readonly ModalErrorHandler _errorHandler;

        [ObservableProperty]
        private FileResult? selectedFile;

        [ObservableProperty]
        private string selectedFileName = string.Empty;

        [ObservableProperty]
        private bool isFileSelected;

        public TranslatePageModel(ModalErrorHandler errorHandler)
        {
            _errorHandler = errorHandler;
        }

        public void HandleFileSelected(FileResult fileResult)
        {
            try
            {
                SelectedFile = fileResult;
                SelectedFileName = fileResult.FileName;
                IsFileSelected = true;
                
                // Here you can add additional file processing logic
                // For example: parse SRT/VTT content, validate format, etc.
            }
            catch (Exception ex)
            {
                _errorHandler?.ShowError("Dosya İşleme Hatası", ex.Message);
            }
        }

        [RelayCommand]
        private void ClearSelectedFile()
        {
            SelectedFile = null;
            SelectedFileName = string.Empty;
            IsFileSelected = false;
        }
    }
}
