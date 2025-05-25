using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Services;

namespace LocalizationTabii.PageModels
{
    public partial class PromptsManagementPageModel : ObservableObject
    {
        private readonly ModalErrorHandler _errorHandler;


        public PromptsManagementPageModel(ModalErrorHandler errorHandler)
        {
            _errorHandler = errorHandler;
        }



    }
}
