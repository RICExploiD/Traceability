using System;
using System.Linq;
using System.Windows;

namespace Traceability
{
    using Models.Connections;
    using Services;

    partial class SettingsWindow : Window
    {
        private void UpdateUIByConfig()
        {
            Dispatcher.InvokeAsync(() =>
            {
                UpdateUIBufferPart();

                try
                {
                    var isMTK = WorkPageCore.SaveType.Equals(ComponentBarcodeType.MTK);
                    if (isMTK)
                    {
                        gBufferSizeInputTextBox.Text = AppSettings.GetOption("BufferSize");

                        UpdateBufferData();
                    }

                    gProductionLineInputComboBox.ItemsSource = new[] { ProductionLine.Refrigerator, ProductionLine.WashingMachine };
                    gProductionLineInputComboBox.SelectedItem = WorkPageCore.Line;
                    gSaveTypeInputComboBox.ItemsSource = new[] { ComponentBarcodeType.Individual, ComponentBarcodeType.MTK };
                    gSaveTypeInputComboBox.SelectedItem = WorkPageCore.SaveType;
                    gOPCTypeInputComboBox.ItemsSource = new[] { OPCType.UA, OPCType.DA };
                    gOPCTypeInputComboBox.SelectedItem = AppSettings.OPCType;
                    
                    gComponentCodeInputTextBox.Text = Sql.GetComponentCodeByStation(WorkPageCore.CurrentPointNumber);

                    var pointNameConfig = AppSettings.PointName;

                    gPointNameTextBoxInput.Text = pointNameConfig;
                    gPointInputTextBox.Text = AppSettings.GetOption("Point");
                    gMRPCodeInputTextBox.Text = AppSettings.GetOption("MRP");
                    gOPCNodeInputTextBox.Text = AppSettings.GetOption("OPCNode");
                    gOPCProgIDInputTextBox.Text = AppSettings.GetOption("OPCProgID");
                    gOPCServerIPTextBox.Text = AppSettings.GetOption("OPCIPServer");
                    gOPCServerPortTextBox.Text = AppSettings.GetOption("OPCPort");

                    gLogsCheckBoxInput.IsChecked = bool.Parse(AppSettings.GetOption("ShowLogs"));
                    gDebugModeCheckBoxInput.IsChecked = bool.Parse(AppSettings.GetOption("ShowDebugForm"));
                    gAlwaysCanGoPermitInputCheckBox.IsChecked = bool.Parse(AppSettings.GetOption("AlwaysCanGoPermit"));
                    gSingleInstanceModeInputCheckBox.IsChecked = bool.Parse(AppSettings.GetOption("IsOnlyOneInstance"));
                    gSaveWithoutMatchVerificationInputCheckBox.IsChecked = bool.Parse(AppSettings.GetOption("SaveWithoutMatchVerification"));
                }
                catch (Models.ConfigArgumentNullException ex)
                {
                    ex.ShowErrorMessage();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}\n{ex.StackTrace}",
                        "Ошибка приложения", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // Varuable represent most required tag, that can be template to another ones 
                const string cfullpath_tag = "TagIsHere";
                var fullpath_splitted = new string[4];
                try
                {
                    fullpath_splitted = AppSettings.GetOption(cfullpath_tag).Split('.');

                    gOPCChannelInputTextBox.Text = fullpath_splitted[0];
                    gOPCDeviceInputTextBox.Text = fullpath_splitted[1];
                    gOPCTagGroupInputTextBox.Text = fullpath_splitted[2];
                    gOPCPresenseTagInputTextBox.Text = fullpath_splitted[3];

                    fullpath_splitted = AppSettings.GetOption("TagButtons").Split('.');
                    gOPCButtonTagInputTextBox.Text = fullpath_splitted[3];

                    fullpath_splitted = AppSettings.GetOption("TagCanGo").Split('.');
                    gOPCPermitionTagInputTextBox.Text = fullpath_splitted[3];
                }
                catch (Models.ConfigArgumentNullException ex) { ex.ShowErrorMessage(); }
                catch (IndexOutOfRangeException ex)
                {
                    MessageBox.Show($"В тэге {string.Join(".",fullpath_splitted)} - {ex.Message}\n" +
                        $"Вероятно количество найденых частей обрабатываемого тэга меньше четырех\n" +
                        $"Проверьте его значение в конфигурационном файле или измените значение в настройках",
                        "Ошибка конфигурации", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });
        }
        private void HideProductComConfig()
        {
            Dispatcher.Invoke(() =>
            {
                gProductScannerSickStackPanel.Visibility = Visibility.Visible;
                gProductScannerCOMStackPanel.Visibility = Visibility.Collapsed;
            });
        }
        private void HideProductSickConfig()
        {
            Dispatcher.Invoke(() =>
            {
                gProductScannerSickStackPanel.Visibility = Visibility.Collapsed;
                gProductScannerCOMStackPanel.Visibility = Visibility.Visible;
            });
        }
        private void HideComponentComConfig()
        {
            Dispatcher.Invoke(() =>
            {
                gComponentScannerCOMStackPanel.Visibility = Visibility.Collapsed;
                gComponentScannerSickStackPanel.Visibility = Visibility.Visible;
            });
        }
        private void HideComponentSickConfig()
        {
            Dispatcher.Invoke(() =>
            {
                gComponentScannerCOMStackPanel.Visibility = Visibility.Visible;
                gComponentScannerSickStackPanel.Visibility = Visibility.Collapsed;
            });
        }
        private void UpdateBufferData()
        {
            Dispatcher.InvokeAsync(() =>
            {
                gBufferTextBlock.Text = string.Join("\n", WorkPageCore.BufferData?
                    .Select(x => string.IsNullOrEmpty(x) ? "empty" : x));
            });
        }
        private void UpdateUIBufferPart()
        {
            Dispatcher.InvokeAsync(() =>
            {
                var isMTK = gSaveTypeInputComboBox.SelectedItem.Equals(ComponentBarcodeType.MTK);

                gBufferGroupBox.Visibility = isMTK ? Visibility.Visible : Visibility.Collapsed;
                gBufferSizeGrid.Visibility = isMTK ? Visibility.Visible : Visibility.Collapsed;
            });
        }
        private void SetProductSickScannerConnectionConfiguration(ConnectionSettingsSick settings)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gProductIPSickInputTextBox.Text = settings.Ip;
                gProductPortSickInputTextBox.Text = settings.Port;
            });
        }
        private void SetProductCOMScannerConnectionConfiguration(ConnectionSettingsCom settings)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gProductCOMCOMInputTextBox.Text = settings.COM;
                gProductSpeedCOMInputTextBox.Text = settings.Speed;
                gProductParityCOMInputTextBox.Text = settings.Parity;
                gProductDataBitsCOMInputTextBox.Text = settings.DataBits;
                gProductStopBitsCOMInputTextBox.Text = settings.StopBits;
            });
        }
        private void SetComponentSickScannerConnectionConfiguration(ConnectionSettingsSick settings)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gComponentIPSickInputTextBox.Text = settings.Ip;
                gComponentPortSickInputTextBox.Text = settings.Port;
            });
        }
        private void SetComponentCOMScannerConnectionConfiguration(ConnectionSettingsCom settings)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gComponentCOMCOMInputTextBox.Text = settings.COM;
                gComponentSpeedCOMInputTextBox.Text = settings.Speed;
                gComponentParityCOMInputTextBox.Text = settings.Parity;
                gComponentDataBitsCOMInputTextBox.Text = settings.DataBits;
                gComponentStopBitsCOMInputTextBox.Text = settings.StopBits;
            });
        }
        private void gResetLabelButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
                gPointNameTextBoxInput.Text = Sql.GetLocationNameByStation(WorkPageCore.CurrentPointNumber)
            );
        }
        private void TopWindowGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left) DragMove();
        }

        private void gCloseSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
