using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Diagnostics;
using System.Configuration;
using System.Windows.Media.Imaging;

namespace Traceability
{
    using Traceability.Services;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private void SetApplictaionTitle(ComponentBarcodeType barcodeType)
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            var buildTime = new FileInfo(Process.GetCurrentProcess().MainModule.FileName).LastWriteTime;

            var pointNo = string.Empty;
            try
            { 
                pointNo = AppSettings.GetOption("Point"); 
            }
            catch (Models.ConfigArgumentNullException)
            { 
                pointNo = "000"; 
            }

            var pointName = AppSettings.PointName;

            Title = $"{processName} " +
                $"v3.2.1 " +
                $"OPC UA " +
                $"Point {pointNo}: {pointName} " +
                $"(last build {buildTime})";

            gStationNameLabel.Content = pointName;
        }
        private void EnterToNavigationWorkPage(ObeserverPattern.IWorkPageObservable page)
        {
            gTraceabilityComponentFrame.NavigationService.Navigate(page as Page);
        }
        public void ClearTemporaryData()
        {
            Dispatcher.InvokeAsync(() =>
            {
                gProductBarcodeLabel.Text = "---";
                gProductModelBrandLabel.Text = "---";
                gIOTTypeLabel.Content = string.Empty;
                gCanGoPermitLabel.Background = Brushes.Gray;
            });
        }
        private void ClearWMDetails()
        {
            Dispatcher.InvokeAsync(() =>
            {
                gRDummyLabel.Text = "---";
                gLDummyLabel.Text = "---";
                gProductNoLabel.Text = "---";
            });
        }
        private void SetSettingsButton(ProductionLine line)
        {
            Dispatcher.InvokeAsync(() =>
            {
                switch (line)
                {
                    case ProductionLine.WashingMachine: 
                        gSettingsButtonImage.Source = new BitmapImage(new Uri(@"/Images/wm.png", UriKind.Relative)); break;
                    case ProductionLine.Refrigerator:
                        gSettingsButtonImage.Source = new BitmapImage(new Uri(@"/Images/ref.png", UriKind.Relative)); break;
                    default: 
                        gSettingsButtonImage.Source = new BitmapImage(new Uri(@"/Images/GearSettingsButton.png", UriKind.Relative)); break;
                }
            });
        }
        public void UpdateConfigUI()
        {
            var showDebugForm = bool.Parse(ConfigurationManager.AppSettings["ShowDebugForm"]);
            OptionalDebugColumnDefinition.Width = new GridLength(showDebugForm ? .35 : 0, GridUnitType.Star);
            OptionalDebugColumnDefinition.MinWidth = showDebugForm ? 330 : 0;
            gGridSplitter.Visibility = showDebugForm ? Visibility.Visible : Visibility.Collapsed;

            var showLogs = bool.Parse(ConfigurationManager.AppSettings["ShowLogs"]);
            LogsRowDefinition.Height = new GridLength(showLogs ? 5 : 0, GridUnitType.Star);

            var pointName = AppSettings.PointName;

            gStationNameLabel.Content = pointName;
            ReloadWorkPage();
        }
        private void StartClock()
        {
            Dispatcher.InvokeAsync(() =>
            {
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = new TimeSpan(0, 0, 1),
                    IsEnabled = true
                };
                timer.Tick += (o, e) => { gCurrentTime.Content = DateTime.Now.ToString("HH:mm:ss"); };
                timer.Start();
            });
        }
        private void SetVisivilityWMDetails(bool isShown = true)
        {
            Dispatcher.InvokeAsync(() =>
            {
                //WMDetialsColumnDefinition.Width = new GridLength(isShown ? .20 : 0, GridUnitType.Star);
                gWMDetialsGrid.Visibility = isShown ? Visibility.Visible: Visibility.Collapsed;
                Grid.SetColumnSpan(gProductBarcodeGrid, isShown ? 1 : 2);
                Grid.SetColumnSpan(gProductModelBrandGrid, isShown ? 1 : 2);
            });
        }
        private void UpdateRDummyTextBox(string dummy)
        {
            Dispatcher.InvokeAsync(() => { gRDummyLabel.Text = dummy; });
        }
        private void UpdateLDummyTextBox(string dummy)
        {
            Dispatcher.InvokeAsync(() => { gLDummyLabel.Text = dummy; });
        }
        private void UpdateProductNoTextBox(string productNo)
        {
            Dispatcher.InvokeAsync(() => { gProductNoLabel.Text = productNo; });
        }

        private void UpdateProductBarcodeLabel(string barcode)
        {
            Dispatcher.InvokeAsync(() =>
            { 
                gProductBarcodeLabel.Text = barcode.Trim();
            });
        }
        private void UpdateProductRowBarcodeLabel(string barcode)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gProductRowBarcodeLabel.Text = barcode;
            });
        }

        private void UpdateIsHereLabel(bool isHere = true)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gIsHereLabel.Background = isHere ? Brushes.Khaki : Brushes.Gray;
                if (!isHere) gProductBarcodeLabel.Text = "---";
            });
        }
        private void UpdateSendButtonLabel(bool isPressed = true)
        {
            Dispatcher.InvokeAsync(() => gSendButtonLabel.Background = isPressed ? Brushes.Khaki : Brushes.Gray);
        }
        private void EventBarcodeOfProductConnectionChanged(bool isConnected)
        {
            Dispatcher.InvokeAsync(() => 
                gProductScannerConnectionStatusLabel.Foreground = isConnected ? Brushes.Green : Brushes.Red);
        }

        private void SqlOnConnectionChangeLabel(bool isConnected)
        {
            Dispatcher.InvokeAsync(() =>
                gSQLConnectionStatusLabel.Foreground = isConnected ? Brushes.Green : Brushes.Red);
        }

        private void MyOPC_OnConnectionChange(bool isConnected)
        {
            Dispatcher.InvokeAsync(() =>
                gOPCConnectionStatusLabel.Foreground = isConnected ? Brushes.Green : Brushes.Red);
        }
        public void ObserveScannedComponent(string ScannedComponent)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gComponentRowBarcodeLabel.Text = ScannedComponent;
            });
        }
        public void ObserveOPCCangoPermitLabel(bool cangoPermit)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gCanGoPermitLabel.Background = cangoPermit ? Brushes.Green : Brushes.Gray;
            });
        }
        public void ObserveCOMConnection(bool isConnected)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gComponentScannerConnectionStatusLabel.Foreground = isConnected ? Brushes.Green : Brushes.Red;
                gComponentScannerConnectionStatusLabel.Content = "Component COM";
            });
        }
        public void ObserveSickConnection(bool isConnected)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gComponentScannerConnectionStatusLabel.Foreground = isConnected ? Brushes.Green : Brushes.Red;
                gComponentScannerConnectionStatusLabel.Content = "Component Sick";
            });
        }

        public void UpdateIOTTypeLabel(string iotType)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gIOTTypeLabel.Content = iotType;
            });
        }
        public void UpdateModelBrandLabel(string brandAndMarketingCode)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gProductModelBrandLabel.Text = brandAndMarketingCode;
            });
        }

        private void ShowLog(string msg)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gLogTextBox.Text = $"{msg}\r\n{gLogTextBox.Text}";
                if (System.Text.RegularExpressions.Regex.Matches(gLogTextBox.Text, "\r\n").Count > 100)
                    gLogTextBox.Text = gLogTextBox.Text.Remove(gLogTextBox.Text.LastIndexOf("\r\n"));
            });
        }
        public void ObserveLogAction(string logText)
        {
            myLog.WriteLog(logText);
        }
        public void OPCLogAction(string logText)
        {
            myLog.WriteLog($"OPC: {logText}");
        }

    }
}
