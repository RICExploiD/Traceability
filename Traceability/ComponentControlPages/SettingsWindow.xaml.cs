namespace Traceability
{
    using Traceability.Services;

    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : System.Windows.Window, ObeserverPattern.ISettingsObservable
    {
        ObeserverPattern.ISettingsObserver Observer;
        public SettingsWindow() { InitializeComponent(); }
        public void InitObserver(ObeserverPattern.ISettingsObserver window) { Observer = window; }
        private void InitCurrentConfig()
        {
            gProductScannerGrid.Visibility = System.Windows.Visibility.Visible;
            gComponentScannerGrid.Visibility = System.Windows.Visibility.Visible;

            switch (AppSettings.ProductScanType)
            {
                case ScanType.SickIP:
                    SetProductSickScannerConnectionConfiguration(AppSettings.ProductSickScanner);
                    HideProductComConfig(); 
                    break;
                case ScanType.COM:
                    SetProductCOMScannerConnectionConfiguration(AppSettings.ProductCOMScanner);
                    HideProductSickConfig(); 
                    break;
            }
            switch (AppSettings.ComponentScanType)
            {
                case ScanType.SickIP:
                    SetComponentSickScannerConnectionConfiguration(AppSettings.ComponentSickScanner);
                    HideComponentComConfig();
                    break;
                case ScanType.COM:
                    SetComponentCOMScannerConnectionConfiguration(AppSettings.ComponentCOMScanner);
                    HideComponentSickConfig(); 
                    break;
            }

            UpdateUIByConfig(); 
        }
        private void SaveAppSettings()
        {
            AppSettings.UpdateSetting("PointName", gPointNameTextBoxInput.Text);
            AppSettings.UpdateSetting("Point", gPointInputTextBox.Text);
            AppSettings.UpdateSetting("IsOnlyOneInstance", gSingleInstanceModeInputCheckBox.IsChecked.ToString());

            AppSettings.UpdateSetting("ShowDebugForm", gDebugModeCheckBoxInput.IsChecked.ToString());
            AppSettings.UpdateSetting("ShowLogs", gLogsCheckBoxInput.IsChecked.ToString());
            
            AppSettings.UpdateSetting("SaveWithoutMatchVerification", gSaveWithoutMatchVerificationInputCheckBox.IsChecked.ToString());
            AppSettings.UpdateSetting("AlwaysCanGoPermit", gAlwaysCanGoPermitInputCheckBox.IsChecked.ToString());
            AppSettings.UpdateSetting("MRP", gMRPCodeInputTextBox.Text);

            AppSettings.UpdateSetting("BufferSize", gBufferSizeInputTextBox.Text);
            AppSettings.UpdateSetting("SaveType", gSaveTypeInputComboBox.SelectedItem.ToString());

            AppSettings.UpdateSetting("ProductionLine", gProductionLineInputComboBox.SelectedItem.ToString());
        }
    }
}
