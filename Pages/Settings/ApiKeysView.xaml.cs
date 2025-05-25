using Microsoft.Maui.Controls;

namespace LocalizationTabii.Pages.Settings
{
    public partial class ApiKeysView : ContentView
    {
        public ApiKeysView()
        {
            InitializeComponent();
        }

        public string OpenAiApiKey
        {
            get => OpenAiApiKeyEntry?.Text ?? string.Empty;
            set
            {
                if (OpenAiApiKeyEntry != null)
                    OpenAiApiKeyEntry.Text = value;
            }
        }

        public string AnthropicApiKey
        {
            get => AnthropicApiKeyEntry?.Text ?? string.Empty;
            set
            {
                if (AnthropicApiKeyEntry != null)
                    AnthropicApiKeyEntry.Text = value;
            }
        }

        public string GoogleApiKey
        {
            get => GoogleApiKeyEntry?.Text ?? string.Empty;
            set
            {
                if (GoogleApiKeyEntry != null)
                    GoogleApiKeyEntry.Text = value;
            }
        }
    }
} 