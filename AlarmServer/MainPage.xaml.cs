using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;

namespace AlarmServer
{
    public sealed partial class MainPage : Page
    {
        Popup openedPopup;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            
            DataContext = (Application.Current as App).MainModel;
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= HardwareButtons_BackPressed;

            base.OnNavigatedFrom(e);
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            e.Handled = CloseOpenedPopup();
        }

        bool CloseOpenedPopup()
        {
            if (openedPopup != null && openedPopup.IsOpen)
            {
                openedPopup.IsOpen = false;
                openedPopup = null;
                return true;
            }

            return false;
        }

        private void OpenConsoleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CloseOpenedPopup())
                return;

            ConsoleControl control = new ConsoleControl()
            {
                Width = ActualWidth,
                Height = ActualHeight,
                DataContext = (Application.Current as App).MainModel
            };

            openedPopup = new Popup() { Child = control };
            openedPopup.IsOpen = true;
        }
    }
}
