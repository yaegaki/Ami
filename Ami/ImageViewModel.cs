using System;
using System.Windows.Media.Imaging;

namespace Ami
{
    public class ImageViewModel
    {
        /// <summary>
        /// 画像
        /// </summary>
        public BitmapSource Image { get; }
        /// <summary>
        /// イメージを作成した日付
        /// </summary>
        public DateTime DateTime { get; }

        public ImageViewModel(BitmapSource image, DateTime dateTime)
        {
            this.Image = image;
            this.DateTime = dateTime;
        }
    }
}
