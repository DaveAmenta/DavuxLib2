using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DavuxLib2.Platform
{
    public class DwmApi
    {
        [DllImport("dwmapi.dll")]
        public static extern int DwmDefWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, out IntPtr result);
        public static bool DwmDefWindowProc(ref System.Windows.Forms.Message m)
        {
            IntPtr result;
            int dwmHandled = DwmDefWindowProc(m.HWnd, m.Msg, m.WParam, m.LParam, out result);

            if (dwmHandled == 1)
            {
                m.Result = result;
                return true;
            }
            return false;
        }

        [DllImport("dwmapi.dll")]
        private static extern void DwmIsCompositionEnabled(ref bool pfEnabled);
        public static bool DwmIsCompositionEnabled()
        {
            bool isGlassSupported = false;
            if (Environment.OSVersion.Version.Major >= 6)
            {
                DwmIsCompositionEnabled(ref isGlassSupported);
            }
            return isGlassSupported;
        }

        [DllImport("dwmapi.dll")]
        public static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMargins);
        public static bool DwmExtendFrameIntoClientArea(Window window, Thickness margin)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException("The Window must be shown before extending glass.");

            if (!DwmIsCompositionEnabled())
            {
                window.Background = Brushes.White;
                HwndSource.FromHwnd(hwnd).CompositionTarget.BackgroundColor = Colors.White;
                return false;
            }

            // Set the background to transparent from both the WPF and Win32 perspectives
            window.Background = Brushes.Transparent;
            HwndSource.FromHwnd(hwnd).CompositionTarget.BackgroundColor = Colors.Transparent;

            MARGINS m = new MARGINS();
            m.topHeight = (int)margin.Top;
            m.leftWidth = (int)margin.Left;
            m.rightWidth = (int)margin.Right;
            m.bottomHeight = (int)margin.Bottom;
            DwmExtendFrameIntoClientArea(hwnd, ref m);
            return true;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }
    }
}
