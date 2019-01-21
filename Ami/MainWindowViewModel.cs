using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Ami
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// キャプチャしたイメージ
        /// </summary>
        public ObservableCollection<ImageViewModel> Images { get; } = new ObservableCollection<ImageViewModel>();
        /// <summary>
        /// 現在選択中のイメージ
        /// </summary>
        public ObservableCollection<ImageViewModel> SelectedImages { get; } = new ObservableCollection<ImageViewModel>();

        /// <summary>
        /// 最後に選択したイメージ
        /// </summary>
        private ImageViewModel lastSelectedItem;
        public ImageViewModel LastSelectedItem
        {
            get => lastSelectedItem;
            set
            {
                if (lastSelectedItem == value)
                {
                    return;
                }

                lastSelectedItem = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastSelectedItem)));
            }
        }

        private string text = string.Empty;
        /// <summary>
        /// Tweetするテキスト
        /// </summary>
        public string Text
        {
            get => text;
            set
            {
                if (text == value)
                {
                    return;
                }

                text = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
            }
        }

        private string hashTag;
        public string HashTag
        {
            get => hashTag;
            set
            {
                if (hashTag == value)
                {
                    return;
                }

                hashTag = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HashTag)));
            }
        }

        private int maxImageCount = 600;
        /// <summary>
        /// イメージをためておく最大数
        /// </summary>
        public int MaxImageCount
        {
            get => maxImageCount;
            set
            {
                maxImageCount = value;
                CollectImages();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// キャプチャする領域
        /// Emptyの場合は画面全体をキャプチャする
        /// </summary>
        private Rect captureRect = Rect.Empty;

        private SettingService settingService;

        public MainWindowViewModel(SettingService settingService)
        {
            this.settingService = settingService;
        }

        /// <summary>
        /// キャプチャ領域を選択する
        /// </summary>
        public void SelectRect()
        {
            var window = new SelectRectWindow();

            var desktopRect = GetDesktopRect();
            // 背景画像は表示した瞬間のキャプチャ画像にする
            var desktopImage = CaptureDesktop(FixDPI(desktopRect));
            window.DesktopImage = desktopImage;
            // 画面全体に広げる
            window.Left = 0;
            window.Top = 0;
            window.Width = desktopRect.Width;
            window.Height = desktopRect.Height;

            var res = window.ShowDialog();
            // もし選択されなかった場合は現状から変更しない
            if (res == null || !res.Value)
            {
                return;
            }

            if (window.ResultRect.IsEmpty)
            {
                return;
            }

            var rect = FixDPI(window.ResultRect);
            // 最低サイズ
            const double min = 5.0;
            if (rect.Width < min || rect.Height < min)
            {
                return;
            }

            this.captureRect = rect;
            var cropped = new CroppedBitmap(desktopImage, new Int32Rect((int)this.captureRect.X, (int)this.captureRect.Y, (int)this.captureRect.Width, (int)this.captureRect.Height));
            AddImage(cropped);
        }

        /// <summary>
        /// 現在の設定されたRectをもとにデスクトップをキャプチャする
        /// </summary>
        public void Capture()
        {
            BitmapSource image;
            try
            {
                image = CaptureDesktop(this.captureRect);
            }
            catch (OutOfMemoryException)
            {
                // 応急処置
                MessageBox.Show("メモリを確保できませんでした。動作が不安定になる可能性があります。");
                return;
            }

            AddImage(image);
        }

        /// <summary>
        /// イメージを削除する
        /// ついでにGCも行う
        /// </summary>
        public void ClearImages()
        {
            this.Images.Clear();
            this.SelectedImages.Clear();
            this.LastSelectedItem = null;
            GC.Collect();
        }

        public void TwitterAuthorize()
        {
            var session = CoreTweet.OAuth.Authorize(SettingService.ConsumerKey, SettingService.ConsumerSecret);
            try
            {
                // ブラウザで開く
                System.Diagnostics.Process.Start(session.AuthorizeUri.AbsoluteUri);
            }
            catch
            {
                // 開けない場合はどうしようもない
            }

            var window = new TwitterAutorizeWindow(session);
            window.Owner = Application.Current.MainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var res = window.ShowDialog();
            if (res == null || !res.Value)
            {
                // 失敗もしくはキャンセル
                return;
            }

            var token = window.ResultToken;
            // 設定を保存する
            this.settingService.UpdateOAuthToken(token.AccessToken, token.AccessTokenSecret);
            // トークンはすぐに保存
            this.settingService.Save();
        }

        public async Task TweetAsync()
        {
            if (string.IsNullOrEmpty(this.settingService.AccessToken) || string.IsNullOrEmpty(this.settingService.AccessTokenSecret))
            {
                MessageBox.Show("Twitterアカウント登録を行ってください");
                return;
            }

            CoreTweet.Tokens token;
            var cursor = Mouse.OverrideCursor;
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                token = await Task.Run(() =>
                {
                    return CoreTweet.Tokens.Create(SettingService.ConsumerKey,
                        SettingService.ConsumerSecret,
                        this.settingService.AccessToken,
                        this.settingService.AccessTokenSecret);
                });
            }
            catch
            {
                // OAuthやり直したほうがいいかもしれない
                MessageBox.Show("Twitterアカウントでエラーが発生しました");
                return;
            }
            finally
            {
                Mouse.OverrideCursor = cursor;
            }

            if (this.SelectedImages.Count > 4)
            {
                MessageBox.Show("Tweetできる画像は4枚までです");
                return;
            }

            IReadOnlyList<BitmapSource> tweetImages;
            if (this.SelectedImages.Count == 0)
            {
                if (this.Images.Count == 0)
                {
                    return;
                }

                tweetImages = new[] { this.Images.First().Image };
            }
            else
            {
                tweetImages = this.SelectedImages.Select(i => i.Image).ToArray();
            }

            string tweetText;
            var hashTags = new List<string>();
            if (!string.IsNullOrWhiteSpace(this.HashTag))
            {
                foreach (var t in this.HashTag.Split(' '))
                {
                    var tag = t.Trim();
                    if (tag.Length == 0)
                    {
                        continue;
                    }

                    // #で始まってない場合はつける
                    if (tag[0] != '#')
                    {
                        tag = '#' + tag;
                    }
                    else if (tag.Length == 1)
                    {
                        // #しかないやつは無視
                        continue;
                    }

                    hashTags.Add(tag);
                }
            }

            if (hashTags.Count == 0)
            {
                tweetText = this.Text;
            }
            else
            {
                var hashTagStr = string.Join(" ", hashTags);

                if (string.IsNullOrWhiteSpace(this.Text))
                {
                    tweetText = hashTagStr;
                }
                else
                {
                    tweetText = this.Text.Trim() + "\n" + hashTagStr;
                }
            }

            var window = new TweetWindow(token, tweetText, tweetImages);
            window.Owner = Application.Current.MainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var res = window.ShowDialog();

            // tweetした場合のみテキストを消す
            if (res != null && res.Value)
            {
                this.Text = string.Empty;
                // ハッシュタグは消さない
                return;
            }
        }

        /// <summary>
        /// 画像を蓄積する
        /// </summary>
        private void AddImage(BitmapSource image)
        {
            var imageVM = new ImageViewModel(image, DateTime.Now);
            this.Images.Insert(0, imageVM);
            CollectImages();

            // 追加したものを選択画像にする
            this.SelectedImages.Clear();
            this.SelectedImages.Add(imageVM);
            this.LastSelectedItem = imageVM;
        }

        /// <summary>
        /// 最大数より多い場合は画像を消す
        /// </summary>
        private void CollectImages()
        {
            while (this.Images.Count > this.MaxImageCount)
            {
                this.Images.RemoveAt(this.Images.Count - 1);
            }
        }

        /// <summary>
        /// デスクトップのキャプチャを作成する
        /// </summary>
        /// <param name="rect">キャプチャする領域。Emptyの場合は全体をキャプチャする。</param>
        /// <returns></returns>
        private BitmapSource CaptureDesktop(Rect rect)
        {
            var targetRect = rect.IsEmpty ? FixDPI(GetDesktopRect()) : rect;

            // デスクトップ全体のDCを取得
            var screenDC = Win32Helper.CreateDC("DISPLAY", null, null, IntPtr.Zero);
            var memoryDC = Win32Helper.CreateCompatibleDC(screenDC);

            var bitmap = Win32Helper.CreateCompatibleBitmap(screenDC, (int)targetRect.Width, (int)targetRect.Height);
            // memoryDCに最初からセットされたものを交換する
            var old = Win32Helper.SelectObject(memoryDC, bitmap);

            Win32Helper.BitBlt(memoryDC, 0, 0, (int)targetRect.Width, (int)targetRect.Height, screenDC, (int)targetRect.Left, (int)targetRect.Top, Win32Helper.SRCCOPY);
            var result = Imaging.CreateBitmapSourceFromHBitmap(bitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            // 元に戻す(元に戻さないとbitmapを削除できない)
            Win32Helper.SelectObject(memoryDC, old);

            Win32Helper.DeleteObject(bitmap);
            Win32Helper.DeleteDC(memoryDC);
            Win32Helper.DeleteDC(screenDC);

            return result;
        }

        /// <summary>
        /// デスクトップ全体のRectを取得する
        /// </summary>
        /// <returns></returns>
        private Rect GetDesktopRect()
        {
            var w = SystemParameters.PrimaryScreenWidth;
            var h = SystemParameters.PrimaryScreenHeight;

            return new Rect(0, 0, w, h);
        }

        /// <summary>
        /// 論理ピクセルから物理ピクセルに変換する
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        private Rect FixDPI(Rect rect)
        {
            var source = PresentationSource.FromVisual(Application.Current.MainWindow);

            // DPIの情報をとれない場合仕方ないのでそのまま返す
            if (source == null && source.CompositionTarget == null)
            {
                return rect;
            }

            var left = rect.X / source.CompositionTarget.TransformFromDevice.M11;
            var top = rect.Y / source.CompositionTarget.TransformFromDevice.M22;
            var width = rect.Width / source.CompositionTarget.TransformFromDevice.M11;
            var height = rect.Height / source.CompositionTarget.TransformFromDevice.M22;
            return new Rect(left, top, width, height);
        }
    }
}
