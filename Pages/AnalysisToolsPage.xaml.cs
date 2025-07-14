using LocalizationTabii.PageModels;

namespace LocalizationTabii.Pages
{
    public partial class AnalysisToolsPage : ContentPage
    {
        private readonly AnalysisToolsPageModel _viewModel;
        
        public AnalysisToolsPage(AnalysisToolsPageModel model)
        {
            InitializeComponent();
            _viewModel = model;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Load projects when page appears
            await _viewModel.LoadProjectsCommand.ExecuteAsync(null);
        }
    }
} 