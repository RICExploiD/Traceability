using System.Windows;

namespace Traceability
{
    using Models.Connections;
    using Services;

    partial class SettingsWindow : Window
    {
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            this.Hide();
            InitCurrentConfig();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitCurrentConfig();
        }

        private void gBufferGroupBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UpdateBufferData();
        }

        private void gProductSickSwitchToCOMButton_Click(object sender, RoutedEventArgs e)
        {
            HideProductSickConfig();
            SetProductCOMScannerConnectionConfiguration(AppSettings.ProductCOMScanner);
        }
        private void gProductCOMSwitchToSickButton_Click(object sender, RoutedEventArgs e)
        {
            HideProductComConfig();
            SetProductSickScannerConnectionConfiguration(AppSettings.ProductSickScanner);
        }

        private void gProductScannerSickApplyButton_Click(object sender, RoutedEventArgs e)
        {
            Observer.ReconnectToProductScanner(ScanType.SickIP);
        }
        private void gProductScannerSickSaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new ConnectionSettingsSick()
            {
                Ip = gProductIPSickInputTextBox.Text,
                Port = gProductPortSickInputTextBox.Text
            };
            AppSettings.UpdateProductSickScannerConnectionConfiguration(settings);
        }
        private void gProductScannerCOMApplyButton_Click(object sender, RoutedEventArgs e)
        {
            Observer.ReconnectToProductScanner(ScanType.COM);
        }
        private void gProductScannerCOMSaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new ConnectionSettingsCom()
            {
                COM = gProductCOMCOMInputTextBox.Text,
                Speed = gProductSpeedCOMInputTextBox.Text,
                Parity = gProductParityCOMInputTextBox.Text,
                DataBits = gProductDataBitsCOMInputTextBox.Text,
                StopBits = gProductStopBitsCOMInputTextBox.Text
            };
            AppSettings.UpdateProductCOMScannerConnectionConfiguration(settings);
        }

        private void gComponentSickSwitchToCOMButton_Click(object sender, RoutedEventArgs e)
        {
            HideComponentSickConfig();
            SetComponentCOMScannerConnectionConfiguration(AppSettings.ComponentCOMScanner);
        }
        private void gComponentCOMSwitchToSickButton_Click(object sender, RoutedEventArgs e)
        {
            HideComponentComConfig();
            SetComponentSickScannerConnectionConfiguration(AppSettings.ComponentSickScanner);
        }

        private void gComponentScannerSickApplyButton_Click(object sender, RoutedEventArgs e)
        {
            Observer.ReconnectToComponentScanner(ScanType.SickIP);
        }
        private void gComponentScannerSickSaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new ConnectionSettingsSick()
            {
                Ip = gComponentIPSickInputTextBox.Text,
                Port = gComponentPortSickInputTextBox.Text
            };
            AppSettings.UpdateComponentSickScannerConnectionConfiguration(settings);
        }
        private void gComponentScannerCOMApplyButton_Click(object sender, RoutedEventArgs e)
        {
            Observer.ReconnectToComponentScanner(ScanType.COM);
        }
        private void gComponentScannerCOMSaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new ConnectionSettingsCom()
            {
                COM = gComponentCOMCOMInputTextBox.Text,
                Speed = gComponentSpeedCOMInputTextBox.Text,
                Parity = gComponentParityCOMInputTextBox.Text,
                DataBits = gComponentDataBitsCOMInputTextBox.Text,
                StopBits = gComponentStopBitsCOMInputTextBox.Text
            };
            AppSettings.UpdateComponentCOMScannerConnectionConfiguration(settings);
        }

        private void gOPCAppSettingsApplyButton_Click(object sender, RoutedEventArgs e)
        {
            Observer.ReconnectToOPCTags(ConnectionPLCType.OPC);
        }
        private void gOPCAppSettingsSaveButton_Click(object sender, RoutedEventArgs e)
        {
            var channel_device_group =
                  $"{gOPCChannelInputTextBox.Text}" +
                  $".{gOPCDeviceInputTextBox.Text}" +
                  $".{gOPCTagGroupInputTextBox.Text}";

            if (!string.IsNullOrEmpty(gOPCPresenseTagInputTextBox.Text))
                AppSettings.UpdateSetting("TagIsHere", $"{channel_device_group}.{gOPCPresenseTagInputTextBox.Text}");
            if (!string.IsNullOrEmpty(gOPCButtonTagInputTextBox.Text))
                AppSettings.UpdateSetting("TagButtons", $"{channel_device_group}.{gOPCButtonTagInputTextBox.Text}");
            if (!string.IsNullOrEmpty(gOPCPermitionTagInputTextBox.Text))
                AppSettings.UpdateSetting("TagCanGo", $"{channel_device_group}.{gOPCPermitionTagInputTextBox.Text}");

            AppSettings.UpdateSetting("OPCNode", gOPCNodeInputTextBox.Text);
            AppSettings.UpdateSetting("OPCProgID", gOPCProgIDInputTextBox.Text);
            AppSettings.UpdateSetting("OPCType", gOPCTypeInputComboBox.Text);
            AppSettings.UpdateSetting("OPCIPServer", gOPCServerIPTextBox.Text);
            AppSettings.UpdateSetting("OPCPort", gOPCServerPortTextBox.Text);
        }

        private void gAppSettingsSaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveAppSettings();
            //TODO
            Dispatcher.InvokeAsync(() =>
            {
                gComponentCodeInputTextBox.Text = Sql.GetComponentCodeByStation(WorkPageCore.CurrentPointNumber);
            });
        }
        private void gAppSettingsApplyButton_Click(object sender, RoutedEventArgs e)
        {
            Observer.UpdateConfigUI();
            WorkPageCore.InitConfigData();
            UpdateBufferData();
        }
        private void gSaveTypeInputComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateUIBufferPart();
        }

        private void SettingsTextBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            (e.Source as System.Windows.Controls.TextBox).IsReadOnly = false;
            (e.Source as System.Windows.Controls.TextBox).Foreground = System.Windows.Media.Brushes.Black;
        }

        private void SettingsTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            (e.Source as System.Windows.Controls.TextBox).IsReadOnly = true;
            (e.Source as System.Windows.Controls.TextBox).Foreground = System.Windows.Media.Brushes.Gray;
        }
    }
}
