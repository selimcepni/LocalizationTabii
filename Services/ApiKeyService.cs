using Microsoft.Maui.Storage;

namespace LocalizationTabii.Services
{
    public class ApiKeyService : IApiKeyService
    {
        private const string OpenAiKeyPref = "OpenAI_ApiKey";
        private const string AnthropicKeyPref = "Anthropic_ApiKey";
        private const string GoogleKeyPref = "Google_ApiKey";
        private const string DeepSeekKeyPref = "DeepSeek_ApiKey";

        public string GetOpenAiApiKey()
        {
            return Preferences.Get(OpenAiKeyPref, string.Empty);
        }

        public string GetAnthropicApiKey()
        {
            return Preferences.Get(AnthropicKeyPref, string.Empty);
        }

        public string GetGoogleApiKey()
        {
            return Preferences.Get(GoogleKeyPref, string.Empty);
        }

        public string GetDeepSeekApiKey()
        {
            return Preferences.Get(DeepSeekKeyPref, string.Empty);
        }

        public void SaveOpenAiApiKey(string apiKey)
        {
            Preferences.Set(OpenAiKeyPref, apiKey);
        }

        public void SaveAnthropicApiKey(string apiKey)
        {
            Preferences.Set(AnthropicKeyPref, apiKey);
        }

        public void SaveGoogleApiKey(string apiKey)
        {
            Preferences.Set(GoogleKeyPref, apiKey);
        }

        public void SaveDeepSeekApiKey(string apiKey)
        {
            Preferences.Set(DeepSeekKeyPref, apiKey);
        }

        public bool HasAnyApiKey()
        {
            return !string.IsNullOrEmpty(GetOpenAiApiKey()) ||
                   !string.IsNullOrEmpty(GetAnthropicApiKey()) ||
                   !string.IsNullOrEmpty(GetGoogleApiKey()) ||
                   !string.IsNullOrEmpty(GetDeepSeekApiKey());
        }
    }
}