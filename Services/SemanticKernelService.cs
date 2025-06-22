using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.Extensions.Options;
using LocalizationTabii.Models;
using System.Text.RegularExpressions;

namespace LocalizationTabii.Services;

/// <summary>
/// Kullanıcının seçtiği modele göre dinamik olarak yapılandırılmış
/// bir Semantic Kernel nesnesi oluşturan merkezi servis.
/// Artık model listesi appsettings.json dosyasından okunuyor.
/// </summary>
public class SemanticKernelService : ISemanticKernelService
{
    private readonly IApiKeyService _apiKeyService;
    // Artık hard-coded bir liste yerine hızlı arama için bir sözlük kullanıyoruz.
    private readonly IReadOnlyDictionary<string, ModelConfiguration> _supportedModels;

    /// <summary>
    /// Constructor - IOptions kullanarak yapılandırmayı enjekte ediyoruz.
    /// </summary>
    public SemanticKernelService(
        IApiKeyService apiKeyService,
        IOptions<ModelProviderSettings> modelProviderOptions)
    {
        _apiKeyService = apiKeyService;

        // Yapılandırmadan gelen listeyi, Identifier'ı anahtar olan bir sözlüğe dönüştürüyoruz.
        // Bu, her seferinde listeyi taramak yerine anında erişim sağlar (çok daha performanslıdır).
        _supportedModels = modelProviderOptions.Value.Models
            .ToDictionary(m => m.Identifier, m => m, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Belirtilen model tanımlayıcısına göre yapılandırılmış bir Kernel nesnesi oluşturur.
    /// </summary>
    /// <param name="modelIdentifier">Kullanıcının seçtiği model tanımlayıcısı</param>
    /// <returns>Kullanıma hazır bir Kernel nesnesi.</returns>
    /// <exception cref="ArgumentException">API anahtarı bulunamazsa fırlatılır.</exception>
    /// <exception cref="NotSupportedException">Sağlanan model tanımlayıcısı desteklenmiyorsa fırlatılır.</exception>
    public Kernel CreateKernelForModel(string modelIdentifier)
    {
        // Sözlükten modeli buluyoruz.
        if (string.IsNullOrWhiteSpace(modelIdentifier) || !_supportedModels.TryGetValue(modelIdentifier, out var modelConfig))
        {
            throw new NotSupportedException($"'{modelIdentifier}' model tanımlayıcısı desteklenmiyor veya geçersiz.");
        }

        var builder = Kernel.CreateBuilder();

        switch (modelConfig.Provider)
        {
            case "OpenAI":
                string openAiKey = _apiKeyService.GetOpenAiApiKey();
                if (string.IsNullOrEmpty(openAiKey))
                    throw new ArgumentException("OpenAI API anahtarı bulunamadı. Lütfen ayarlardan ekleyin.");
                
                builder.AddOpenAIChatCompletion(modelId: modelConfig.ModelId, apiKey: openAiKey);
                break;

            case "Google":
                string googleKey = _apiKeyService.GetGoogleApiKey();
                if (string.IsNullOrEmpty(googleKey))
                    throw new ArgumentException("Google API anahtarı bulunamadı. Lütfen ayarlardan ekleyin.");

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only
                builder.AddGoogleAIGeminiChatCompletion(modelId: modelConfig.ModelId, apiKey: googleKey);
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only
                break;

            case "Anthropic":
                string anthropicKey = _apiKeyService.GetAnthropicApiKey();
                if (string.IsNullOrEmpty(anthropicKey))
                    throw new ArgumentException("Anthropic API anahtarı bulunamadı. Lütfen ayarlardan ekleyin.");

                // Anthropic için OpenAI-compatible endpoint kullanacağız
                builder.AddOpenAIChatCompletion(
                    modelId: modelConfig.ModelId,
                    apiKey: anthropicKey,
                    httpClient: new HttpClient(),
                    endpoint: new Uri("https://api.anthropic.com/v1"));
                break;

            case "DeepSeek":
                string deepSeekKey = _apiKeyService.GetDeepSeekApiKey();
                if (string.IsNullOrEmpty(deepSeekKey))
                    throw new ArgumentException("DeepSeek API anahtarı bulunamadı. Lütfen ayarlardan ekleyin.");

                // DeepSeek OpenAI-compatible API kullanıyor
                builder.AddOpenAIChatCompletion(
                    modelId: modelConfig.ModelId,
                    apiKey: deepSeekKey,
                    httpClient: new HttpClient(),
                    endpoint: new Uri("https://api.deepseek.com/v1"));
                break;

            default:
                throw new InvalidOperationException($"'{modelConfig.Provider}' sağlayıcısı için bir yapılandırma mantığı bulunamadı.");
        }

        return builder.Build();
    }

    /// <summary>
    /// Kullanılabilir modellerin listesini döndürür.
    /// Artık yapılandırmadan gelen modelleri kontrol ediyoruz.
    /// </summary>
    /// <returns>Desteklenen model tanımlayıcılarının listesi.</returns>
    public List<string> GetAvailableModels()
    {
        var availableModels = new List<string>();

        // Artık yapılandırmadan gelen modelleri kontrol ediyoruz.
        foreach (var modelConfig in _supportedModels.Values)
        {
            bool apiKeyAvailable = modelConfig.Provider switch
            {
                "OpenAI" => !string.IsNullOrEmpty(_apiKeyService.GetOpenAiApiKey()),
                "Google" => !string.IsNullOrEmpty(_apiKeyService.GetGoogleApiKey()),
                "Anthropic" => !string.IsNullOrEmpty(_apiKeyService.GetAnthropicApiKey()),
                "DeepSeek" => !string.IsNullOrEmpty(_apiKeyService.GetDeepSeekApiKey()),
                _ => false
            };

            if (apiKeyAvailable)
            {
                availableModels.Add(modelConfig.Identifier);
            }
        }

        return availableModels;
    }

    /// <summary>
    /// Model tanımlayıcısından insan okunabilir isim üretir.
    /// </summary>
    /// <param name="modelIdentifier">Model tanımlayıcısı</param>
    /// <returns>İnsan okunabilir model ismi</returns>
    public string GetModelDisplayName(string modelIdentifier)
    {
        if (string.IsNullOrWhiteSpace(modelIdentifier) || !_supportedModels.TryGetValue(modelIdentifier, out var modelConfig))
        {
            return "Bilinmeyen Model";
        }

        // Eğer DisplayName ayarlanmışsa onu kullan, yoksa otomatik oluştur
        if (!string.IsNullOrWhiteSpace(modelConfig.DisplayName))
        {
            return $"{modelConfig.Provider} {modelConfig.DisplayName}";
        }

        // Otomatik display name oluştur
        var cleanModelName = modelConfig.ModelId.Split('-')
            .Select(p => char.ToUpper(p[0]) + p.Substring(1))
            .Aggregate((a, b) => a + " " + b);
        cleanModelName = Regex.Replace(cleanModelName, @" \d{8}$", "").Replace("Latest", "").Trim();

        return $"{modelConfig.Provider} {cleanModelName}";
    }
} 