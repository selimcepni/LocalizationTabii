using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Services;
using LocalizationTabii.Models;
using LocalizationTabii.PageModels;
using System.Collections.ObjectModel;

namespace LocalizationTabii.PageModels
{
    public partial class PromptsManagementPageModel : ObservableObject
    {
        private readonly ModalErrorHandler _errorHandler;
        private readonly IPromptStorageService _promptStorageService;

        public event EventHandler? ShowAddPromptRequested;
        public event EventHandler<Prompt>? ShowEditPromptRequested;

        [ObservableProperty]
        private ObservableCollection<Prompt> prompts = new();

        [ObservableProperty]
        private int promptsCount;

        // Pagination properties
        [ObservableProperty]
        private int currentPage = 1;
        
        [ObservableProperty]
        private int pageSize = 5;
        
        [ObservableProperty]
        private int totalPages;
        
        [ObservableProperty]
        private int totalCount;
        
        [ObservableProperty]
        private bool hasNextPage;
        
        [ObservableProperty]
        private bool hasPreviousPage;
        
        [ObservableProperty]
        private string paginationInfo = string.Empty;

        partial void OnPromptsChanged(ObservableCollection<Prompt> value)
        {
            PromptsCount = value?.Count ?? 0;
            HasPrompts = PromptsCount > 0;
            ShowEmptyState = PromptsCount == 0 && CurrentPage == 1;
        }

        [ObservableProperty]
        private ObservableCollection<CategoryOption> categories = new();

        [ObservableProperty]
        private CategoryOption? selectedCategory;

        partial void OnSelectedCategoryChanged(CategoryOption? value)
        {
            CurrentPage = 1; // Reset to first page when filter changes
            if (value != null) // Null kontrolü ekle
            {
                Task.Run(async () => await LoadPromptsAsync());
            }
        }

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool hasPrompts;

        [ObservableProperty]
        private bool showEmptyState;

