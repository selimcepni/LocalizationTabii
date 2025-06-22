using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using LocalizationTabii.Services;
using Syncfusion.Maui.Buttons;

namespace LocalizationTabii.Pages.Settings
{
    public partial class ApiKeysView : ContentView
    {
        private readonly ImageSource _eyeIcon;
        private readonly ImageSource _eyeOffIcon;
        private readonly IApiKeyService _apiKeyService;

        public ApiKeysView()
        {
            InitializeComponent();
            
            // DI'dan ApiKeyService'i al
            _apiKeyService = MauiProgram.ServiceProvider.GetService<IApiKeyService>()!;
            
            // İkonları kaynaktan yükle
            _eyeIcon = (ImageSource)Application.Current.Resources["IconEye"];
            _eyeOffIcon = (ImageSource)Application.Current.Resources["IconEyeOff"];
            
            LoadApiKeys();
        }

        private void LoadApiKeys()
        {
            if (OpenAiApiKeyEntry != null)
                OpenAiApiKeyEntry.Text = _apiKeyService.GetOpenAiApiKey();
            if (AnthropicApiKeyEntry != null)
                AnthropicApiKeyEntry.Text = _apiKeyService.GetAnthropicApiKey();
            if (GoogleApiKeyEntry != null)
                GoogleApiKeyEntry.Text = _apiKeyService.GetGoogleApiKey();
            if (DeepSeekApiKeyEntry != null)
                DeepSeekApiKeyEntry.Text = _apiKeyService.GetDeepSeekApiKey();
        }

        private async void OnOpenAiKeySaveClicked(object sender, EventArgs e)
        {
            if (OpenAiApiKeyEntry?.Text != null)
            {
                _apiKeyService.SaveOpenAiApiKey(OpenAiApiKeyEntry.Text);
                
                await Application.Current.MainPage.DisplayAlert("Başarılı", "OpenAI API anahtarı kaydedildi.", "Tamam");
            }
        }

        private async void OnAnthropicKeySaveClicked(object sender, EventArgs e)
        {
            if (AnthropicApiKeyEntry?.Text != null)
            {
                _apiKeyService.SaveAnthropicApiKey(AnthropicApiKeyEntry.Text);
                await Application.Current.MainPage.DisplayAlert("Başarılı", "Anthropic API anahtarı kaydedildi.", "Tamam");
            }
        }

        private async void OnGoogleKeySaveClicked(object sender, EventArgs e)
        {
            if (GoogleApiKeyEntry?.Text != null)
            {
                _apiKeyService.SaveGoogleApiKey(GoogleApiKeyEntry.Text);
                await Application.Current.MainPage.DisplayAlert("Başarılı", "Google API anahtarı kaydedildi.", "Tamam");
            }
        }

        private async void OnDeepSeekKeySaveClicked(object sender, EventArgs e)
        {
            if (DeepSeekApiKeyEntry?.Text != null)
            {
                _apiKeyService.SaveDeepSeekApiKey(DeepSeekApiKeyEntry.Text);
                await Application.Current.MainPage.DisplayAlert("Başarılı", "DeepSeek API anahtarı kaydedildi.", "Tamam");
            }
        }

        private void OnOpenAiToggleVisibilityClicked(object sender, EventArgs e)
        {
            if (OpenAiApiKeyEntry != null)
            {
                OpenAiApiKeyEntry.IsPassword = !OpenAiApiKeyEntry.IsPassword;
                if (sender is SfButton button)
                {
                    button.ImageSource = OpenAiApiKeyEntry.IsPassword ? _eyeIcon : _eyeOffIcon;
                    button.ShowIcon = true;
                }
            }
        }

        private void OnAnthropicToggleVisibilityClicked(object sender, EventArgs e)
        {
            if (AnthropicApiKeyEntry != null)
            {
                AnthropicApiKeyEntry.IsPassword = !AnthropicApiKeyEntry.IsPassword;
                if (sender is SfButton button)
                {
                    button.ImageSource = AnthropicApiKeyEntry.IsPassword ? _eyeIcon : _eyeOffIcon;
                    button.ShowIcon = true;
                }
            }
        }

        private void OnGoogleToggleVisibilityClicked(object sender, EventArgs e)
        {
            if (GoogleApiKeyEntry != null)
            {
                GoogleApiKeyEntry.IsPassword = !GoogleApiKeyEntry.IsPassword;
                if (sender is SfButton button)
                {
                    button.ImageSource = GoogleApiKeyEntry.IsPassword ? _eyeIcon : _eyeOffIcon;
                    button.ShowIcon = true;
                }
            }
        }

        private void OnDeepSeekToggleVisibilityClicked(object sender, EventArgs e)
        {
            if (DeepSeekApiKeyEntry != null)
            {
                DeepSeekApiKeyEntry.IsPassword = !DeepSeekApiKeyEntry.IsPassword;
                if (sender is SfButton button)
                {
                    button.ImageSource = DeepSeekApiKeyEntry.IsPassword ? _eyeIcon : _eyeOffIcon;
                    button.ShowIcon = true;
                }
            }
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

        public string DeepSeekApiKey
        {
            get => DeepSeekApiKeyEntry?.Text ?? string.Empty;
            set
            {
                if (DeepSeekApiKeyEntry != null)
                    DeepSeekApiKeyEntry.Text = value;
            }
        }
    }
} 