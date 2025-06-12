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
            var result = await addPromptPopup.ShowPopupAsync();
            
            if (result != null)
            {
                await _viewModel.AddPromptAsync(result);
                
                // Başarı mesajı göster
                await DisplayAlert("✅ Başarılı", $"'{result.Title}' prompt'u başarıyla eklendi!", "Tamam");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Prompt eklenirken hata oluştu: {ex.Message}", "Tamam");
        }
    }

    private async void OnShowEditPromptRequested(object? sender, Prompt e)
    {
        try
        {
            if (e == null)
            {
                await DisplayAlert("Hata", "Düzenlenecek prompt bulunamadı.", "Tamam");
                return;
            }
            
            var result = await editPromptPopup.ShowPopupAsync(e);
            
            if (result != null)
            {
                await _viewModel.UpdatePromptAsync(result);
                
                // Başarı mesajı göster
                await DisplayAlert("✅ Başarılı", $"'{result.Title}' prompt'u başarıyla güncellendi!", "Tamam");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Prompt güncellenirken hata oluştu: {ex.Message}", "Tamam");
        }
    }
}