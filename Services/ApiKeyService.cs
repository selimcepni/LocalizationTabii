using Microsoft.Maui.Storage;

namespace LocalizationTabii.Services
{
    public static class ApiKeyService
    {
        private const string OpenAiKeyPref = "OpenAI_ApiKey";
        private const string AnthropicKeyPref = "Anthropic_ApiKey";
        private const string GoogleKeyPref = "Google_ApiKey";

        public static string GetOpenAiApiKey()
        {
            return Preferences.Get(OpenAiKeyPref, string.Empty);
        }

        public static string GetAnthropicApiKey()
        {
            return Preferences.Get(AnthropicKeyPref, string.Empty);
        }

        public static string GetGoogleApiKey()
        {
            return Preferences.Get(GoogleKeyPref, string.Empty);
        }

        public static void SaveOpenAiApiKey(string apiKey)
        {
            Preferences.Set(OpenAiKeyPref, apiKey);
        }

        public static void SaveAnthropicApiKey(string apiKey)
        {
            Preferences.Set(AnthropicKeyPref, apiKey);
        }

        public static void SaveGoogleApiKey(string apiKey)
        {
            Preferences.Set(GoogleKeyPref, apiKey);
        }

        public static bool HasAnyApiKey()
        {
            return !string.IsNullOrEmpty(GetOpenAiApiKey()) ||
                   !string.IsNullOrEmpty(GetAnthropicApiKey()) ||
                   !string.IsNullOrEmpty(GetGoogleApiKey());
        }
    }
}