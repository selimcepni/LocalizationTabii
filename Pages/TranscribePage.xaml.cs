namespace LocalizationTabii.Pages
{
    public partial class TranscribePage : ContentPage
    {
        public TranscribePage(TranscribePageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }


       
    }
}