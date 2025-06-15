using LocalizationTabii.ComponentModel;

namespace LocalizationTabii.Components;

public partial class ProcessingSRTComponent : ContentView
{
    public ProcessingSRTComponent()
    {
        InitializeComponent();
    }

    public ProcessingSRTComponent(ProcessingSRTViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }
} 