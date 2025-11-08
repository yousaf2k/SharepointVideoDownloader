using System.Windows;
using Sharepoint_Video_Downloader.ViewModels;

namespace Sharepoint_Video_Downloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void txtLog_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            txtLog.ScrollToEnd();
        }
    }
}