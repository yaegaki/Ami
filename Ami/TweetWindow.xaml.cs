using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Ami
{
    /// <summary>
    /// TweetWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TweetWindow : Window
    {
        private CoreTweet.Tokens tokens;
        private string text;
        private IReadOnlyList<BitmapSource> images;

        public TweetWindow(CoreTweet.Tokens tokens, string text, IReadOnlyList<BitmapSource> images)
        {
            InitializeComponent();

            this.textblock.Text = text;
            this.listview.ItemsSource = images;

            this.tokens = tokens;
            this.text = text;
            this.images = images;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Tweet();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Tweet();
                    break;
                case Key.Escape:
                    this.DialogResult = false;
                    break;
                default:
                    break;
            }
        }

        private void Tweet()
        {
            try
            {
                var media = this.images.Select(image =>
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    using (var ms = new System.IO.MemoryStream())
                    {
                        encoder.Save(ms);
                        ms.Seek(0, System.IO.SeekOrigin.Begin);

                        return tokens.Media.Upload(media: ms).MediaId;
                    }
                });

                tokens.Statuses.Update(status: this.text, media_ids: media);

                // Tweet終わったら閉じる
                this.DialogResult = true;
            }
            catch
            {
                MessageBox.Show("失敗しました");
            }
        }
    }
}
