using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Models;
using LocalizationTabii.Services;
using Syncfusion.Maui.Popup;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Syncfusion.Maui.ListView;

namespace LocalizationTabii.Components;

public partial class ChoosePromptPopup : SfPopup, INotifyPropertyChanged
{
    private readonly IPromptStorageService _promptStorageService;
    private TaskCompletionSource<Prompt?> _taskCompletionSource;
    private ObservableCollection<Prompt> _prompts;
    private bool _isLoading;
    private bool _isEmpty;

    public ObservableCollection<Prompt> Prompts
    {
        get => _prompts;
        set
        {
            _prompts = value;
            OnPropertyChanged();
            IsEmpty = _prompts?.Count == 0;
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        set
        {
            _isEmpty = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadPromptsCommand { get; }
    public ICommand SelectPromptCommand { get; }

    public ChoosePromptPopup()
    {
        InitializeComponent();
        
        _promptStorageService = MauiProgram.ServiceProvider?.GetService<IPromptStorageService>();
        _prompts = new ObservableCollection<Prompt>();
        
        LoadPromptsCommand = new Command(async () => await LoadPromptsAsync());
        SelectPromptCommand = new Command<Prompt>(async (prompt) => await SelectPrompt(prompt));
        
        BindingContext = this;
    }

    public async Task<Prompt?> ShowAsync()
    {
        try
        {
            _taskCompletionSource = new TaskCompletionSource<Prompt?>();
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsOpen = true;
            });
            
            await LoadPromptsAsync();
            
            return await _taskCompletionSource.Task;
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Application.Current?.MainPage?.DisplayAlert("Hata", $"Popup açılırken hata: {ex.Message}", "Tamam");
            });
            return null;
        }
    }

    private async Task LoadPromptsAsync()
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsLoading = true;
            });

            if (_promptStorageService != null)
            {
                await _promptStorageService.InitializeDatabaseAsync();
                var result = await _promptStorageService.GetPromptsPagedAsync(1, 10);
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Prompts.Clear();
                    foreach (var prompt in result.Items)
                    {
                        Prompts.Add(prompt);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Application.Current?.MainPage?.DisplayAlert("Hata", $"Promptlar yüklenirken hata: {ex.Message}", "Tamam");
            });
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsLoading = false;
            });
        }
    }

    private async void OnSelectionChanged(object sender, ItemSelectionChangedEventArgs e)
    {
        try
        {
            if (e.AddedItems?.Count > 0 && e.AddedItems[0] is Prompt selectedPrompt)
            {
                await SelectPrompt(selectedPrompt);
            }
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Application.Current?.MainPage?.DisplayAlert("Hata", $"Seçim işleminde hata: {ex.Message}", "Tamam");
            });
        }
    }

    private async Task SelectPrompt(Prompt prompt)
    {
        try
        {
            if (prompt != null && _promptStorageService != null)
            {
                // Usage count'u artır
                prompt.UsageCount++;
                await _promptStorageService.UpdatePromptAsync(prompt);
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsOpen = false;
                });
                
                _taskCompletionSource?.SetResult(prompt);
            }
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Application.Current?.MainPage?.DisplayAlert("Hata", $"Prompt seçilirken hata: {ex.Message}", "Tamam");
            });
        }
    }

    private async Task ClosePopup()
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsOpen = false;
            });
            
            _taskCompletionSource?.SetResult(null);
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Application.Current?.MainPage?.DisplayAlert("Hata", $"Popup kapatılırken hata: {ex.Message}", "Tamam");
            });
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 