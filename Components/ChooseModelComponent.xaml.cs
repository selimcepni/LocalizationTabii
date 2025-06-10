using LocalizationTabii.ComponentModel;

namespace LocalizationTabii.Components;

public partial class ChooseModelComponent : ContentView
{
    public ChooseModelComponent()
    {
        InitializeComponent();
        BindingContext = new ChooseModelViewModel();
    }

    public void SetFileInfo(string fileName, string fileSize)
    {
        if (BindingContext is ChooseModelViewModel viewModel)
        {
            viewModel.SetFileInfo(fileName, fileSize);
        }
    }
} 