using Microsoft.Maui.Controls;
using LocalizationTabii.Pages.Settings;
using System;

namespace LocalizationTabii.Pages
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage(SettingsPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }

        private void ApiKeysMenuGrid_Tapped(object sender, EventArgs e)
        {
            DefaultContentView.IsVisible = false;
            ApiKeysContentView.IsVisible = true;
        }
    }
}