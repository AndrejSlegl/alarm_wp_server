using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AlarmServer.Views
{
    public sealed partial class AlarmSettingsView : Page
    {
        public AlarmSettingsView()
        {
            this.InitializeComponent();

            DataContext = (Application.Current as App).MainModel;
        }
    }
}
