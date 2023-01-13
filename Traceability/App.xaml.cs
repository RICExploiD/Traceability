using System;
using System.Windows;
using System.Configuration;
using System.Diagnostics;

namespace Traceability
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void AppStartup(object sender, StartupEventArgs e)
        {
            try
            {
                if (bool.Parse(ConfigurationManager.AppSettings["IsOnlyOneInstance"]))
                {
                    // get the list of all processes by the "procName"       
                    string procName = Process.GetCurrentProcess().ProcessName;
                    Process[] processes = Process.GetProcessesByName(procName);
                    if (processes.Length > 1)
                    {
                        MessageBox.Show("Приложение " + procName + " уже запущено.\n" +
                            "Запущенное приложение будет закрыта.", "Application error", MessageBoxButton.OK);
                        Current.Shutdown();
                    }
                }

                var windowtest = new MainWindow();
                windowtest.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}\n{ex.StackTrace}", "Application error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }
    }
}
