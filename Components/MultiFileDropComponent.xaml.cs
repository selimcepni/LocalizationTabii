using LocalizationTabii.PageModels;

namespace LocalizationTabii.Components;

public partial class MultiFileDropComponent : ContentView
{
    public MultiFileDropComponent()
    {
        InitializeComponent();
    }

    public MultiFileDropComponent(AnalysisToolsPageModel viewModel) : this()
    {
        BindingContext = viewModel;
    }
} 