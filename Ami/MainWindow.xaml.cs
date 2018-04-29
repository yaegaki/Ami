using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Ami
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel viewModel;
        private SettingService settingService = new SettingService();

        public MainWindow()
        {
            this.settingService.Load();
            this.viewModel = new MainWindowViewModel(this.settingService);

            InitializeComponent();
            this.DataContext = this.viewModel;
        }

        private void CaptureClick(object sender, RoutedEventArgs e)
        {
            this.viewModel.Capture();
        }

        private void TweetClick(object sender, RoutedEventArgs e)
        {
            this.viewModel.Tweet();
        }

        private void TwitterAuthorizeClick(object sender, RoutedEventArgs e)
        {
            this.viewModel.TwitterAuthorize();
        }

        private void SelectRectClick(object sender, RoutedEventArgs e)
        {
            this.viewModel.SelectRect();
        }

        private void grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.S)
                {
                    e.Handled = true;
                    this.viewModel.Capture();
                }
                else if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    this.viewModel.Tweet();
                }
            }

        }

        private void listview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.viewModel.SelectedImages.Clear();
            foreach (var image in this.listview.SelectedItems.OfType<BitmapSource>())
            {
                this.viewModel.SelectedImages.Add(image);
            }
        }
    }
}
