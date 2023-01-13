using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace DisplayManager
{
    internal static class Monitors
    {
        private static List<Screen> Screens = null;
        private static Screen ChoosenScreen = null;
        private static Window ChoosenWindow = null;

        public static int SendBackTime = 5;
        internal static List<Screen> GetScreens()
        {
            Screens = new List<Screen>();

            var handler = new NativeMethods
                .DisplayDevicesMethods
                .EnumMonitorsDelegate(MonitorEnumProc);

            NativeMethods.DisplayDevicesMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, handler, IntPtr.Zero); // should be sequential

            return Screens;
        }
        private static void SendWindowBackToSavedScreen(object s, EventArgs e)
        {
            if (ChoosenWindow.Left == ChoosenScreen.TopX
                && ChoosenWindow.Top == ChoosenScreen.TopY) return;

            Task.Run(() =>
            {
                Task.Delay(SendBackTime * 1000).Wait();
                SetWindowToScreen(ChoosenWindow, ChoosenScreen);
            });
        }
        public static void SetWindowToScreen(this Window window, 
            Screen screen,  bool sendback = false)
        {
            ChoosenScreen = screen;
            ChoosenWindow = window;

            if (sendback) window.MouseLeave += SendWindowBackToSavedScreen;

            window.Dispatcher.Invoke(() =>
            {
                window.WindowState = WindowState.Normal;

                window.Left = screen.TopX;
                window.Top = screen.TopY;

                window.WindowState = WindowState.Maximized;
            });

        }

        private static bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, NativeMethods.DisplayDevicesMethods.RECT rect, IntPtr dwData)
        {
            NativeMethods.DisplayDevicesMethods.MONITORINFO mi =
                new NativeMethods.DisplayDevicesMethods.MONITORINFO();

            if (NativeMethods.DisplayDevicesMethods.GetMonitorInfo(hMonitor, mi))
            {
                Screens.Add
                    (   new Screen
                    ((  mi.dwFlags & 1
                    )   == 1 // 1 = primary monitor
                    ,   mi.rcMonitor.Left
                    ,   mi.rcMonitor.Top
                    ,   Math.Abs(mi.rcMonitor.Right - mi.rcMonitor.Left)
                    ,   Math.Abs(mi.rcMonitor.Bottom - mi.rcMonitor.Top)
                    ));
            }

            return true;
        }
    }

}
