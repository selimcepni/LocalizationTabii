using LocalizationTabii.PageModels;

namespace LocalizationTabii.Components;

public partial class ProjectListComponent : ContentView
{
    public ProjectListComponent()
    {
        InitializeComponent();
    }

    public ProjectListComponent(AnalysisToolsPageModel viewModel) : this()
    {
        BindingContext = viewModel;
    }
} 