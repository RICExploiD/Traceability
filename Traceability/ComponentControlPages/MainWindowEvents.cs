using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Traceability
{
    using Services;

    partial class MainWindow : Window
    {
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnterToLoginPage();
            CardReaderInit();
            ConnectToOpcTags(ConnectionPLCType.OPC, AppSettings.OPCType);
            ReconnectToProductScanner(AppSettings.ProductScanType);
            SetApplictaionTitle(ComponentBarcodeType.MTK);
            StartClock();
            UpdateConfigUI();
            SetVisivilityWMDetails(WorkPageCore.IsCurrentLineWM);
            UpdateSettingsButton(WorkPageCore.Line);
            Sql.StartSQLConnectionMonitoring();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            OperatorLogout();
            DisconnectFromProductScanner();
            WorkPageCore.DisconnectFromComponentScanner();
            Application.Current.Shutdown();
        }

        private void SqlOnConnectionChange(bool isConnected)
        {
            SqlOnConnectionChangeLabel(isConnected);

            myLog.WriteLog($"SQL соединение{(isConnected ? string.Empty : " не")} успешно");
        }

        private void gProductRowBarcodeViewBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Clipboard.SetText(gProductRowBarcodeLabel.Text.ToString());
                gProductRowBarcodeCopiedLabel.Visibility = Visibility.Visible;
            });

            Task.Run(() =>
            {
                Task.Delay(3000).Wait();
                Dispatcher.Invoke(() =>
                {
                    gProductRowBarcodeCopiedLabel.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void gComponentRowBarcodeViewBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Clipboard.SetText(gComponentRowBarcodeLabel.Text.ToString());
                gComponentRowBarcodeCopiedLabel.Visibility = Visibility.Visible;
            });

            Task.Run(() =>
            {
                Task.Delay(3000).Wait();
                Dispatcher.Invoke(() =>
                {
                    gComponentRowBarcodeCopiedLabel.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void gSettingsButtonImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            (SettingsWindow as SettingsWindow).Show();
            (SettingsWindow as SettingsWindow).Focus();

            if ((SettingsWindow as SettingsWindow).WindowState == WindowState.Minimized)
                (SettingsWindow as SettingsWindow).WindowState = WindowState.Normal;
        }

        private void gSettingsButtonImage_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void gSettingsButtonImage_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }
    }
}