        public PromptsManagementPageModel(ModalErrorHandler errorHandler, IPromptStorageService promptStorageService)
        {
            _errorHandler = errorHandler;
            _promptStorageService = promptStorageService;
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                
                // Veritabanını başlat
                await _promptStorageService.InitializeDatabaseAsync();
                
                // Kategorileri yükle
                await LoadCategoriesAsync();
                
                // Kategoriler yüklendikten sonra promptları yükle
                if (Categories.Any())
                {
                    await LoadPromptsAsync();
                }
            }
            catch (Exception ex)
            {
                _errorHandler.ShowError("Hata", "Veriler yüklenirken hata oluştu: " + ex.Message);
                System.Diagnostics.Debug.WriteLine($"❌ InitializeAsync hatası: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadPromptsAsync()
        {
            try
            {
                IsLoading = true;
                
                // Pagination ile veri al
                var searchTerm = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
                var categoryFilter = (SelectedCategory?.Code == "all" || SelectedCategory == null) ? null : SelectedCategory.Name;
                
                var result = await _promptStorageService.GetPromptsPagedAsync(
                    CurrentPage, 
                    PageSize, 
                    searchTerm, 
                    categoryFilter);

                // UI'ı güncelle
                Prompts.Clear();
                foreach (var prompt in result.Items)
                {
                    Prompts.Add(prompt);
                }
                
                // Pagination bilgilerini güncelle
                TotalCount = result.TotalCount;
                TotalPages = result.TotalPages;
                HasNextPage = result.HasNextPage;
                HasPreviousPage = result.HasPreviousPage;
                
                // Pagination info text
                var startItem = TotalCount == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
                var endItem = Math.Min(CurrentPage * PageSize, TotalCount);
                PaginationInfo = $"{startItem}-{endItem} / {TotalCount} prompt";
                
                // Manuel olarak UI property'lerini güncelle
                PromptsCount = Prompts.Count;
                HasPrompts = PromptsCount > 0;
                ShowEmptyState = PromptsCount == 0 && CurrentPage == 1;
           
            }
            catch (Exception ex)
            {
                _errorHandler.ShowError("Hata", "Promptlar yüklenirken hata oluştu: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GoToNextPageAsync()
        {
            if (HasNextPage)
            {
                CurrentPage++;
                await LoadPromptsAsync();
            }
        }

        [RelayCommand]
        private async Task GoToPreviousPageAsync()
        {
            if (HasPreviousPage)
            {
                CurrentPage--;
                await LoadPromptsAsync();
            }
        }

        [RelayCommand]
        private async Task GoToFirstPageAsync()
        {
            if (CurrentPage != 1)
            {
                CurrentPage = 1;
                await LoadPromptsAsync();
            }
        }

        [RelayCommand]
        private async Task GoToLastPageAsync()
        {
            if (CurrentPage != TotalPages && TotalPages > 0)
            {
                CurrentPage = TotalPages;
                await LoadPromptsAsync();
            }
        }

        [RelayCommand]
        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categoryList = await _promptStorageService.GetCategoriesAsync();
                
                Categories.Clear();
                Categories.Add(new CategoryOption { Code = "all", Name = "Tümü" });
                
                // Veritabanından gelen kategorileri ekle
                foreach (var category in categoryList)
                {
                    if (!string.IsNullOrEmpty(category))
                    {
                        Categories.Add(new CategoryOption { Code = category.ToLower().Replace(" ", "_"), Name = category });
                    }
                }
                
                // Sadece 3 temel kategoriyi manuel ekle (popup'ta kullanılanlar)
                var requiredCategories = new[] { "Çeviri", "Ön Düzenleme", "Teknik" };
                foreach (var category in requiredCategories)
                {
                    if (!Categories.Any(c => c.Name == category))
                    {
                        Categories.Add(new CategoryOption { Code = category.ToLower().Replace(" ", "_"), Name = category });
                    }
                }
                
                // İlk kategoriyi seç (Tümü) - sadece null ise
                if (SelectedCategory == null && Categories.Any())
                {
                    SelectedCategory = Categories.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                _errorHandler.ShowError("Hata", "Kategoriler yüklenirken hata oluştu: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            await LoadPromptsAsync();
        }

        [RelayCommand]
        private async Task CategoryChangedAsync()
        {
            await LoadPromptsAsync();
        }

        [RelayCommand]
        private void ShowAddPrompt()
        {
            ShowAddPromptRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void ShowEditPrompt(Prompt prompt)
        {
            if (prompt == null) return;
            ShowEditPromptRequested?.Invoke(this, prompt);
        }

        [RelayCommand]
        private async Task ShowDeleteConfirmation(Prompt prompt)
        {
            if (prompt == null) return;
            
            var result = await Application.Current.MainPage.DisplayAlert(
                "🗑️ Prompt Sil", 
                $"'{prompt.Title}' prompt'unu silmek istediğinizden emin misiniz?\n\nBu işlem geri alınamaz.", 
                "Sil", 
                "İptal");
            
            if (result)
            {
                await DeletePromptAsync(prompt);
            }
        }

        public async Task AddPromptAsync(Prompt prompt)
        {
            try
            {
                var promptId = await _promptStorageService.AddPromptAsync(prompt);
                
                // İlk sayfaya git (yeni eklenen prompt ilk sayfada görünsün)
                CurrentPage = 1;
                
                // Promptları yeniden yükle
                await LoadPromptsAsync();
                
                // Kategorileri yeniden yükle (yeni kategori eklenmiş olabilir)
                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                _errorHandler.ShowError("Hata", "Prompt eklenirken hata oluştu: " + ex.Message);
            }
        }

        public async Task UpdatePromptAsync(Prompt prompt)
        {
            try
            {
                await _promptStorageService.UpdatePromptAsync(prompt);
                await LoadPromptsAsync();
                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                _errorHandler.ShowError("Hata", "Prompt güncellenirken hata oluştu: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task DeletePromptAsync(Prompt prompt)
        {
            try
            {
                if (prompt != null)
                {
                    await _promptStorageService.DeletePromptAsync(prompt.Id);
                    await LoadPromptsAsync();
                    
                    // Başarı mesajı
                    await Application.Current.MainPage.DisplayAlert("✅ Başarılı", 
                        $"'{prompt.Title}' prompt'u başarıyla silindi.", "Tamam");
                }
            }
            catch (Exception ex)
            {
                _errorHandler.ShowError("Hata", "Prompt silinirken hata oluştu: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task UsePromptAsync(Prompt prompt)
        {
            try
            {
                if (prompt != null)
                {
                    await _promptStorageService.IncrementUsageCountAsync(prompt.Id);
                    
                    // Prompt içeriğini panoya kopyala
                    await Clipboard.SetTextAsync(prompt.Content);
                    
                    // Kullanım sayısını güncelle
                    await LoadPromptsAsync();
                    
                    // Modern toast notification benzeri başarı mesajı
                    await Application.Current.MainPage.DisplayAlert("✅ Başarılı", 
                        "Prompt panoya kopyalandı ve kullanım sayısı güncellendi!", "Tamam");
                }
            }
            catch (Exception ex)
            {
                _errorHandler.ShowError("Hata", "Prompt kullanılırken hata oluştu: " + ex.Message);
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            // Reset to first page when search changes
            CurrentPage = 1;
            
            // Debounce search - 500ms delay
            Task.Run(async () =>
            {
                await Task.Delay(500);
                if (SearchText == value) // Eğer arama metni değişmemişse
                {
                    await LoadPromptsAsync();
                }
            });
        }
    }
}
