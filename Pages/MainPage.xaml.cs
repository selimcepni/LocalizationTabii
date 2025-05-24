using LocalizationTabii.Models;
using LocalizationTabii.PageModels;

namespace LocalizationTabii.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}