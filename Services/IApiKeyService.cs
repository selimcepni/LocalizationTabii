namespace LocalizationTabii.Services;

/// <summary>
/// API anahtarlarını yönetmek için interface
/// </summary>
public interface IApiKeyService
{
    string GetOpenAiApiKey();
    string GetAnthropicApiKey();
    string GetGoogleApiKey();
    string GetDeepSeekApiKey();
    void SaveOpenAiApiKey(string apiKey);
    void SaveAnthropicApiKey(string apiKey);
    void SaveGoogleApiKey(string apiKey);
    void SaveDeepSeekApiKey(string apiKey);
    bool HasAnyApiKey();
} 