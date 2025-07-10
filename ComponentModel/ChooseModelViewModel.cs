using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Models;
using LocalizationTabii.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SyncfusionListView = Syncfusion.Maui.ListView;

namespace LocalizationTabii.ComponentModel
{
    public partial class ChooseModelViewModel : ObservableObject
    {
        private readonly ISemanticKernelService _semanticKernelService;

        [ObservableProperty]
        private string selectedFileName = string.Empty;

        [ObservableProperty]
        private string fileSize = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ModelInfo> availableModels = new();

        [ObservableProperty]
        private ModelInfo? selectedModel;

        [ObservableProperty]
        private bool isLoading = true;

        public bool HasNoModels => !IsLoading && AvailableModels.Count == 0;

        public event EventHandler<ModelSelectedEventArgs>? ModelSelected;
        public event EventHandler? GoBackRequested;

        public ChooseModelViewModel(ISemanticKernelService semanticKernelService)
        {
            _semanticKernelService = semanticKernelService;
            
            System.Diagnostics.Debug.WriteLine("🚀 ChooseModelViewModel başlatılıyor...");
            
            // Direkt gerçek modelleri yükle
            _ = LoadAvailableModelsAsync();
        }

        public void SetFileInfo(string fileName, string fileSize)
        {
            SelectedFileName = fileName;
            FileSize = fileSize;
            System.Diagnostics.Debug.WriteLine($"🔧 Dosya bilgisi ayarlandı: {fileName} - {fileSize}");
        }

        private async Task LoadAvailableModelsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔧 GERÇEK Model yükleme başladı...");
                IsLoading = true;
                
                // API key'leri kontrol et
                var apiKeyService = MauiProgram.ServiceProvider.GetRequiredService<IApiKeyService>();
                
                bool hasOpenAI = !string.IsNullOrEmpty(apiKeyService.GetOpenAiApiKey());
                bool hasGoogle = !string.IsNullOrEmpty(apiKeyService.GetGoogleApiKey());
                bool hasAnthropic = !string.IsNullOrEmpty(apiKeyService.GetAnthropicApiKey());
                bool hasDeepSeek = !string.IsNullOrEmpty(apiKeyService.GetDeepSeekApiKey());
                
                System.Diagnostics.Debug.WriteLine($"🔧 API Key durumları:");
                System.Diagnostics.Debug.WriteLine($"🔧   OpenAI: {(hasOpenAI ? "✅ VAR" : "❌ YOK")}");
                System.Diagnostics.Debug.WriteLine($"🔧   Google: {(hasGoogle ? "✅ VAR" : "❌ YOK")}");
                System.Diagnostics.Debug.WriteLine($"🔧   Anthropic: {(hasAnthropic ? "✅ VAR" : "❌ YOK")}");
                System.Diagnostics.Debug.WriteLine($"🔧   DeepSeek: {(hasDeepSeek ? "✅ VAR" : "❌ YOK")}");
                
                // Semantic Kernel'dan modelleri al
                var modelIdentifiers = _semanticKernelService.GetAvailableModels();
                System.Diagnostics.Debug.WriteLine($"🔧 Semantic Kernel'dan {modelIdentifiers.Count} model bulundu");
                
                // Mevcut listeyi temizle
                AvailableModels.Clear();
                
                if (modelIdentifiers.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("🔧 GERÇEK modeller yükleniyor:");
                    
                    foreach (var identifier in modelIdentifiers)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔧   İşleniyor: {identifier}");
                        
                        var displayName = _semanticKernelService.GetModelDisplayName(identifier);
                        var provider = identifier.Split('_')[0];
                        
                        var modelInfo = new ModelInfo
                        {
                            Identifier = identifier,
                            DisplayName = displayName,
                            Provider = provider,
                            Description = GetModelDescription(provider)
                        };

                        AvailableModels.Add(modelInfo);
                        System.Diagnostics.Debug.WriteLine($"🔧   ✅ Eklendi: {displayName} ({provider})");
                    }
                    
