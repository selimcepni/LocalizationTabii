using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Models;
using Syncfusion.Maui.Popup;

namespace LocalizationTabii.Components;

public partial class ChoosePromptPopup : SfPopup
{
    public ChoosePromptPopup()
    {
        InitializeComponent();
        BindingContext = new ChoosePromptPopupViewModel();
    }

    public Task<Prompt?> ShowPopupAsync()
    {
        var viewModel = (ChoosePromptPopupViewModel)BindingContext;
        return viewModel.ShowAsync();
    }

    public Task<Prompt?> ShowPopupAsync(string fileName, string fileSize, string selectedModel)
    {
        var viewModel = (ChoosePromptPopupViewModel)BindingContext;
        viewModel.SetFileAndModelInfo(fileName, fileSize, selectedModel);
        return viewModel.ShowAsync();
    }
}

public partial class ChoosePromptPopupViewModel : ObservableObject
{
    private TaskCompletionSource<Prompt?>? _taskCompletionSource;

    [ObservableProperty]
    private bool isPopupOpen;

    [ObservableProperty]
    private string selectedFileName = string.Empty;

    [ObservableProperty]
    private string fileSize = string.Empty;

    [ObservableProperty]
    private string selectedModel = string.Empty;

    [ObservableProperty]
    private string selectedModelDisplayName = string.Empty;

    public Prompt? Result { get; private set; }

    public ChoosePromptPopupViewModel()
    {
    }

    public void SetFileAndModelInfo(string fileName, string fileSize, string selectedModel)
    {
        SelectedFileName = fileName;
        FileSize = fileSize;
        SelectedModel = selectedModel;
        
        // Model display name'ini belirle
        SelectedModelDisplayName = selectedModel switch
        {
            "gpt-4-turbo" => "GPT-4 Turbo",
            "gpt-3.5-turbo" => "GPT-3.5 Turbo",
            _ => selectedModel
        };
    }

    public Task<Prompt?> ShowAsync()
    {
        Reset();
        _taskCompletionSource = new TaskCompletionSource<Prompt?>();
        IsPopupOpen = true;
        return _taskCompletionSource.Task;
    }

    private void Reset()
    {
        Result = null;
    }

    [RelayCommand]
    private void ClosePopup()
    {
        IsPopupOpen = false;
        _taskCompletionSource?.SetResult(null);
    }

    // Popup kapatıldığında otomatik çağrılacak
    partial void OnIsPopupOpenChanged(bool value)
    {
        if (!value && _taskCompletionSource != null && !_taskCompletionSource.Task.IsCompleted)
        {
            _taskCompletionSource.SetResult(Result);
        }
    }
} 