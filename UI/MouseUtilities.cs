using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace WpfApp1.UI
{

    /// <summary>
    /// Provides access to the mouse location by calling unmanaged code.
    /// </summary>
    public class MouseUtilities
    {
        #region Methods
        /// <summary>
        ///     Returns the mouse cursor location.  This method is necessary during
        ///     a drag-drop operation because the WPF mechanisms for retrieving the
        ///     cursor coordinates are unreliable.
        /// </summary>
        /// <param name="relativeTo">The Visual to which the mouse coordinates will be relative.</param>
        public static Point GetMousePosition(Visual relativeTo)
        {
            Win32Point mouse = new Win32Point();
            GetCursorPos(ref mouse);
            return relativeTo.PointFromScreen(new Point(mouse.X, mouse.Y));
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(ref Win32Point pt);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hwnd, ref Win32Point pt);
        #endregion

        #region Nested type: Win32Point
        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public readonly Int32 X;
            public readonly Int32 Y;
        }
        #endregion
    }
}
