using LocalizationTabii.ComponentModel;

namespace LocalizationTabii.Components;

public partial class ChooseModelComponent : ContentView
{
    private readonly ChooseModelViewModel _viewModel;

    public event EventHandler<ModelSelectedEventArgs>? ModelSelected;
    public event EventHandler? GoBackRequested;

    public ChooseModelComponent()
    {
        InitializeComponent();
        
        // Dependency injection ile ViewModel'i al
        _viewModel = MauiProgram.ServiceProvider.GetRequiredService<ChooseModelViewModel>();
        BindingContext = _viewModel;
        
        // Event'leri parent'a forward et
        _viewModel.ModelSelected += (sender, e) => ModelSelected?.Invoke(this, e);
        _viewModel.GoBackRequested += (sender, e) => GoBackRequested?.Invoke(this, e);
    }

    public void SetFileInfo(string fileName, string fileSize)
    {
        _viewModel.SetFileInfo(fileName, fileSize);
    }
} 