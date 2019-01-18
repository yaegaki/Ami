using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        private Task tweetTask; 

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
            Tweet();
        }

        private void TwitterAuthorizeClick(object sender, RoutedEventArgs e)
        {
            this.viewModel.TwitterAuthorize();
        }

        private void SelectRectClick(object sender, RoutedEventArgs e)
        {
            this.viewModel.SelectRect();
        }

        private void ClearClick(object sender, RoutedEventArgs e)
        {
            this.viewModel.ClearImages();
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
                    Tweet();
                }
            }

        }

        private void listview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.viewModel.SelectedImages.Clear();
            foreach (var imageVM in this.listview.SelectedItems.OfType<ImageViewModel>())
            {
                this.viewModel.SelectedImages.Add(imageVM);
            }
        }

        private void Tweet()
        {
            // 結果は無視
            TweetAsync().ContinueWith(_ => { });
        }

        private async Task TweetAsync()
        {
            if (this.tweetTask != null)
            {
                return;
            }

            try
            {
                this.tweetTask = this.viewModel.TweetAsync();
                await this.tweetTask;
                // ツイートが終わったらテキストボックスにフォーカスする
                this.tweetTextBox.Focus();
            }
            catch
            {
            }
            finally
            {
                this.tweetTask = null;
            }
        }

        #region DragDrop

        enum DragDropState
        {
            None,
            MouseDown,
        }

        private DragDropState dragState;
        private Point dragStartPoint;

        private void listview_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var fwe = e.OriginalSource as FrameworkElement;
            if (fwe == null)
            {
                return;
            }

            if (!(fwe.DataContext is ImageViewModel))
            {
                return;
            }

            this.dragState = DragDropState.MouseDown;
            this.dragStartPoint = e.GetPosition(this);
        }

        private void listview_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Released)
            {
                return;
            }

            switch (this.dragState)
            {
                case DragDropState.MouseDown:
                    if ((this.dragStartPoint - e.GetPosition(this)).Length < 5)
                    {
                        return;
                    }

                    StartDrag();
                    break;
                default:
                    break;
            }
        }

        private void StartDrag()
        {
            var tempFiles = new System.Collections.Specialized.StringCollection();
            try
            {
                var tempDir = System.IO.Path.GetTempPath();
                foreach (var imageVM in this.viewModel.SelectedImages)
                {
                    string tempPath = string.Empty;
                    var found = false;
                    var name = imageVM.DateTime.ToString("yyyy_MM_dd_hh_mm_ss");
                    for (var i = 0; i < 100; i++)
                    {
                        if (i > 0)
                        {
                            tempPath = tempDir + name + $"({i + 1})" + ".png";
                        }
                        else
                        {
                            tempPath = tempDir + name + ".png";
                        }

                        if (!System.IO.File.Exists(tempPath))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        this.dragState = DragDropState.None;
                        return;
                    }

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(imageVM.Image));

                    using (var file = System.IO.File.OpenWrite(tempPath))
                    {
                        encoder.Save(file);
                    }
                    tempFiles.Add(tempPath);
                }

                var data = new DataObject();
                data.SetFileDropList(tempFiles);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Copy);
            }
            catch
            {
                this.dragState = DragDropState.None; ;
                return;
            }
            finally
            {
                foreach (var tempFile in tempFiles)
                {
                    try
                    {
                        if (System.IO.File.Exists(tempFile))
                        {
                            System.IO.File.Delete(tempFile);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void listview_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.dragState = DragDropState.None;
        }

        #endregion

        private void Copy_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.viewModel.LastSelectedItem != null;
        }

        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.viewModel.LastSelectedItem == null)
            {
                return;
            }

            Clipboard.SetImage(this.viewModel.LastSelectedItem.Image);
        }
    }
}
