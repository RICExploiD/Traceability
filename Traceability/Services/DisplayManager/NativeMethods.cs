using System;
using System.Runtime.InteropServices;

namespace DisplayManager
{
    internal static class NativeMethods
    {
        /// <summary>
        /// Methods for retrieving display devices.
        /// </summary>
        internal static class DisplayDevicesMethods
        {
            internal delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, RECT rect, IntPtr dwData);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

            /// <summary>
            /// Retrieves information about a display monitor.
            /// </summary>
            /// <param name="hmonitor">A handle to the display monitor of interest.</param>
            /// <param name="info">A pointer to a MONITORINFO or MONITORINFOEX structure that receives information about the specified display monitor.</param>
            /// <returns>If the function succeeds, the return value is nonzero.</returns>
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetMonitorInfo(IntPtr hmonitor, [In, Out] NativeMethods.DisplayDevicesMethods.MONITORINFO info);

            /// <summary>
            /// The RECT structure defines the coordinates of the upper-left and lower-right corners of a rectangle.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            internal struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }

            /// <summary>
            /// The MONITORINFO structure contains information about a display monitor.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
            internal class MONITORINFO
            {
                internal int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                internal RECT rcMonitor = new RECT();
                internal RECT rcWork = new RECT();
                internal int dwFlags;
            }
        }
    }
}
