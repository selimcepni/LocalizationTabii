using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Data;
using LocalizationTabii.Models;
using LocalizationTabii.Services;
using System.Collections.ObjectModel;

namespace LocalizationTabii.PageModels
{
    public partial class AnalysisToolsPageModel : ObservableObject
    {
        private readonly AnalysisProjectRepository _analysisProjectRepository;
        private readonly ModalErrorHandler _errorHandler;

        [ObservableProperty]
        private ObservableCollection<AnalysisProject> projects = new();

        [ObservableProperty]
        private AnalysisProject? selectedProject;

        [ObservableProperty]
        private bool isProjectSelected;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<AnalysisDocument> selectedProjectDocuments = new();

        public bool HasDocuments => SelectedProjectDocuments.Count > 0;

        public AnalysisToolsPageModel(AnalysisProjectRepository analysisProjectRepository, ModalErrorHandler errorHandler)
        {
            _analysisProjectRepository = analysisProjectRepository;
            _errorHandler = errorHandler;
        }

        [RelayCommand]
        private async Task LoadProjectsAsync()
        {
            try
            {
                IsLoading = true;
                var projectList = await _analysisProjectRepository.ListAsync();
                Projects.Clear();
                
                foreach (var project in projectList)
                {
                    Projects.Add(project);
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.ShowError("Proje Yükleme Hatası", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CreateProjectAsync(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                _errorHandler?.ShowError("Hata", "Proje adı boş olamaz.");
                return;
            }

            try
            {
                var newProject = new AnalysisProject
                {
                    Name = projectName.Trim(),
                    Description = $"{projectName} analiz projesi",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                var projectId = await _analysisProjectRepository.SaveItemAsync(newProject);
                newProject.ID = projectId;
                
                Projects.Insert(0, newProject);
                SelectedProject = newProject;
                IsProjectSelected = true;
                await LoadSelectedProjectDocuments();
            }
            catch (Exception ex)
            {
                _errorHandler?.ShowError("Proje Oluşturma Hatası", ex.Message);
            }
        }

        [RelayCommand]
        private async Task SelectProjectAsync(AnalysisProject project)
        {
            try
            {
                SelectedProject = project;
                IsProjectSelected = true;
                await LoadSelectedProjectDocuments();
            }
            catch (Exception ex)
            {
                _errorHandler?.ShowError("Proje Seçim Hatası", ex.Message);
            }
        }

        [RelayCommand]
        private void BackToProjectList()
        {
            SelectedProject = null;
            IsProjectSelected = false;
            SelectedProjectDocuments.Clear();
        }

        [RelayCommand]
        private async Task DeleteProjectAsync(AnalysisProject project)
        {
            try
            {
                await _analysisProjectRepository.DeleteItemAsync(project);
                Projects.Remove(project);
                
                if (SelectedProject?.ID == project.ID)
                {
                    BackToProjectList();
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.ShowError("Proje Silme Hatası", ex.Message);
            }
        }

        [RelayCommand]
        private async Task AddDocumentAsync(FileResult fileResult)
        {
            if (SelectedProject == null)
            {
                _errorHandler?.ShowError("Hata", "Önce bir proje seçin.");
                return;
            }

            try
            {
                var content = await ReadFileContentAsync(fileResult);
                var documentType = GetDocumentType(fileResult.FileName);

                var document = new AnalysisDocument
                {
                    FileName = fileResult.FileName,
                    OriginalFilePath = fileResult.FullPath,
                    FileExtension = Path.GetExtension(fileResult.FileName),
                    FileSize = content.Length,
                    AddedAt = DateTime.Now,
                    Content = content,
                    AnalysisProjectID = SelectedProject.ID,
                    Type = documentType
                };

                await _analysisProjectRepository.SaveDocumentAsync(document);
                SelectedProjectDocuments.Insert(0, document);
                SelectedProject.Documents.Add(document);

                // Update project's UpdatedAt
                SelectedProject.UpdatedAt = DateTime.Now;
                await _analysisProjectRepository.SaveItemAsync(SelectedProject);
            }
            catch (Exception ex)
            {
                _errorHandler?.ShowError("Dosya Ekleme Hatası", ex.Message);
            }
        }

        [RelayCommand]
        private async Task RemoveDocumentAsync(AnalysisDocument document)
        {
            try
            {
                await _analysisProjectRepository.DeleteDocumentAsync(document);
                SelectedProjectDocuments.Remove(document);
                SelectedProject?.Documents.Remove(document);

                if (SelectedProject != null)
                {
                    SelectedProject.UpdatedAt = DateTime.Now;
                    await _analysisProjectRepository.SaveItemAsync(SelectedProject);
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.ShowError("Dosya Silme Hatası", ex.Message);
            }
        }

        private async Task LoadSelectedProjectDocuments()
        {
            if (SelectedProject == null) return;

            try
            {
                var documents = await _analysisProjectRepository.ListDocumentsAsync(SelectedProject.ID);
                SelectedProjectDocuments.Clear();
                
                foreach (var doc in documents)
                {
                    SelectedProjectDocuments.Add(doc);
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.ShowError("Döküman Yükleme Hatası", ex.Message);
            }
        }

        private async Task<string> ReadFileContentAsync(FileResult fileResult)
        {
            using var stream = await fileResult.OpenReadAsync();
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private DocumentType GetDocumentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".srt" => DocumentType.Srt,
                ".txt" => DocumentType.Text,
                ".md" => DocumentType.Text,
                ".rtf" => DocumentType.Text,
                _ => DocumentType.Other
            };
        }

        [RelayCommand]
        private async Task CreateNewProjectAsync()
        {
            try
            {
                string projectName = await Application.Current.MainPage.DisplayPromptAsync(
                    "Yeni Proje", 
                    "Proje adını girin:",
                    "Oluştur",
                    "İptal",
                    placeholder: "Proje adı...");

                if (!string.IsNullOrWhiteSpace(projectName))
                {
                    await CreateProjectAsync(projectName);
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.ShowError("Proje Oluşturma Hatası", ex.Message);
            }
        }

        [RelayCommand]
        private async Task SelectFilesAsync()
        {
            try
            {
                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "public.text", "public.subtitle", "public.data" } },
                        { DevicePlatform.Android, new[] { "text/*", "application/*" } },
                        { DevicePlatform.WinUI, new[] { ".txt", ".srt", ".md", ".rtf", ".csv" } },
                        { DevicePlatform.Tizen, new[] { "*/*" } },
                        { DevicePlatform.macOS, new[] { "txt", "srt", "md", "rtf", "csv" } },
                    });

                var options = new PickOptions()
                {
                    PickerTitle = "Analiz için dosya seçin",
                    FileTypes = customFileType,
                };

                var results = await FilePicker.Default.PickMultipleAsync(options);
                if (results != null)
                {
                    foreach (var result in results)
                    {
                        await AddDocumentAsync(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _errorHandler?.ShowError("Dosya Seçim Hatası", ex.Message);
            }
        }

        [RelayCommand]
        private async Task AnalyzeDocumentsAsync()
        {
            if (SelectedProject == null || SelectedProjectDocuments.Count == 0)
            {
                _errorHandler?.ShowError("Hata", "Analiz edilecek döküman bulunamadı.");
                return;
            }

            try
            {
                // TODO: Implement document analysis logic
                await Application.Current.MainPage.DisplayAlert(
                    "Analiz", 
                    $"Toplam {SelectedProjectDocuments.Count} döküman analiz edilecek.\n\nBu özellik yakında eklenecek.", 
                    "Tamam");
            }
            catch (Exception ex)
            {
                _errorHandler?.ShowError("Analiz Hatası", ex.Message);
            }
        }

        partial void OnSelectedProjectDocumentsChanged(ObservableCollection<AnalysisDocument> value)
        {
            OnPropertyChanged(nameof(HasDocuments));
        }

        partial void OnSearchTextChanged(string value)
        {
            // TODO: Implement search filtering if needed
        }
    }
} 