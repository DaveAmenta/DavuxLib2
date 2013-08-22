using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace DavuxLib2.Platform
{
    public class User32
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern void LockWorkStation();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr OpenInputDesktop(DesktopAccountFlags dwFlags, bool fInherit,
           DesktopDesiredAccess dwDesiredAccess);


        [Flags]
        public enum DesktopAccountFlags : uint
        {
            DF_ALLOWOTHERACCOUNTHOOK = 1,
        }

        [Flags]
        public enum DesktopDesiredAccess : uint
        {
            DESKTOP_CREATEMENU = 0x04,
            DESKTOP_CREATEWINDOW = 0x02,
            DESKTOP_ENUMERATE = 0x40,
            DESKTOP_HOOKCONTROL = 0x08,
            DESKTOP_READOBJECTS = 0x01,
            DESKTOP_WRITEOBJECTS = 0x80,
            DESKTOP_SWITCHDESKTOP = 0x100,
        }

        public static bool IsWorkStationsLocked()
        {

            IntPtr hDesktop = User32.OpenInputDesktop(User32.DesktopAccountFlags.DF_ALLOWOTHERACCOUNTHOOK,
                true, User32.DesktopDesiredAccess.DESKTOP_CREATEMENU | User32.DesktopDesiredAccess.DESKTOP_CREATEWINDOW
                | User32.DesktopDesiredAccess.DESKTOP_ENUMERATE | User32.DesktopDesiredAccess.DESKTOP_HOOKCONTROL
                | User32.DesktopDesiredAccess.DESKTOP_READOBJECTS | User32.DesktopDesiredAccess.DESKTOP_SWITCHDESKTOP
                | User32.DesktopDesiredAccess.DESKTOP_WRITEOBJECTS);
            return hDesktop == IntPtr.Zero;
        }
    }
}
