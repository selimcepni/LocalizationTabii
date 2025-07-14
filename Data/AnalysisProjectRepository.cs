using LocalizationTabii.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace LocalizationTabii.Data
{
    /// <summary>
    /// Repository class for managing analysis projects in the database.
    /// </summary>
    public class AnalysisProjectRepository
    {
        private bool _hasBeenInitialized = false;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalysisProjectRepository"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public AnalysisProjectRepository(ILogger<AnalysisProjectRepository> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initializes the database connection and creates the AnalysisProject and AnalysisDocument tables if they do not exist.
        /// </summary>
        private async Task Init()
        {
            if (_hasBeenInitialized)
                return;

            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            try
            {
                // Create AnalysisProject table
                var createTableCmd = connection.CreateCommand();
                createTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS AnalysisProject (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL
                    );";
                await createTableCmd.ExecuteNonQueryAsync();

                // Create AnalysisDocument table
                createTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS AnalysisDocument (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        FileName TEXT NOT NULL,
                        OriginalFilePath TEXT NOT NULL,
                        FileExtension TEXT NOT NULL,
                        FileSize INTEGER NOT NULL,
                        AddedAt TEXT NOT NULL,
                        Content TEXT NOT NULL,
                        AnalysisProjectID INTEGER NOT NULL,
                        Type INTEGER NOT NULL,
                        FOREIGN KEY (AnalysisProjectID) REFERENCES AnalysisProject (ID) ON DELETE CASCADE
                    );";
                await createTableCmd.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating AnalysisProject tables");
                throw;
            }

            _hasBeenInitialized = true;
        }

        /// <summary>
        /// Retrieves a list of all analysis projects from the database.
        /// </summary>
        /// <returns>A list of <see cref="AnalysisProject"/> objects.</returns>
        public async Task<List<AnalysisProject>> ListAsync()
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM AnalysisProject ORDER BY UpdatedAt DESC";
            var projects = new List<AnalysisProject>();

            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                projects.Add(new AnalysisProject
                {
                    ID = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3)),
                    UpdatedAt = DateTime.Parse(reader.GetString(4))
                });
            }

            // Load documents for each project
            foreach (var project in projects)
            {
                project.Documents = await ListDocumentsAsync(project.ID);
            }

            return projects;
        }

        /// <summary>
        /// Retrieves a specific analysis project by its ID.
        /// </summary>
        /// <param name="id">The ID of the project.</param>
        /// <returns>A <see cref="AnalysisProject"/> object if found; otherwise, null.</returns>
        public async Task<AnalysisProject?> GetAsync(int id)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM AnalysisProject WHERE ID = @id";
            selectCmd.Parameters.AddWithValue("@id", id);

            await using var reader = await selectCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var project = new AnalysisProject
                {
                    ID = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3)),
                    UpdatedAt = DateTime.Parse(reader.GetString(4))
                };

                project.Documents = await ListDocumentsAsync(project.ID);
                return project;
            }

            return null;
        }

        /// <summary>
        /// Saves an analysis project to the database.
        /// </summary>
        /// <param name="item">The project to save.</param>
        /// <returns>The ID of the saved project.</returns>
        public async Task<int> SaveItemAsync(AnalysisProject item)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var saveCmd = connection.CreateCommand();
            if (item.ID == 0)
            {
                saveCmd.CommandText = @"
                    INSERT INTO AnalysisProject (Name, Description, CreatedAt, UpdatedAt)
                    VALUES (@Name, @Description, @CreatedAt, @UpdatedAt);
                    SELECT last_insert_rowid();";
                saveCmd.Parameters.AddWithValue("@CreatedAt", item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                saveCmd.CommandText = @"
                    UPDATE AnalysisProject
                    SET Name = @Name, Description = @Description, UpdatedAt = @UpdatedAt
                    WHERE ID = @ID";
                saveCmd.Parameters.AddWithValue("@ID", item.ID);
            }

            item.UpdatedAt = DateTime.Now;
            saveCmd.Parameters.AddWithValue("@Name", item.Name);
            saveCmd.Parameters.AddWithValue("@Description", item.Description);
            saveCmd.Parameters.AddWithValue("@UpdatedAt", item.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            var result = await saveCmd.ExecuteScalarAsync();
            if (item.ID == 0)
            {
                item.ID = Convert.ToInt32(result);
            }

            return item.ID;
        }

        /// <summary>
        /// Deletes an analysis project from the database.
        /// </summary>
        /// <param name="item">The project to delete.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> DeleteItemAsync(AnalysisProject item)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM AnalysisProject WHERE ID = @ID";
            deleteCmd.Parameters.AddWithValue("@ID", item.ID);

            return await deleteCmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Retrieves documents for a specific analysis project.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <returns>A list of <see cref="AnalysisDocument"/> objects.</returns>
        public async Task<List<AnalysisDocument>> ListDocumentsAsync(int projectId)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM AnalysisDocument WHERE AnalysisProjectID = @projectId ORDER BY AddedAt DESC";
            selectCmd.Parameters.AddWithValue("@projectId", projectId);
            var documents = new List<AnalysisDocument>();

            await using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                documents.Add(new AnalysisDocument
                {
                    ID = reader.GetInt32(0),
                    FileName = reader.GetString(1),
                    OriginalFilePath = reader.GetString(2),
                    FileExtension = reader.GetString(3),
                    FileSize = reader.GetInt64(4),
                    AddedAt = DateTime.Parse(reader.GetString(5)),
                    Content = reader.GetString(6),
                    AnalysisProjectID = reader.GetInt32(7),
                    Type = (DocumentType)reader.GetInt32(8)
                });
            }

            return documents;
        }

        /// <summary>
        /// Saves a document to the database.
        /// </summary>
        /// <param name="document">The document to save.</param>
        /// <returns>The ID of the saved document.</returns>
        public async Task<int> SaveDocumentAsync(AnalysisDocument document)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var saveCmd = connection.CreateCommand();
            if (document.ID == 0)
            {
                saveCmd.CommandText = @"
                    INSERT INTO AnalysisDocument (FileName, OriginalFilePath, FileExtension, FileSize, AddedAt, Content, AnalysisProjectID, Type)
                    VALUES (@FileName, @OriginalFilePath, @FileExtension, @FileSize, @AddedAt, @Content, @AnalysisProjectID, @Type);
                    SELECT last_insert_rowid();";
            }
            else
            {
                saveCmd.CommandText = @"
                    UPDATE AnalysisDocument
                    SET FileName = @FileName, OriginalFilePath = @OriginalFilePath, FileExtension = @FileExtension, 
                        FileSize = @FileSize, Content = @Content, Type = @Type
                    WHERE ID = @ID";
                saveCmd.Parameters.AddWithValue("@ID", document.ID);
            }

            saveCmd.Parameters.AddWithValue("@FileName", document.FileName);
            saveCmd.Parameters.AddWithValue("@OriginalFilePath", document.OriginalFilePath);
            saveCmd.Parameters.AddWithValue("@FileExtension", document.FileExtension);
            saveCmd.Parameters.AddWithValue("@FileSize", document.FileSize);
            saveCmd.Parameters.AddWithValue("@AddedAt", document.AddedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            saveCmd.Parameters.AddWithValue("@Content", document.Content);
            saveCmd.Parameters.AddWithValue("@AnalysisProjectID", document.AnalysisProjectID);
            saveCmd.Parameters.AddWithValue("@Type", (int)document.Type);

            var result = await saveCmd.ExecuteScalarAsync();
            if (document.ID == 0)
            {
                document.ID = Convert.ToInt32(result);
            }

            return document.ID;
        }

        /// <summary>
        /// Deletes a document from the database.
        /// </summary>
        /// <param name="document">The document to delete.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> DeleteDocumentAsync(AnalysisDocument document)
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM AnalysisDocument WHERE ID = @ID";
            deleteCmd.Parameters.AddWithValue("@ID", document.ID);

            return await deleteCmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Drops the AnalysisProject and AnalysisDocument tables from the database.
        /// </summary>
        public async Task DropTableAsync()
        {
            await Init();
            await using var connection = new SqliteConnection(Constants.DatabasePath);
            await connection.OpenAsync();

            var dropCmd = connection.CreateCommand();
            dropCmd.CommandText = "DROP TABLE IF EXISTS AnalysisDocument";
            await dropCmd.ExecuteNonQueryAsync();
            
            dropCmd.CommandText = "DROP TABLE IF EXISTS AnalysisProject";
            await dropCmd.ExecuteNonQueryAsync();

            _hasBeenInitialized = false;
        }
    }
} 