using LocalizationTabii.ComponentModel;

namespace LocalizationTabii.Components;

public partial class SRTResultComponent : ContentView
{
    public SRTResultComponent()
    {
        InitializeComponent();
    }

    public SRTResultComponent(SRTResultViewModel viewModel) : this()
    {
        BindingContext = viewModel;
    }
} 