namespace testmvvp.Views
{
    using Classes;
    using ViewModels;
    using Windows.UI.Xaml.Controls;

    /// <summary>Models
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            DataContext = new MainPageViewModel(new RFDevice());
        }
    }
}