                    // İlk modeli seç
                    if (AvailableModels.Count > 0)
                    {
                        SelectedModel = AvailableModels.First();
                        System.Diagnostics.Debug.WriteLine($"🔧 ✅ İlk model seçildi: {SelectedModel.DisplayName}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🔧 ❌ Semantic Kernel'dan model bulunamadı!");
                    
                    // Fallback: Test modelleri ekle
                    System.Diagnostics.Debug.WriteLine("🔧 📋 Fallback test modelleri ekleniyor...");
                    
                    var fallbackModels = new[]
                    {
                        new ModelInfo { Identifier = "OpenAI_gpt-4o", DisplayName = "OpenAI GPT-4o", Provider = "OpenAI", Description = "Yüksek kaliteli çeviri" },
                        new ModelInfo { Identifier = "OpenAI_gpt-4o-mini", DisplayName = "OpenAI GPT-4o Mini", Provider = "OpenAI", Description = "Hızlı ve ekonomik" },
                        new ModelInfo { Identifier = "OpenAI_gpt-4-turbo", DisplayName = "OpenAI GPT-4 Turbo", Provider = "OpenAI", Description = "Güçlü çeviri modeli" },
                        new ModelInfo { Identifier = "OpenAI_o3", DisplayName = "OpenAI o3", Provider = "OpenAI", Description = "En yeni model" },
                        new ModelInfo { Identifier = "OpenAI_o3-pro", DisplayName = "OpenAI o3-Pro", Provider = "OpenAI", Description = "Pro seviye model" }
                    };
                    
                    foreach (var model in fallbackModels)
                    {
                        AvailableModels.Add(model);
                        System.Diagnostics.Debug.WriteLine($"🔧   📋 Fallback eklendi: {model.DisplayName}");
                    }
                    
                    if (AvailableModels.Count > 0)
                    {
                        SelectedModel = AvailableModels.First();
                        System.Diagnostics.Debug.WriteLine($"🔧 📋 Fallback model seçildi: {SelectedModel.DisplayName}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"🔧 ✅ Toplam {AvailableModels.Count} model yüklendi");
                
                // UI'ı güncelle
                OnPropertyChanged(nameof(AvailableModels));
                OnPropertyChanged(nameof(HasNoModels));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Model yükleme hatası: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                
                // Hata durumunda en azından bir model ekle
                AvailableModels.Add(new ModelInfo 
                { 
                    Identifier = "Error_Model", 
                    DisplayName = "Hata: Model Yüklenemedi", 
                    Provider = "System", 
                    Description = "Model yükleme hatası" 
                });
                
                if (AvailableModels.Count > 0)
                {
                    SelectedModel = AvailableModels.First();
                }
            }
            finally
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine("🔧 ✅ Model yükleme tamamlandı");
                System.Diagnostics.Debug.WriteLine($"🔧 📊 Durum: IsLoading={IsLoading}, ModelCount={AvailableModels.Count}, HasNoModels={HasNoModels}");
            }
        }

        private string GetModelDescription(string provider)
        {
            return provider switch
            {
                "OpenAI" => "Yüksek kaliteli çeviri ve anlam korunması",
                "Google" => "Hızlı ve çok dilli çeviri desteği", 
                "Anthropic" => "Güvenli ve ayrıntılı çeviri yaklaşımı",
                "DeepSeek" => "Açık kaynak tabanlı ve ekonomik çeviri",
                _ => "Profesyonel çeviri hizmeti"
            };
        }

        partial void OnIsLoadingChanged(bool value)
        {
            OnPropertyChanged(nameof(HasNoModels));
            System.Diagnostics.Debug.WriteLine($"🔧 IsLoading değişti: {value}");
        }

        partial void OnAvailableModelsChanged(ObservableCollection<ModelInfo> value)
        {
            OnPropertyChanged(nameof(HasNoModels));
            System.Diagnostics.Debug.WriteLine($"🔧 AvailableModels değişti: {value?.Count ?? 0} model");
            
            // Collection değişikliklerini de takip et
            if (value != null)
            {
                value.CollectionChanged += (s, e) => {
                    OnPropertyChanged(nameof(HasNoModels));
                    System.Diagnostics.Debug.WriteLine($"🔧 Collection değişti: {value.Count} model");
                };
            }
        }

        [RelayCommand]
        private void GoBack()
        {
            System.Diagnostics.Debug.WriteLine("🔧 Geri dön tuşuna basıldı");
            GoBackRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void SelectModel(object parameter)
        {
            try
            {
                // Syncfusion TapCommand ItemTappedEventArgs gönderir
                if (parameter is SyncfusionListView.ItemTappedEventArgs tappedEventArgs)
                {
                    if (tappedEventArgs.DataItem is ModelInfo model)
                    {
                        SelectedModel = model;
                        System.Diagnostics.Debug.WriteLine($"🔧 ✅ Model seçildi (Syncfusion): {model.DisplayName} ({model.Identifier})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"🔧 ❌ DataItem null veya yanlış tip: {tappedEventArgs.DataItem?.GetType().Name ?? "null"}");
                    }
                }
                // Fallback: Direkt ModelInfo objesi
                else if (parameter is ModelInfo directModel)
                {
                    SelectedModel = directModel;
                    System.Diagnostics.Debug.WriteLine($"🔧 ✅ Model seçildi (Direct): {directModel.DisplayName} ({directModel.Identifier})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 ❌ Bilinmeyen parameter tipi: {parameter?.GetType().Name ?? "null"}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Model seçim hatası: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task StartTranslation()
        {
            if (SelectedModel == null) 
            {
                System.Diagnostics.Debug.WriteLine("❌ Model seçilmemiş, çeviri başlatılamaz");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"🔧 🚀 Çeviri başlatılıyor: {SelectedModel.DisplayName}");
            
            // Model seçimi event'ini fırlat
            ModelSelected?.Invoke(this, new ModelSelectedEventArgs(SelectedModel.Identifier, SelectedFileName, FileSize));
        }
    }

    public class ModelInfo
    {
        public string Identifier { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ModelSelectedEventArgs : EventArgs
    {
        public string SelectedModel { get; }
        public string FileName { get; }
        public string FileSize { get; }

        public ModelSelectedEventArgs(string selectedModel, string fileName, string fileSize)
        {
            SelectedModel = selectedModel;
            FileName = fileName;
            FileSize = fileSize;
        }
    }
} 