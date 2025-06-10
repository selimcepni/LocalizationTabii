using Microsoft.Data.Sqlite;
using LocalizationTabii.Models;
using System.Text;
using System.Collections.Concurrent;

namespace LocalizationTabii.Services
{
    public class PromptStorageService : IPromptStorageService
    {
        private readonly string _databasePath;
        private readonly string _connectionString;
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private bool _isInitialized = false;
        
        // Cache kategoriler için
        private readonly ConcurrentBag<string> _categoriesCache = new();
        private DateTime _categoriesCacheTime = DateTime.MinValue;
        
        // Cache diller için
        private readonly ConcurrentBag<string> _languagesCache = new();
        private DateTime _languagesCacheTime = DateTime.MinValue;
        
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public PromptStorageService()
        {
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "prompts.db");
            _connectionString = $"Data Source={_databasePath};Cache=Shared;Pooling=true;";
        }

        public async Task InitializeDatabaseAsync()
        {
            if (_isInitialized) return;

            await _initializationSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_isInitialized) return;

                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);

                var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Prompts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Content TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        Category TEXT NOT NULL DEFAULT 'Çeviri',
                        Language TEXT NOT NULL DEFAULT 'Türkçe',
                        UsageCount INTEGER NOT NULL DEFAULT 0
                    );
                    
                    CREATE INDEX IF NOT EXISTS idx_prompts_category ON Prompts(Category);
                    CREATE INDEX IF NOT EXISTS idx_prompts_language ON Prompts(Language);
                    CREATE INDEX IF NOT EXISTS idx_prompts_isactive ON Prompts(IsActive);
                    CREATE INDEX IF NOT EXISTS idx_prompts_updated ON Prompts(UpdatedAt);";

                await createTableCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

                // Language sütununu mevcut tabloya ekle (eğer yoksa)
                var alterTableCommand = connection.CreateCommand();
                alterTableCommand.CommandText = @"
                    ALTER TABLE Prompts ADD COLUMN Language TEXT NOT NULL DEFAULT 'Türkçe'";
                
                try
                {
                    await alterTableCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                catch (SqliteException)
                {
                    // Sütun zaten varsa hata yok sayılır
                }

                // UsageCount sütununu mevcut tabloya ekle (eğer yoksa)
                var alterUsageCountCommand = connection.CreateCommand();
                alterUsageCountCommand.CommandText = @"
                    ALTER TABLE Prompts ADD COLUMN UsageCount INTEGER NOT NULL DEFAULT 0";
                
                try
                {
                    await alterUsageCountCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                catch (SqliteException)
                {
                    // Sütun zaten varsa hata yok sayılır
                }

                // Check if we need to add sample data
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT COUNT(*) FROM Prompts WHERE IsActive = 1";
                var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync().ConfigureAwait(false));

                if (count == 0)
                {
                    await AddSamplePromptsAsync(connection).ConfigureAwait(false);
                }

                _isInitialized = true;
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        private async Task AddSamplePromptsAsync(SqliteConnection connection)
        {
            var samplePrompts = new[]
            {
                new { Title = "Profesyonel Çeviri Talebi", Content = "Lütfen aşağıdaki metni {hedef_dil} diline profesyonel bir şekilde çevirin. Çeviride kültürel bağlamı ve teknik terimleri dikkate alın. Orijinal metnin tonunu ve stilini koruyarak, hedef dilde doğal ve akıcı bir çeviri yapın. Gerekirse açıklayıcı notlar ekleyin.\n\nÇevrilecek metin:\n{metin}\n\nHedef dil: {hedef_dil}\nKaynak dil: {kaynak_dil}\nMetin türü: {metin_turu}\nHedef kitle: {hedef_kitle}", Category = "Çeviri", Language = "Türkçe" },
                new { Title = "Technical Documentation Generator", Content = "Create comprehensive technical documentation for {technology} focusing on {topic}. Include the following sections: overview, prerequisites, step-by-step implementation, code examples, best practices, troubleshooting, and references. Ensure the documentation is clear, well-structured, and suitable for {target_audience}. Use proper formatting with headers, bullet points, and code blocks where appropriate.\n\nTechnology: {technology}\nTopic: {topic}\nTarget Audience: {target_audience}\nComplexity Level: {complexity_level}", Category = "Teknik", Language = "İngilizce" },
                new { Title = "Metin Ön Düzenleme ve İyileştirme", Content = "Aşağıdaki metni kapsamlı bir şekilde düzenleyin ve iyileştirin. Dil bilgisi hatalarını düzeltin, yazım kurallarına uygunluğunu sağlayın, cümle yapılarını geliştirin ve metnin akıcılığını artırın. Ayrıca ton ve stil tutarlılığını kontrol edin, gereksiz tekrarları kaldırın ve metnin genel kalitesini yükseltin.\n\nDüzenlenecek metin:\n{metin}\n\nHedef ton: {ton}\nMetin türü: {metin_turu}\nHedef kitle: {hedef_kitle}\nUzunluk tercihi: {uzunluk}", Category = "Ön Düzenleme", Language = "Türkçe" },
                new { Title = "Generador de Documentación Técnica", Content = "Crear documentación técnica completa para {tecnología} enfocándose en {tema}. Incluir las siguientes secciones: descripción general, prerrequisitos, implementación paso a paso, ejemplos de código, mejores prácticas, solución de problemas y referencias. Asegurar que la documentación sea clara, bien estructurada y adecuada para {público_objetivo}. Usar formato apropiado con encabezados, viñetas y bloques de código cuando sea apropiado.\n\nTecnología: {tecnología}\nTema: {tema}\nPúblico Objetivo: {público_objetivo}\nNivel de Complejidad: {nivel_complejidad}", Category = "Teknik", Language = "İspanyolca" },
                new { Title = "پیشہ ورانہ ترجمہ جنریٹر", Content = "براہ کرم درج ذیل متن کو {ہدف_زبان} میں پیشہ ورانہ انداز میں ترجمہ کریں۔ ثقافتی سیاق اور تکنیکی اصطلاحات کا خیال رکھیں۔ اصل متن کے لہجے اور انداز کو برقرار رکھتے ہوئے ہدف زبان میں فطری اور روانی سے ترجمہ کریں۔ ضرورت کے مطابق وضاحتی نوٹس شامل کریں۔\n\nترجمہ کے لیے متن:\n{متن}\n\nہدف زبان: {ہدف_زبان}\nماخذ زبان: {ماخذ_زبان}\nمتن کی قسم: {متن_کی_قسم}\nہدف سامعین: {ہدف_سامعین}", Category = "Çeviri", Language = "Urduca" }
            };

            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var prompt in samplePrompts)
                {
                    var insertCommand = connection.CreateCommand();
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText = @"
                        INSERT INTO Prompts (Title, Content, Category, Language, CreatedAt, UpdatedAt)
                        VALUES (@title, @content, @category, @language, @createdAt, @updatedAt)";
                    
                    insertCommand.Parameters.AddWithValue("@title", prompt.Title);
                    insertCommand.Parameters.AddWithValue("@content", prompt.Content);
                    insertCommand.Parameters.AddWithValue("@category", prompt.Category);
                    insertCommand.Parameters.AddWithValue("@language", prompt.Language);
                    insertCommand.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    insertCommand.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    await insertCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                
                await transaction.CommitAsync().ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task<List<Prompt>> GetAllPromptsAsync()
        {
            var prompts = new List<Prompt>();
            
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Prompts WHERE IsActive = 1 ORDER BY UpdatedAt DESC";

                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    prompts.Add(MapReaderToPrompt(reader));
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return prompts;
        }

        public async Task<Prompt?> GetPromptByIdAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Prompts WHERE Id = @id AND IsActive = 1";
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            if (await reader.ReadAsync().ConfigureAwait(false))
            {
                return MapReaderToPrompt(reader);
            }

            return null;
        }

        public async Task<List<Prompt>> GetPromptsByCategoryAsync(string category)
        {
            var prompts = new List<Prompt>();
            
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Prompts WHERE Category = @category AND IsActive = 1 ORDER BY UpdatedAt DESC";
            command.Parameters.AddWithValue("@category", category);

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                prompts.Add(MapReaderToPrompt(reader));
            }

            return prompts;
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            // Cache kontrolü
            if (DateTime.Now - _categoriesCacheTime < _cacheExpiry && _categoriesCache.Any())
            {
                return _categoriesCache.ToList();
            }
            
            var categories = new List<string>();
            
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT DISTINCT Category FROM Prompts WHERE IsActive = 1 ORDER BY Category";

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                categories.Add(reader["Category"].ToString() ?? string.Empty);
            }

            // Cache güncelle
            _categoriesCache.Clear();
            foreach (var category in categories)
            {
                _categoriesCache.Add(category);
            }
            _categoriesCacheTime = DateTime.Now;

            return categories;
        }

        public async Task<List<string>> GetLanguagesAsync()
        {
            // Cache kontrolü
            if (DateTime.Now - _languagesCacheTime < _cacheExpiry && _languagesCache.Any())
            {
                return _languagesCache.ToList();
            }
            
            var languages = new List<string>();
            
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT DISTINCT Language FROM Prompts WHERE IsActive = 1 ORDER BY Language";

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                languages.Add(reader["Language"].ToString() ?? string.Empty);
            }

            // Cache güncelle
            _languagesCache.Clear();
            foreach (var language in languages)
            {
                _languagesCache.Add(language);
            }
            _languagesCacheTime = DateTime.Now;

            return languages;
        }

        public async Task<int> AddPromptAsync(Prompt prompt)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Prompts (Title, Content, Category, Language, CreatedAt, UpdatedAt, IsActive, UsageCount)
                VALUES (@title, @content, @category, @language, @createdAt, @updatedAt, @isActive, @usageCount);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@title", prompt.Title);
            command.Parameters.AddWithValue("@content", prompt.Content);
            command.Parameters.AddWithValue("@category", prompt.Category);
            command.Parameters.AddWithValue("@language", prompt.Language);
            command.Parameters.AddWithValue("@createdAt", prompt.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@isActive", prompt.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("@usageCount", prompt.UsageCount);

            var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
            
            var promptId = result != null ? Convert.ToInt32(result) : 0;
            
            // Cache invalidate
            _categoriesCacheTime = DateTime.MinValue;
            _languagesCacheTime = DateTime.MinValue;
            
            return promptId;
        }

        public async Task<bool> UpdatePromptAsync(Prompt prompt)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Prompts 
                SET Title = @title, Content = @content, Category = @category, Language = @language, UpdatedAt = @updatedAt, IsActive = @isActive, UsageCount = @usageCount
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", prompt.Id);
            command.Parameters.AddWithValue("@title", prompt.Title);
            command.Parameters.AddWithValue("@content", prompt.Content);
            command.Parameters.AddWithValue("@category", prompt.Category);
            command.Parameters.AddWithValue("@language", prompt.Language);
            command.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@isActive", prompt.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("@usageCount", prompt.UsageCount);

            var rowsAffected = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            
            // Cache invalidate
            _categoriesCacheTime = DateTime.MinValue;
            _languagesCacheTime = DateTime.MinValue;
            
            return rowsAffected > 0;
        }

        public async Task<bool> DeletePromptAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Prompts SET IsActive = 0 WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            
            // Cache invalidate
            _categoriesCacheTime = DateTime.MinValue;
            _languagesCacheTime = DateTime.MinValue;
            
            return rowsAffected > 0;
        }

        public async Task<bool> IncrementUsageCountAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Prompts SET UsageCount = UsageCount + 1, UpdatedAt = @updatedAt WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            var rowsAffected = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            return rowsAffected > 0;
        }

        public async Task<List<Prompt>> SearchPromptsAsync(string searchTerm)
        {
            var prompts = new List<Prompt>();
            
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM Prompts 
                WHERE IsActive = 1 AND (
                    Title LIKE @searchTerm OR 
                    Content LIKE @searchTerm OR 
                    Category LIKE @searchTerm
                )
                ORDER BY 
                    CASE 
                        WHEN Title LIKE @exactMatch THEN 1
                        WHEN Category LIKE @exactMatch THEN 2
                        ELSE 3
                    END,
                    UsageCount DESC, 
                    UpdatedAt DESC";
            
            command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
            command.Parameters.AddWithValue("@exactMatch", $"%{searchTerm}%");

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                prompts.Add(MapReaderToPrompt(reader));
            }

            return prompts;
        }

        public async Task<PaginatedResult<Prompt>> GetPromptsPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, string? category = null, string? language = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            var whereConditions = new List<string> { "IsActive = 1" };
            var parameters = new List<(string name, object value)>();

            // Arama filtresi
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereConditions.Add("(Title LIKE @searchTerm OR Content LIKE @searchTerm OR Category LIKE @searchTerm)");
                parameters.Add(("@searchTerm", $"%{searchTerm}%"));
            }

            // Kategori filtresi
            if (!string.IsNullOrWhiteSpace(category) && category != "all")
            {
                whereConditions.Add("Category = @category");
                parameters.Add(("@category", category));
            }

            // Dil filtresi
            if (!string.IsNullOrWhiteSpace(language) && language != "all")
            {
                whereConditions.Add("Language = @language");
                parameters.Add(("@language", language));
            }

            var whereClause = string.Join(" AND ", whereConditions);
            var offset = (pageNumber - 1) * pageSize;

            // Toplam sayım
            using var countCommand = connection.CreateCommand();
            countCommand.CommandText = $"SELECT COUNT(*) FROM Prompts WHERE {whereClause}";
            foreach (var param in parameters)
            {
                countCommand.Parameters.AddWithValue(param.name, param.value);
            }
            var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync().ConfigureAwait(false));

            // Sayfalı veri
            var prompts = new List<Prompt>();
            using var command = connection.CreateCommand();
            
            // Arama terimi varsa farklı, yoksa basit sıralama yapalım
            string orderClause;
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                orderClause = @"
                    ORDER BY 
                        CASE 
                            WHEN Title LIKE @exactTerm THEN 1
                            WHEN Title LIKE @startTerm THEN 2
                            WHEN Content LIKE @exactTerm THEN 3
                            WHEN Content LIKE @startTerm THEN 4
                            ELSE 5
                        END,
                        UsageCount DESC,
                        UpdatedAt DESC";
            }
            else
            {
                orderClause = "ORDER BY UsageCount DESC, UpdatedAt DESC";
            }
            
            command.CommandText = $@"
                SELECT * FROM Prompts 
                WHERE {whereClause}
                {orderClause}
                LIMIT @pageSize OFFSET @offset";

            // Mevcut parametreleri ekle
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.name, param.value);
            }
            
            // Arama terimi varsa ek parametreler ekle
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // Eğer zaten @searchTerm parametresi varsa farklı isimler kullan
                if (parameters.Any(p => p.name == "@searchTerm"))
                {
                    command.Parameters.AddWithValue("@exactTerm", $"%{searchTerm}%");
                    command.Parameters.AddWithValue("@startTerm", $"{searchTerm}%");
                }
                else
                {
                    command.Parameters.AddWithValue("@exactTerm", $"%{searchTerm}%");
                    command.Parameters.AddWithValue("@startTerm", $"{searchTerm}%");
                }
            }
            
            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@offset", offset);

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                prompts.Add(MapReaderToPrompt(reader));
            }

            return new PaginatedResult<Prompt>
            {
                Items = prompts,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<int> GetTotalPromptsCountAsync(string? searchTerm = null, string? category = null, string? language = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            var whereConditions = new List<string> { "IsActive = 1" };
            var parameters = new List<(string name, object value)>();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereConditions.Add("(Title LIKE @searchTerm OR Content LIKE @searchTerm OR Category LIKE @searchTerm)");
                parameters.Add(("@searchTerm", $"%{searchTerm}%"));
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "all")
            {
                whereConditions.Add("Category = @category");
                parameters.Add(("@category", category));
            }

            if (!string.IsNullOrWhiteSpace(language) && language != "all")
            {
                whereConditions.Add("Language = @language");
                parameters.Add(("@language", language));
            }

            var whereClause = string.Join(" AND ", whereConditions);

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM Prompts WHERE {whereClause}";
            
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.name, param.value);
            }

            return Convert.ToInt32(await command.ExecuteScalarAsync().ConfigureAwait(false));
        }

        private static Prompt MapReaderToPrompt(SqliteDataReader reader)
        {
            return new Prompt
            {
                Id = Convert.ToInt32(reader["Id"]),
                Title = reader["Title"].ToString() ?? string.Empty,
                Content = reader["Content"].ToString() ?? string.Empty,
                Category = reader["Category"].ToString() ?? string.Empty,
                Language = reader["Language"]?.ToString() ?? "Türkçe",
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString() ?? DateTime.Now.ToString()),
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString() ?? DateTime.Now.ToString()),
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1,
                UsageCount = Convert.ToInt32(reader["UsageCount"])
            };
        }
        
        public void Dispose()
        {
            _initializationSemaphore?.Dispose();
        }
    }
} 