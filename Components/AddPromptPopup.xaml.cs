using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Models;
using System.Collections.ObjectModel;

namespace LocalizationTabii.Components;

public partial class AddPromptPopup : Syncfusion.Maui.Toolkit.Popup.SfPopup
{
    public AddPromptPopup()
    {
        InitializeComponent();
        BindingContext = new AddPromptPopupViewModel();
    }

    public Task<Prompt?> ShowPopupAsync()
    {
        var viewModel = (AddPromptPopupViewModel)BindingContext;
        return viewModel.ShowAsync();
    }
}

public partial class AddPromptPopupViewModel : ObservableObject
{
    private TaskCompletionSource<Prompt?>? _taskCompletionSource;

    public AddPromptPopupViewModel()
    {
        LoadData();
    }

    [ObservableProperty]
    private bool isPopupOpen;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string content = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private CategoryOption? selectedCategory;

    [ObservableProperty]
    private LanguageOption? selectedLanguage;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool hasError;

    public ObservableCollection<CategoryOption> Categories { get; } = new();
    public ObservableCollection<LanguageOption> Languages { get; } = new();

    public Prompt? Result { get; private set; }

    private void LoadData()
    {
        // Kategoriler - Sadece 3 kategori
        Categories.Clear();
        Categories.Add(new CategoryOption { Code = "translation", Name = "Çeviri" });
        Categories.Add(new CategoryOption { Code = "preprocessing", Name = "Ön Düzenleme" });
        Categories.Add(new CategoryOption { Code = "technical", Name = "Teknik" });

        // Diller - Sadece 5 dil
        Languages.Clear();
        Languages.Add(new LanguageOption { Code = "tr", Name = "Türkçe" });
        Languages.Add(new LanguageOption { Code = "en", Name = "İngilizce" });
        Languages.Add(new LanguageOption { Code = "ar", Name = "Arapça" });
        Languages.Add(new LanguageOption { Code = "es", Name = "İspanyolca" });
        Languages.Add(new LanguageOption { Code = "ur", Name = "Urduca" });
    }

    public Task<Prompt?> ShowAsync()
    {
        Reset();
        _taskCompletionSource = new TaskCompletionSource<Prompt?>();
        IsPopupOpen = true;
        return _taskCompletionSource.Task;
    }

    public void Reset()
    {
        Title = string.Empty;
        Content = string.Empty;
        Description = string.Empty;
        SelectedCategory = null;
        SelectedLanguage = null;
        ErrorMessage = null;
        HasError = false;
        Result = null;
    }

    [RelayCommand]
    private async void Save()
    {
        // Validasyon
        if (string.IsNullOrWhiteSpace(Title))
        {
            ShowError("Başlık alanı zorunludur.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Content))
        {
            ShowError("İçerik alanı zorunludur.");
            return;
        }

        if (SelectedCategory == null)
        {
            ShowError("Kategori seçimi zorunludur.");
            return;
        }

        if (SelectedLanguage == null)
        {
            ShowError("Dil seçimi zorunludur.");
            return;
        }

        try
        {
            ClearError();

            // Yeni prompt oluştur
            Result = new Prompt
            {
                Title = Title.Trim(),
                Content = Content.Trim(),
                Category = SelectedCategory.Name,
                Language = SelectedLanguage.Name,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = true,
                UsageCount = 0
            };

            // Önce result'u set et
            _taskCompletionSource?.SetResult(Result);
            
            // Sonra popup'ı kapat
            await Task.Delay(100); // Kısa bir delay
            IsPopupOpen = false;
        }
        catch (Exception ex)
        {
            ShowError($"Beklenmeyen bir hata oluştu: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        IsPopupOpen = false;
        _taskCompletionSource?.SetResult(null);
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = null;
        HasError = false;
    }

    partial void OnIsPopupOpenChanged(bool value)
    {
        if (!value && _taskCompletionSource != null && !_taskCompletionSource.Task.IsCompleted)
        {
            // Popup dışarıdan kapatıldıysa (close button vs.) ve henüz result set edilmemişse
            _taskCompletionSource?.SetResult(null);
        }
    }
}