using Microsoft.Extensions.DependencyInjection;
using LocalizationTabii.Services;
using Microsoft.SemanticKernel;
using System.Text;

namespace LocalizationTabii.Pages;

public partial class TestSemanticKernelPage : ContentPage
{
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly StringBuilder _detailedLog = new();
    private Kernel? _testKernel;

    public TestSemanticKernelPage()
    {
        InitializeComponent();
        
        // Dependency Injection'dan servisi alıyoruz
        _semanticKernelService = MauiProgram.ServiceProvider.GetRequiredService<ISemanticKernelService>();
        
        LogMessage("🎯 Test sayfası başlatıldı. Testlere hazır!");
    }

    private void LogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {message}\n";
        _detailedLog.Append(logEntry);
        
        // UI'da göster
        MainThread.BeginInvokeOnMainThread(() =>
        {
            DetailedResults.Text = _detailedLog.ToString();
        });
        
        // Console'a da yazdır
        System.Diagnostics.Debug.WriteLine($"🧪 {logEntry.Trim()}");
    }

    private async void OnTestConfigClicked(object sender, EventArgs e)
    {
        try
        {
            LogMessage("📋 Adım 1 başlatılıyor: Yapılandırma kontrolü...");
            
            // GetAvailableModels çağrısı yapılandırmanın yüklendiğini gösterir
            var availableModels = _semanticKernelService.GetAvailableModels();
            
            if (availableModels.Any())
            {
                ConfigResult.Text = $"✅ Başarılı! {availableModels.Count} model yapılandırması bulundu";
                ConfigResult.TextColor = Colors.Green;
                LogMessage($"✅ Yapılandırma başarılı. Bulunan modeller: {string.Join(", ", availableModels)}");
            }
            else
            {
                ConfigResult.Text = "⚠️ Yapılandırma tamam ama API anahtarı yok";
                ConfigResult.TextColor = Colors.Orange;
                LogMessage("⚠️ Yapılandırma yüklendi ancak hiçbir sağlayıcı için API anahtarı tanımlanmamış");
            }
        }
        catch (Exception ex)
        {
            ConfigResult.Text = $"❌ Hata: {ex.Message}";
            ConfigResult.TextColor = Colors.Red;
            LogMessage($"❌ Yapılandırma testi başarısız: {ex.Message}");
        }
    }

    private async void OnTestModelsClicked(object sender, EventArgs e)
    {
        try
        {
            LogMessage("🤖 Adım 2 başlatılıyor: Kullanılabilir modeller kontrol ediliyor...");
            
            var availableModels = _semanticKernelService.GetAvailableModels();
            
            if (availableModels.Any())
            {
                // Picker'ı güncelle
                ModelPicker.ItemsSource = availableModels;
                
                ModelsResult.Text = $"✅ {availableModels.Count} model kullanılabilir";
                ModelsResult.TextColor = Colors.Green;
                
                LogMessage($"✅ Kullanılabilir modeller ({availableModels.Count} adet):");
                foreach (var model in availableModels)
                {
                    var displayName = _semanticKernelService.GetModelDisplayName(model);
                    LogMessage($"   • {model} → {displayName}");
                }
            }
            else
            {
                ModelsResult.Text = "⚠️ Hiç model bulunamadı. API anahtarlarını kontrol edin";
                ModelsResult.TextColor = Colors.Orange;
                LogMessage("⚠️ Hiç kullanılabilir model bulunamadı. Muhtemelen API anahtarları eksik.");
            }
        }
        catch (Exception ex)
        {
            ModelsResult.Text = $"❌ Hata: {ex.Message}";
            ModelsResult.TextColor = Colors.Red;
            LogMessage($"❌ Model listesi alma başarısız: {ex.Message}");
        }
    }

    private async void OnTestKernelClicked(object sender, EventArgs e)
    {
        try
        {
            if (ModelPicker.SelectedItem == null)
            {
                KernelResult.Text = "⚠️ Önce bir model seçin";
                KernelResult.TextColor = Colors.Orange;
                return;
            }

            var selectedModel = ModelPicker.SelectedItem.ToString();
            LogMessage($"⚙️ Adım 3 başlatılıyor: {selectedModel} modeli için Kernel oluşturuluyor...");
            
            _testKernel = _semanticKernelService.CreateKernelForModel(selectedModel!);
            
            if (_testKernel != null)
            {
                KernelResult.Text = $"✅ Kernel başarıyla oluşturuldu: {selectedModel}";
                KernelResult.TextColor = Colors.Green;
                LogMessage($"✅ Kernel başarıyla oluşturuldu. Model: {selectedModel}");
                
                // AI test butonunu aktif et
                TestAIButton.IsEnabled = true;
            }
            else
            {
                KernelResult.Text = "❌ Kernel oluşturulamadı (null döndü)";
                KernelResult.TextColor = Colors.Red;
                LogMessage("❌ CreateKernelForModel null döndürdü");
            }
        }
        catch (Exception ex)
        {
            KernelResult.Text = $"❌ Hata: {ex.Message}";
            KernelResult.TextColor = Colors.Red;
            LogMessage($"❌ Kernel oluşturma başarısız: {ex.Message}");
        }
    }

    private async void OnTestAIClicked(object sender, EventArgs e)
    {
        if (_testKernel == null)
        {
            AIResult.Text = "⚠️ Önce Kernel oluşturun";
            AIResult.TextColor = Colors.Orange;
            return;
        }

        if (string.IsNullOrWhiteSpace(TestPromptEntry.Text))
        {
            AIResult.Text = "⚠️ Test sorusu girin";
            AIResult.TextColor = Colors.Orange;
            return;
        }

        try
        {
            var prompt = TestPromptEntry.Text.Trim();
            LogMessage($"🚀 Adım 4 başlatılıyor: AI'ya soru soruluyor: '{prompt}'");
            
            // Loading göster
            AIResult.Text = "🔄 AI düşünüyor...";
            AIResult.TextColor = Colors.Blue;
            TestAIButton.IsEnabled = false;
            
            // AI'ya sor
            var startTime = DateTime.Now;
            var result = await _testKernel.InvokePromptAsync(prompt);
            var endTime = DateTime.Now;
            var duration = endTime - startTime;
            
            var response = result.GetValue<string>();
            
            if (!string.IsNullOrEmpty(response))
            {
                AIResult.Text = $"✅ AI yanıtladı! ({duration.TotalSeconds:F1}s)";
                AIResult.TextColor = Colors.Green;
                LogMessage($"✅ AI başarıyla yanıtladı! Süre: {duration.TotalSeconds:F1} saniye");
                LogMessage($"📝 AI Yanıtı: {response}");
                LogMessage("🎉 TÜM TESTLER BAŞARILI! SemanticKernelService düzgün çalışıyor.");
            }
            else
            {
                AIResult.Text = "⚠️ AI boş yanıt döndürdü";
                AIResult.TextColor = Colors.Orange;
                LogMessage("⚠️ AI boş yanıt döndürdü");
            }
        }
        catch (Exception ex)
        {
            AIResult.Text = $"❌ Hata: {ex.Message}";
            AIResult.TextColor = Colors.Red;
            LogMessage($"❌ AI sorgusu başarısız: {ex.Message}");
        }
        finally
        {
            TestAIButton.IsEnabled = true;
        }
    }
} 