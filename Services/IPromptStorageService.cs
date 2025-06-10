using LocalizationTabii.Models;

namespace LocalizationTabii.Services
{
    public interface IPromptStorageService : IDisposable
    {
        Task<List<Prompt>> GetAllPromptsAsync();
        Task<Prompt?> GetPromptByIdAsync(int id);
        Task<List<Prompt>> GetPromptsByCategoryAsync(string category);
        
        // Pagination metotları
        Task<PaginatedResult<Prompt>> GetPromptsPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, string? category = null, string? language = null);
        Task<int> GetTotalPromptsCountAsync(string? searchTerm = null, string? category = null, string? language = null);
        
        Task<List<string>> GetCategoriesAsync();
        Task<List<string>> GetLanguagesAsync();
        Task<int> AddPromptAsync(Prompt prompt);
        Task<bool> UpdatePromptAsync(Prompt prompt);
        Task<bool> DeletePromptAsync(int id);
        Task<bool> IncrementUsageCountAsync(int id);
        Task<List<Prompt>> SearchPromptsAsync(string searchTerm);
        Task InitializeDatabaseAsync();
    }
} 