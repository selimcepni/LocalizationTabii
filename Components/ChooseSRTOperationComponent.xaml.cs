using LocalizationTabii.ComponentModel;

namespace LocalizationTabii.Components
{
    public partial class ChooseSRTOperationComponent : ContentView
    {
        private ChooseSRTOperationViewModel? _viewModel;

        public ChooseSRTOperationComponent()
        {
            InitializeComponent();
            _viewModel = new ChooseSRTOperationViewModel();
            BindingContext = _viewModel;
        }

        public void SetFileInfo(string fileName, string fileSize)
        {
            _viewModel?.SetFileInfo(fileName, fileSize);
        }
    }
} 