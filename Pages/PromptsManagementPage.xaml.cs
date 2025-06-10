using LocalizationTabii.PageModels;
using LocalizationTabii.Models;
using LocalizationTabii.Components;
using Syncfusion.Maui.Toolkit;

namespace LocalizationTabii.Pages;

public partial class PromptsManagementPage : ContentPage
{
    private readonly PromptsManagementPageModel _viewModel;

    public PromptsManagementPage(PromptsManagementPageModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        // Event'leri bağla
        _viewModel.ShowAddPromptRequested += OnShowAddPromptRequested;
        _viewModel.ShowEditPromptRequested += OnShowEditPromptRequested;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Event'leri temizle
        _viewModel.ShowAddPromptRequested -= OnShowAddPromptRequested;
        _viewModel.ShowEditPromptRequested -= OnShowEditPromptRequested;
    }



    private async void OnShowAddPromptRequested(object? sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔄 OnShowAddPromptRequested başladı");
            var result = await addPromptPopup.ShowPopupAsync();
            
            System.Diagnostics.Debug.WriteLine($"🔄 Popup tamamlandı, result: {(result != null ? "var" : "null")}");
            
            if (result != null)
            {
                System.Diagnostics.Debug.WriteLine($"🔄 AddPromptAsync çağrılıyor: Title='{result.Title}'");
                await _viewModel.AddPromptAsync(result);
                System.Diagnostics.Debug.WriteLine($"✅ AddPromptAsync tamamlandı");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ℹ️ Kullanıcı popup'ı iptal etti");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ AddPrompt popup hatası: {ex.Message}");
            await DisplayAlert("Hata", "Popup açılırken hata oluştu: " + ex.Message, "Tamam");
        }
    }

    private async void OnShowEditPromptRequested(object? sender, Prompt e)
    {
        try
        {
            var editPopup = new EditPromptPopup();
            var result = await editPopup.ShowPopupAsync(e);
            
            if (result != null)
            {
                await _viewModel.UpdatePromptAsync(result);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Prompt düzenleme sırasında hata oluştu: {ex.Message}", "Tamam");
        }
    }


}