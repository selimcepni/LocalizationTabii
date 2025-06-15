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
    private readonly IPromptStorageService? _promptStorageService;
    private TaskCompletionSource<Prompt?>? _taskCompletionSource;
    private ObservableCollection<Prompt> _prompts;
    private ObservableCollection<Prompt> _filteredPrompts;
    private ObservableCollection<CategoryOption> _categories;
    private ObservableCollection<LanguageOption> _languages;
    private bool _isLoading;
    private bool _isEmpty;
    private string _searchText = string.Empty;
    private CategoryOption? _selectedCategory;
    private LanguageOption? _selectedLanguage;
    private string _emptyMessage = "Henüz prompt bulunamadı";
    private int _totalPrompts;

    public ObservableCollection<Prompt> Prompts
    {
        get => _prompts;
        set
        {
            _prompts = value;
            OnPropertyChanged();
            TotalPrompts = _prompts?.Count ?? 0;
            ApplyFilters();
        }
    }

    public ObservableCollection<Prompt> FilteredPrompts
    {
        get => _filteredPrompts;
        set
        {
            _filteredPrompts = value;
            OnPropertyChanged();
            IsEmpty = _filteredPrompts?.Count == 0;
        }
    }

    public ObservableCollection<CategoryOption> Categories
    {
        get => _categories;
        set
        {
            _categories = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<LanguageOption> Languages
    {
        get => _languages;
        set
        {
            _languages = value;
            OnPropertyChanged();
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

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            ApplyFilters();
        }
    }

    public CategoryOption? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasActiveFilters));
            ApplyFilters();
        }
    }

    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            _selectedLanguage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasActiveFilters));
            ApplyFilters();
        }
    }

    public string EmptyMessage
    {
        get => _emptyMessage;
        set
        {
            _emptyMessage = value;
            OnPropertyChanged();
        }
    }

    public int TotalPrompts
    {
        get => _totalPrompts;
        set
        {
            _totalPrompts = value;
            OnPropertyChanged();
        }
    }

    public bool HasActiveFilters => 
        !string.IsNullOrWhiteSpace(SearchText) || 
        (SelectedCategory != null && SelectedCategory.Code != "all") ||
        (SelectedLanguage != null && SelectedLanguage.Code != "all");

    public ICommand LoadPromptsCommand { get; }
    public ICommand SelectPromptCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    public ChoosePromptPopup()
    {
        InitializeComponent();
        
        _promptStorageService = MauiProgram.ServiceProvider?.GetService<IPromptStorageService>();
        _prompts = new ObservableCollection<Prompt>();
        _filteredPrompts = new ObservableCollection<Prompt>();
        _categories = new ObservableCollection<CategoryOption>();
        _languages = new ObservableCollection<LanguageOption>();
        
        LoadPromptsCommand = new Command(async () => await LoadPromptsAsync());
        SelectPromptCommand = new Command<Prompt>(async (prompt) => await SelectPrompt(prompt));
        SearchCommand = new Command<string>(OnSearchCommand);
        ClearFiltersCommand = new Command(ClearFilters);
        
        LoadFilterOptions();
        BindingContext = this;
        
        // Popup kapatılma event'ini handle et
        this.Closed += OnPopupClosed;
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        // Eğer TaskCompletionSource henüz tamamlanmadıysa, null değerle tamamla
        if (_taskCompletionSource != null && !_taskCompletionSource.Task.IsCompleted)
        {
            _taskCompletionSource.SetResult(null);
            _taskCompletionSource = null;
        }
    }

    private void OnSearchCommand(string? searchText)
    {
        SearchText = searchText ?? string.Empty;
    }

    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedCategory = Categories?.FirstOrDefault(c => c.Code == "all");
        SelectedLanguage = Languages?.FirstOrDefault(l => l.Code == "all");
    }

    private void LoadFilterOptions()
    {
        // Kategoriler
        Categories.Clear();
        Categories.Add(new CategoryOption { Code = "all", Name = "Tüm Kategoriler" });
        Categories.Add(new CategoryOption { Code = "translation", Name = "Çeviri" });
        Categories.Add(new CategoryOption { Code = "preprocessing", Name = "Ön Düzenleme" });
        Categories.Add(new CategoryOption { Code = "technical", Name = "Teknik" });

        // Diller
        Languages.Clear();
        Languages.Add(new LanguageOption { Code = "all", Name = "Tüm Diller" });
        Languages.Add(new LanguageOption { Code = "tr", Name = "Türkçe" });
        Languages.Add(new LanguageOption { Code = "en", Name = "İngilizce" });
        Languages.Add(new LanguageOption { Code = "ar", Name = "Arapça" });
        Languages.Add(new LanguageOption { Code = "es", Name = "İspanyolca" });
        Languages.Add(new LanguageOption { Code = "ur", Name = "Urduca" });

        // Varsayılan seçimler
        SelectedCategory = Categories.FirstOrDefault();
        SelectedLanguage = Languages.FirstOrDefault();
    }

    private void ApplyFilters()
    {
        if (Prompts == null)
        {
            FilteredPrompts = new ObservableCollection<Prompt>();
            return;
        }

        var filtered = Prompts.AsEnumerable();

        // Arama filtresi
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLower();
            filtered = filtered.Where(p => 
                p.Title.ToLower().Contains(searchLower) || 
                p.Content.ToLower().Contains(searchLower) ||
                p.Category.ToLower().Contains(searchLower));
        }

        // Kategori filtresi
        if (SelectedCategory != null && SelectedCategory.Code != "all")
        {
            filtered = filtered.Where(p => p.Category == SelectedCategory.Name);
        }

        // Dil filtresi
        if (SelectedLanguage != null && SelectedLanguage.Code != "all")
        {
            filtered = filtered.Where(p => p.Language == SelectedLanguage.Name);
        }

        // Sonuçları sırala (kullanım sayısına göre azalan, sonra güncelleme tarihine göre azalan)
        filtered = filtered.OrderByDescending(p => p.UsageCount)
                          .ThenByDescending(p => p.UpdatedAt);

        FilteredPrompts = new ObservableCollection<Prompt>(filtered);

        // Empty message güncelle
        UpdateEmptyMessage();
    }

    private void UpdateEmptyMessage()
    {
        if (HasActiveFilters)
        {
            EmptyMessage = "Arama kriterlerinize uygun prompt bulunamadı";
        }
        else
        {
            EmptyMessage = "Henüz prompt bulunamadı";
        }
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
                await Shell.Current.DisplayAlert("Hata", $"Popup açılırken hata: {ex.Message}", "Tamam");
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
                var result = await _promptStorageService.GetPromptsPagedAsync(1, 50); // Daha fazla prompt yükle
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Prompts.Clear();
                    foreach (var prompt in result.Items)
                    {
                        Prompts.Add(prompt);
                    }
                });

                // Dinamik kategori ve dil seçeneklerini yükle
                await LoadDynamicFilterOptions();
            }
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.DisplayAlert("Hata", $"Promptlar yüklenirken hata: {ex.Message}", "Tamam");
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

    private async Task LoadDynamicFilterOptions()
    {
        try
        {
            if (_promptStorageService != null)
            {
                // Veritabanından mevcut kategorileri al
                var dbCategories = await _promptStorageService.GetCategoriesAsync();
                var dbLanguages = await _promptStorageService.GetLanguagesAsync();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Mevcut seçimi koru
                    var currentCategorySelection = SelectedCategory;
                    var currentLanguageSelection = SelectedLanguage;

                    // Kategorileri güncelle
                    Categories.Clear();
                    Categories.Add(new CategoryOption { Code = "all", Name = "Tüm Kategoriler" });
                    foreach (var category in dbCategories)
                    {
                        if (!string.IsNullOrEmpty(category))
                        {
                            Categories.Add(new CategoryOption 
                            { 
                                Code = category.ToLower().Replace(" ", "_"), 
                                Name = category 
                            });
                        }
                    }

                    // Dilleri güncelle
                    Languages.Clear();
                    Languages.Add(new LanguageOption { Code = "all", Name = "Tüm Diller" });
                    foreach (var language in dbLanguages)
                    {
                        if (!string.IsNullOrEmpty(language))
                        {
                            Languages.Add(new LanguageOption 
                            { 
                                Code = language.ToLower().Replace(" ", "_"), 
                                Name = language 
                            });
                        }
                    }

                    // Seçimleri geri yükle
                    SelectedCategory = Categories.FirstOrDefault(c => c.Code == currentCategorySelection?.Code) 
                                     ?? Categories.FirstOrDefault();
                    SelectedLanguage = Languages.FirstOrDefault(l => l.Code == currentLanguageSelection?.Code) 
                                     ?? Languages.FirstOrDefault();
                });
            }
        }
        catch
        {
            // Hata durumunda varsayılan seçenekleri kullan
            LoadFilterOptions();
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
                await Shell.Current.DisplayAlert("Hata", $"Seçim işleminde hata: {ex.Message}", "Tamam");
            });
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        SearchText = e.NewTextValue ?? string.Empty;
    }

    private void OnCategorySelectionChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        if (e.AddedItems?.Count > 0 && e.AddedItems[0] is CategoryOption category)
        {
            SelectedCategory = category;
        }
    }

    private void OnLanguageSelectionChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        if (e.AddedItems?.Count > 0 && e.AddedItems[0] is LanguageOption language)
        {
            SelectedLanguage = language;
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
                await Shell.Current.DisplayAlert("Hata", $"Prompt seçilirken hata: {ex.Message}", "Tamam");
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
            
            // OnPopupClosed event handler'ı TaskCompletionSource'u handle edecek
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.DisplayAlert("Hata", $"Popup kapatılırken hata: {ex.Message}", "Tamam");
            });
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 