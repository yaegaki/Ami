using System.Windows;

namespace Ami
{
    /// <summary>
    /// TwitterAutorizeWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TwitterAutorizeWindow : Window
    {
        private CoreTweet.OAuth.OAuthSession session;
        public CoreTweet.Tokens ResultToken { get; private set; }

        public TwitterAutorizeWindow(CoreTweet.OAuth.OAuthSession session)
        {
            InitializeComponent();

            this.session = session;
        }

        private void OKClick(object sender, RoutedEventArgs e)
        {
            var text = this.pin.Text.Trim();
            if (text.Length == 0)
            {
                MessageBox.Show("Pinコードを入力してください");
                return;
            }

            try
            {
                this.ResultToken = CoreTweet.OAuth.GetTokens(session, text);
            }
            catch
            {
                MessageBox.Show("失敗しました");
                return;
            }

            this.DialogResult = true;
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
