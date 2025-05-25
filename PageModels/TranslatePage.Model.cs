using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Services;

namespace LocalizationTabii.PageModels
{
    public partial class TranslatePageModel : ObservableObject
    {
        private readonly ModalErrorHandler _errorHandler;

        [ObservableProperty]
        private bool _isPopupVisible;

        public TranslatePageModel(ModalErrorHandler errorHandler)
        {
            _errorHandler = errorHandler;
        }

        [RelayCommand]
        private void ShowPopup()
        {
            IsPopupVisible = true;
        }

        [RelayCommand]
        private void ClosePopup()
        {
            IsPopupVisible = false;
        }
    }
}
