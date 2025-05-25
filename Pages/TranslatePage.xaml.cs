namespace LocalizationTabii.Pages
{
    public partial class TranslatePage : ContentPage
    {
        public TranslatePage(TranslatePageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }


        private void button_Clicked(object sender, EventArgs e)
        {
            popup.Show();
        }
    }
}