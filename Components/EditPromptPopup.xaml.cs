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

    public async Task<Prompt?> ShowPopupAsync(Prompt prompt)
    {
        var viewModel = (EditPromptPopupViewModel)BindingContext;
        return await viewModel.ShowAsync(prompt);
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

    public async Task<Prompt?> ShowAsync(Prompt prompt)
    {
        // Önceki TaskCompletionSource'u temizle
        if (_taskCompletionSource != null && !_taskCompletionSource.Task.IsCompleted)
        {
            _taskCompletionSource.SetResult(null);
        }
        
        LoadPrompt(prompt);
        _taskCompletionSource = new TaskCompletionSource<Prompt?>();
        
        IsPopupOpen = true;
        
        return await _taskCompletionSource.Task;
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
    private async Task Update()
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

            // Prompt'ı güncelle - yeni bir kopya oluştur
            var updatedPrompt = new Prompt
            {
                Id = _originalPrompt.Id,
                Title = Title.Trim(),
                Content = Content.Trim(),
                Category = SelectedCategory.Name,
                Language = SelectedLanguage.Name,
                CreatedAt = _originalPrompt.CreatedAt, // Orijinal oluşturma tarihini koru
                UpdatedAt = DateTime.Now,
                UsageCount = _originalPrompt.UsageCount, // Kullanım sayısını koru
                IsActive = _originalPrompt.IsActive
            };

            Result = updatedPrompt;

            // TaskCompletionSource'u tamamla
            var tcs = _taskCompletionSource;
            _taskCompletionSource = null;
            
            // Popup'ı kapat
            IsPopupOpen = false;
            
            // Sonucu döndür
            tcs?.SetResult(Result);
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
        
        var tcs = _taskCompletionSource;
        _taskCompletionSource = null;
        
        IsPopupOpen = false;
        tcs?.SetResult(null);
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