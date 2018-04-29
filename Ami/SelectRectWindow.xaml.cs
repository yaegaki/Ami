using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Ami
{
    /// <summary>
    /// SelectRectWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectRectWindow : Window
    {
        public ImageSource DesktopImage
        {
            set => this.image.Source = value;
        }
        public Rect ResultRect { get; private set; }

        public SelectRectWindow()
        {
            InitializeComponent();
        }

        Point anchorPos;
        bool mousedown;

        private void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.mousedown = true;
            this.anchorPos = e.GetPosition(this);
            this.grid.CaptureMouse();
            Canvas.SetLeft(this.grid, this.anchorPos.X);
            Canvas.SetTop(this.grid, this.anchorPos.Y);
            this.grid.Width = 0;
            this.grid.Height = 0;
        }

        private void Grid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.mousedown = false;
            this.grid.ReleaseMouseCapture();

            this.ResultRect = CalcRect(e.GetPosition(this));
            this.DialogResult = true;
        }

        private void Grid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!this.mousedown)
            {
                return;
            }

            var rect = CalcRect(e.GetPosition(this));

            Canvas.SetLeft(this.grid, rect.Left);
            Canvas.SetTop(this.grid, rect.Top);
            this.grid.Width = rect.Width;
            this.grid.Height = rect.Height;
        }

        private Rect CalcRect(Point mousePos)
        {
            double left, top, width, height;

            if (mousePos.X < this.anchorPos.X)
            {
                left = mousePos.X;
            }
            else
            {
                left = this.anchorPos.X;
            }
            width = Math.Abs(this.anchorPos.X - mousePos.X);

            if (mousePos.Y < this.anchorPos.Y)
            {
                top = mousePos.Y;
            }
            else
            {
                top = this.anchorPos.Y;
            }
            height = Math.Abs(this.anchorPos.Y - mousePos.Y);

            return new Rect(left, top, width, height);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
        }
    }
}
