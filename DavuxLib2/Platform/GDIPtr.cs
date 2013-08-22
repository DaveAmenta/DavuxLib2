using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DavuxLib2.Platform
{
    public class GDIPtr : IDisposable
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        public IntPtr Ptr { get; private set; }

        public GDIPtr(IntPtr Ptr)
        {
            this.Ptr = Ptr;
        }

        public void Dispose()
        {
            DeleteObject(Ptr);
        }
    }
}
