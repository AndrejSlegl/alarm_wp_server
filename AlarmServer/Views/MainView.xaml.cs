using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace AlarmServer.Views
{
    public sealed partial class MainView : Page
    {
        public MainView()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            
            DataContext = (Application.Current as App).MainModel;
        }

        private void OpenConsoleBtn_Click(object sender, RoutedEventArgs e)
        {
            var frame = (Frame)Window.Current.Content;
            frame.Navigate(typeof(ConsoleView));
        }

        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var frame = (Frame)Window.Current.Content;
            frame.Navigate(typeof(AlarmSettingsView));
        }
    }
}
