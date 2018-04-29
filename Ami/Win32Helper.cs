using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Ami
{
    static class Win32Helper
    {
        // public struct RECT
        // {
        //     public int left;
        //     public int top;
        //     public int right;
        //     public int bottom;

        //     public RECT()
        //     {
        //         this.left = 0;
        //         this.top = 0;
        //         this.right = 0;
        //         this.bottom = 0;
        //     }
        // }

        // public struct BITMAP
        // {
        //     public int bmType;
        //     public int bmWidth;
        //     public int bmHeight;
        //     public int bmWidthBytes;
        //     public ushort bmPlanes;
        //     public ushort bmBitsPixel;
        //     IntPtr bmBits;
        // }

        public static readonly uint SRCCOPY = 0x00CC0020;

        public delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lparam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public extern static bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lparam);

        // [DllImport("dwmapi.dll")]
        // public static extern long DwmGetWindowAttribute(IntPtr hWnd, uint dwAttribute, out RECT pvAttribute, uint cbAttribute);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd,
            StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateDC(string pwszDriver, string pwszDevice, string pszPort, IntPtr pdm);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        // [DllImport("gdi32.dll", SetLastError = true)]
        // public static extern int GetObjectW(IntPtr h, int c, out BITMAP pv);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern int BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, uint rop);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern int DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern int DeleteObject(IntPtr obj);
    }
}
