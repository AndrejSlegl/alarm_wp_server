using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AlarmServer.Views
{
    public sealed partial class ConsoleView : Page
    {
        public ConsoleView()
        {
            this.InitializeComponent();

            DataContext = (Application.Current as App).MainModel;
        }
    }
}
