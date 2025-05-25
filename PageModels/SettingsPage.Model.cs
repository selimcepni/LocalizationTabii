using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Services;

namespace LocalizationTabii.PageModels
{
    public partial class SettingsPageModel : ObservableObject
    {
        private readonly ModalErrorHandler _errorHandler;

        public SettingsPageModel(ModalErrorHandler errorHandler)
        {
            _errorHandler = errorHandler;
        }



    }
}
