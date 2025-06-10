using Syncfusion.Maui.Toolkit.Popup;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Models;
using System.Collections.ObjectModel;

namespace LocalizationTabii.Components;

public partial class EditPromptPopup : SfPopup
{
    public EditPromptPopup()
    {
        InitializeComponent();
        BindingContext = new EditPromptPopupViewModel();
    }

    public Task<Prompt?> ShowPopupAsync(Prompt prompt)
    {
        var viewModel = (EditPromptPopupViewModel)BindingContext;
        return viewModel.ShowAsync(prompt);
    }
}

public partial class EditPromptPopupViewModel : ObservableObject
{
    private TaskCompletionSource<Prompt?>? _taskCompletionSource;
    private Prompt? _originalPrompt;

    public EditPromptPopupViewModel()
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
    private CategoryOption? selectedCategory;

    [ObservableProperty]
    private LanguageOption? selectedLanguage;

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private DateTime updatedAt;

    [ObservableProperty]
    private int usageCount;

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



    public Task<Prompt?> ShowAsync(Prompt prompt)
    {
        LoadPrompt(prompt);
        _taskCompletionSource = new TaskCompletionSource<Prompt?>();
        IsPopupOpen = true;
        return _taskCompletionSource.Task;
    }

    public void LoadPrompt(Prompt prompt)
    {
        _originalPrompt = prompt;
        
        Title = prompt.Title;
        Content = prompt.Content;
        
        // Mevcut kategori ve dili seç
        SelectedCategory = Categories.FirstOrDefault(c => c.Name == prompt.Category);
        SelectedLanguage = Languages.FirstOrDefault(l => l.Name == prompt.Language);
        
        CreatedAt = prompt.CreatedAt;
        UpdatedAt = prompt.UpdatedAt;
        UsageCount = prompt.UsageCount;
        
        ClearError();
        Result = null;
    }

    [RelayCommand]
    private void Update()
    {
        if (_originalPrompt == null)
        {
            ShowError("Düzenlenecek prompt bulunamadı.");
            return;
        }

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

            // Prompt'ı güncelle
            _originalPrompt.Title = Title.Trim();
            _originalPrompt.Content = Content.Trim();
            _originalPrompt.Category = SelectedCategory.Name;
            _originalPrompt.Language = SelectedLanguage.Name;
            _originalPrompt.UpdatedAt = DateTime.Now;

            Result = _originalPrompt;

            // Popup'ı kapat
            IsPopupOpen = false;
            _taskCompletionSource?.SetResult(Result);
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
            // Popup dışarıdan kapatıldıysa (close button vs.)
            Cancel();
        }
    }
} 