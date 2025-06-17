using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

#if MACCATALYST || IOS
using Foundation;
using UIKit;
#endif

namespace LocalizationTabii.ComponentModel
{
    public partial class FileDragDropViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isSuccess;
        
        [ObservableProperty]
        private bool hasSelectedFile;

        [ObservableProperty]
        private string uploadedFileName = string.Empty;

        [ObservableProperty]
        private string fileSize = string.Empty;

        public event EventHandler<FileSelectedEventArgs>? FileSelected;
        public event EventHandler<ContinueEventArgs>? ContinueRequested;

        public FileDragDropViewModel()
        {
            // Drop handling artık code-behind'da yapılıyor
        }

        private bool CanSelectFile()
        {
            return !IsLoading;
        }

        [RelayCommand(CanExecute = nameof(CanSelectFile))]
        private async Task SelectFile()
        {
            // Loading state'ini hemen set et
            IsLoading = true;
            IsSuccess = false;
            HasSelectedFile = false;
            
            try
            {
                // Sadece SRT ve VTT dosyalarına izin ver
                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "public.subtitle", "org.matroska.srt", "org.videolan.vlc.srt" } },
                        { DevicePlatform.macOS, new[] { "srt", "vtt" } },
                        { DevicePlatform.MacCatalyst, new[] { "srt", "vtt" } },
                        { DevicePlatform.Android, new[] { "application/x-subrip", "text/vtt" } },
                        { DevicePlatform.WinUI, new[] { ".srt", ".vtt" } },
                    });

                var options = new PickOptions
                {
                    PickerTitle = "SRT veya VTT Dosyası Seçin",
                    FileTypes = customFileType,
                };

                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    await ProcessSelectedFileAsync(result);
                }
                else
                {
                    // Dosya seçimi iptal edildi, state'i main thread'de sıfırla
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IsLoading = false;
                        IsSuccess = false;
                        HasSelectedFile = false;
                        SelectFileCommand.NotifyCanExecuteChanged();
                    });
                }
            }
            catch (Exception ex)
            {
                // Main thread'de state güncelle
                MainThread.BeginInvokeOnMainThread(() =>
            {
                IsLoading = false;
                IsSuccess = false;
                    HasSelectedFile = false;
                    SelectFileCommand.NotifyCanExecuteChanged();
                });
                
                await Application.Current?.MainPage?.DisplayAlert("Hata", 
                    $"Dosya seçimi sırasında hata oluştu: {ex.Message}", "Tamam");
            }
        }

        private async Task HandleFileDropAsync(DragEventArgs dragArgs)
        {
            // İlk olarak alert göster - drop event çağrıldı mı?
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Debug", 
                    "🔧 DROP EVENT ÇAĞRILDI!", "Tamam");
            }
            
            try
            {
                // Debug: Drop event çağrıldı
                System.Diagnostics.Debug.WriteLine($"🔧 DROP EVENT ÇAĞRILDI!");
                System.Diagnostics.Debug.WriteLine($"🔧 dragArgs null?: {dragArgs == null}");
                System.Diagnostics.Debug.WriteLine($"🔧 dragArgs.Data null?: {dragArgs?.Data == null}");
                System.Diagnostics.Debug.WriteLine($"🔧 dragArgs.PlatformArgs null?: {dragArgs?.PlatformArgs == null}");

#if MACCATALYST || IOS
                // macOS/iOS implementation
                System.Diagnostics.Debug.WriteLine($"🔧 macOS/iOS platform detected");
                
                var session = dragArgs?.PlatformArgs?.DropSession;
                System.Diagnostics.Debug.WriteLine($"🔧 DropSession null?: {session == null}");
                
                if (session != null)
                {
                    var items = session.Items;
                    System.Diagnostics.Debug.WriteLine($"🔧 Items count: {items?.Length ?? 0}");
                    
                    // Alert ile items count göster
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Debug", 
                            $"🔧 Items count: {items?.Length ?? 0}", "Tamam");
                    }
                    
                    foreach (var item in items)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔧 Processing item: {item}");
                        
                        // Try to load file representation
                        var types = item.ItemProvider.RegisteredTypeIdentifiers;
                        System.Diagnostics.Debug.WriteLine($"🔧 Registered types: {string.Join(", ", types)}");
                        
                        // Alert ile types göster
                        if (Application.Current?.MainPage != null)
                        {
                            await Application.Current.MainPage.DisplayAlert("Debug", 
                                $"🔧 Registered types: {string.Join(", ", types)}", "Tamam");
                        }
                        
                        foreach (var type in types)
                        {
                            System.Diagnostics.Debug.WriteLine($"🔧 Trying type: {type}");
                            
                            if (item.ItemProvider.HasItemConformingTo(type))
                            {
                                try
                                {
                                    System.Diagnostics.Debug.WriteLine($"🔧 Loading file representation for type: {type}");
                                    var result = await item.ItemProvider.LoadInPlaceFileRepresentationAsync(type);
                                    System.Diagnostics.Debug.WriteLine($"🔧 Result: {result}");
                                    System.Diagnostics.Debug.WriteLine($"🔧 FileUrl: {result?.FileUrl}");
                                    System.Diagnostics.Debug.WriteLine($"🔧 Path: {result?.FileUrl?.Path}");
                                    
                                    if (result?.FileUrl?.Path != null)
                                    {
                                        // Process file directly with path
                                        var fileName = Path.GetFileName(result.FileUrl.Path);
                                        System.Diagnostics.Debug.WriteLine($"🔧 Processing file: {fileName}");
                                        
                                        // Alert ile dosya adını göster
                                        if (Application.Current?.MainPage != null)
                                        {
                                            await Application.Current.MainPage.DisplayAlert("Debug", 
                                                $"🔧 Dosya bulundu: {fileName}\nPath: {result.FileUrl.Path}", "Tamam");
                                        }
                                        
                                        await ProcessSelectedFileFromPath(fileName, result.FileUrl.Path);
                                        return;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"🔧 Exception loading type {type}: {ex.Message}");
                                    
                                    // Alert ile exception göster
                                    if (Application.Current?.MainPage != null)
                                    {
                                        await Application.Current.MainPage.DisplayAlert("Debug", 
                                            $"🔧 Exception: {ex.Message}", "Tamam");
                                    }
                                    
                                    // Continue to next type
                                    continue;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Alert ile DropSession null olduğunu göster
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Debug", 
                            "🔧 DropSession NULL!", "Tamam");
                    }
                }
