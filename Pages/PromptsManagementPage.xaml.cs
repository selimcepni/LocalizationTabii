namespace LocalizationTabii.Pages
{
    public partial class PromptsManagementPage : ContentPage
    {
        public PromptsManagementPage(PromptsManagementPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }


       
    }
}