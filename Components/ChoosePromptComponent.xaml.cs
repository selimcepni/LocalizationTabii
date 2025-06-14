using LocalizationTabii.ComponentModel;

namespace LocalizationTabii.Components;

public partial class ChoosePromptComponent : ContentView
{
    public ChoosePromptComponent()
    {
        InitializeComponent();
        BindingContext = new ChoosePromptViewModel();
    }

    public void SetFileAndModelInfo(string fileName, string fileSize, string selectedModel)
    {
        if (BindingContext is ChoosePromptViewModel viewModel)
        {
            viewModel.SetFileAndModelInfo(fileName, fileSize, selectedModel);
        }
    }
} 