#else
                // Windows implementation
                if (dragArgs?.Data != null)
                {
                    // Try to get files from the data
                    var files = new List<FileResult>();
                    
                    if (dragArgs.Data?.Properties != null)
                    {
                        foreach (var property in dragArgs.Data.Properties)
                        {
                            if (property.Key.Contains("Files") || property.Key.Contains("StorageItems"))
                            {
                                if (property.Value is IEnumerable<object> enumerable)
                                {
                                    files.AddRange(enumerable.OfType<FileResult>());
                                }
                            }
                        }
                    }
                    
                    if (files.Any())
                    {
                        await ProcessSelectedFileAsync(files.First());
                        return;
                    }
                }
#endif
                
                // If we get here, no files were found
                System.Diagnostics.Debug.WriteLine($"🔧 No files found - showing alert");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = false;
                    IsSuccess = false;
                    HasSelectedFile = false;
                    SelectFileCommand.NotifyCanExecuteChanged();
                });
                
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Debug",
                        "🔧 Hiç dosya bulunamadı!", "Tamam");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🔧 Exception in HandleFileDropAsync: {ex}");
                
                // Alert ile main exception göster
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Debug", 
                        $"🔧 Ana Exception: {ex.Message}", "Tamam");
                }
                
                // State'i main thread'de sıfırla
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = false;
                    IsSuccess = false;
                    HasSelectedFile = false;
                    SelectFileCommand.NotifyCanExecuteChanged();
                });
                
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Hata",
                        $"Dosya yükleme hatası: {ex.Message}", "Tamam");
                }
            }
        }

        private void HandleDragOver(DragEventArgs? args)
        {
            if (args != null)
            {
                args.AcceptedOperation = DataPackageOperation.Copy;
            }
        }

        private async Task ProcessSelectedFileAsync(FileResult fileResult)
        {
            if (!IsValidFileType(fileResult.FileName))
            {
                // Sadece geçersiz dosya tipi için alert göster
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Geçersiz Dosya Tipi", 
                        "Sadece SRT ve VTT dosyaları desteklenmektedir.\n\nLütfen .srt veya .vtt uzantılı bir dosya seçin.", "Tamam");
                }
                // State'i main thread'de sıfırla
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = false;
                    IsSuccess = false;
                    HasSelectedFile = false;
                    SelectFileCommand.NotifyCanExecuteChanged();
                });
                return;
            }

            // IsLoading zaten SelectFile'da set edildi
            try
            {
                // Dosya boyutu kontrolü (10MB limit)
                using var stream = await fileResult.OpenReadAsync();
                if (stream.Length > 10 * 1024 * 1024) // 10MB
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Dosya Çok Büyük",
                            "Dosya boyutu 10MB'dan küçük olmalıdır.", "Tamam");
                    }
                    // State'i main thread'de sıfırla
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IsLoading = false;
                        IsSuccess = false;
                        HasSelectedFile = false;
                        SelectFileCommand.NotifyCanExecuteChanged();
                    });
                    return;
                }

                UploadedFileName = fileResult.FileName;
                FileSize = FormatFileSize(stream.Length);
                HasSelectedFile = true;
                IsSuccess = true;

                // Notify parent that file was selected
                FileSelected?.Invoke(this, new FileSelectedEventArgs(fileResult));

                // Başarı mesajını 3 saniye göster, sonra dosya seçili durumuna geç
                await Task.Delay(3000);
                IsSuccess = false;
            }
            catch (Exception ex)
            {
                // State'i main thread'de tamamen sıfırla
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = false;
                    IsSuccess = false;
                HasSelectedFile = false;
                    SelectFileCommand.NotifyCanExecuteChanged();
                });
                
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Hata",
                        $"Dosya işleme hatası: {ex.Message}", "Tamam");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool IsValidFileType(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension == ".srt" || extension == ".vtt";
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        [RelayCommand]
        private void ClearFile()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                HasSelectedFile = false;
                UploadedFileName = string.Empty;
                FileSize = string.Empty;
                IsSuccess = false;
                IsLoading = false;
                SelectFileCommand.NotifyCanExecuteChanged();
            });
        }

        [RelayCommand]
        private async Task Continue()
        {
            // Continue event'ini fırlat - dosya bilgileriyle birlikte
            ContinueRequested?.Invoke(this, new ContinueEventArgs(UploadedFileName, FileSize));
        }

        public async Task ProcessSelectedFileFromPath(string fileName, string filePath)
        {
            // Loading state'ini başlat (drag & drop için)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsLoading = true;
                IsSuccess = false;
                HasSelectedFile = false;
                SelectFileCommand.NotifyCanExecuteChanged();
            });

            if (!IsValidFileType(fileName))
            {
                // Sadece geçersiz dosya tipi için alert göster
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Geçersiz Dosya Tipi", 
                        "Sadece SRT ve VTT dosyaları desteklenmektedir.\n\nLütfen .srt veya .vtt uzantılı bir dosya seçin.", "Tamam");
                }
                // State'i main thread'de sıfırla
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = false;
                    IsSuccess = false;
                    HasSelectedFile = false;
                    SelectFileCommand.NotifyCanExecuteChanged();
                });
                return;
            }

            try
            {
                // Dosya boyutu kontrolü (10MB limit)
                using var stream = File.OpenRead(filePath);
                if (stream.Length > 10 * 1024 * 1024) // 10MB
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Dosya Çok Büyük",
                            "Dosya boyutu 10MB'dan küçük olmalıdır.", "Tamam");
                    }
                    // State'i main thread'de sıfırla
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IsLoading = false;
                        IsSuccess = false;
                        HasSelectedFile = false;
                        SelectFileCommand.NotifyCanExecuteChanged();
                    });
                    return;
                }

                UploadedFileName = fileName;
                FileSize = FormatFileSize(stream.Length);
                HasSelectedFile = true;
                IsSuccess = true;

                // Notify parent that file was selected
                FileSelected?.Invoke(this, new FileSelectedEventArgs(new FileResult(filePath)));

                // Başarı mesajını 3 saniye göster, sonra dosya seçili durumuna geç
                await Task.Delay(3000);
                IsSuccess = false;
            }
            catch (Exception ex)
            {
                // State'i main thread'de tamamen sıfırla
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = false;
                    IsSuccess = false;
                    HasSelectedFile = false;
                    SelectFileCommand.NotifyCanExecuteChanged();
                });
                
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Hata",
                        $"Dosya işleme hatası: {ex.Message}", "Tamam");
                }
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = false;
                    SelectFileCommand.NotifyCanExecuteChanged();
                });
            }
        }
    }

    public class FileSelectedEventArgs : EventArgs
    {
        public FileResult FileResult { get; }

        public FileSelectedEventArgs(FileResult fileResult)
        {
            FileResult = fileResult;
        }
    }

    public class ContinueEventArgs : EventArgs
    {
        public string FileName { get; }
        public string FileSize { get; }

        public ContinueEventArgs(string fileName, string fileSize)
        {
            FileName = fileName;
            FileSize = fileSize;
        }
    }
}