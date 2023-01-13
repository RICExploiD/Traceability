using System;
using System.Configuration;

namespace Traceability.Services
{
    using Models;
    using Models.Connections;

    static class AppSettings
    {
        public static ConnectionSettingsSick ProductSickScanner { get { return GetProductSickScannerConnectionConfiguration(); } }
        public static ConnectionSettingsCom ProductCOMScanner { get { return GetProductCOMScannerConnectionConfiguration(); } }
        public static ConnectionSettingsSick ComponentSickScanner { get { return GetComponentSickScannerConnectionConfiguration(); } }
        public static ConnectionSettingsCom ComponentCOMScanner { get { return GetComponentCOMScannerConnectionConfiguration(); } }
        public static ScanType ProductScanType { get { return GetProductScanType(); } }
        public static ScanType ComponentScanType { get { return GetComponentScanType(); } }
        public static ScanType ExtraScanType { get { return GetExtraScanType(); } }
        public static OPCType OPCType { get { return GetOPCType(); } }
        public static string PointName 
        {
            get 
            {
                try
                {
                    return GetOption("PointName");
                }
                catch (ConfigArgumentNullException)
                {
                    return Sql.GetLocationNameByStation(WorkPageCore.CurrentPointNumber);
                }
            }
        }
        static public void UpdateSetting(string key, string value)
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[key].Value = value;
            configuration.Save();

            ConfigurationManager.RefreshSection("appSettings");
        }
        static public string GetOption(string option)
        {
            var result = ConfigurationManager.AppSettings[option];
            if (string.IsNullOrEmpty(result)) throw new ConfigArgumentNullException(option);
            return result;
        }

        static private OPCType GetOPCType()
        {
            Enum.TryParse(ConfigurationManager.AppSettings["OPCType"], out OPCType OPCType);
            return OPCType;
        }
        static private ScanType GetProductScanType()
        {
            Enum.TryParse(ConfigurationManager.AppSettings["ProductScanType"], out ScanType scanType);
            return scanType;
        }
        static private ScanType GetComponentScanType()
        {
            Enum.TryParse(ConfigurationManager.AppSettings["ComponentScanType"], out ScanType scanType);
            return scanType;
        }
        static private ScanType GetExtraScanType()
        {
            Enum.TryParse(ConfigurationManager.AppSettings["ExtraScanType"], out ScanType scanType);
            return scanType;
        }
        static private ConnectionSettingsSick GetProductSickScannerConnectionConfiguration()
        {
            return new ConnectionSettingsSick()
            {
                Ip = ConfigurationManager.AppSettings["ProductSickIP"],
                Port = ConfigurationManager.AppSettings["ProductSickPort"]
            };
        }
        static public ConnectionSettingsCom GetProductCOMScannerConnectionConfiguration()
        {
            return new ConnectionSettingsCom()
            {
                COM = ConfigurationManager.AppSettings["ProductComPort"],
                Speed = ConfigurationManager.AppSettings["ProductBaudRate"],
                Parity = ConfigurationManager.AppSettings["ProductParity"],
                DataBits = ConfigurationManager.AppSettings["ProductDataBits"],
                StopBits = ConfigurationManager.AppSettings["ProductStopBits"]
            };
        }
        static public ConnectionSettingsSick GetComponentSickScannerConnectionConfiguration()
        {
            return new ConnectionSettingsSick()
            {
                Ip = ConfigurationManager.AppSettings["ComponentSickIP"],
                Port = ConfigurationManager.AppSettings["ComponentSickPort"]
            };
        }
        static public ConnectionSettingsCom GetComponentCOMScannerConnectionConfiguration()
        {
            return new ConnectionSettingsCom()
            {
                COM = ConfigurationManager.AppSettings["ComponentComPort"],
                Speed = ConfigurationManager.AppSettings["ComponentBaudRate"],
                Parity = ConfigurationManager.AppSettings["ComponentParity"],
                DataBits = ConfigurationManager.AppSettings["ComponentDataBits"],
                StopBits = ConfigurationManager.AppSettings["ComponentStopBits"]
            };
        }

        static public void UpdateProductSickScannerConnectionConfiguration(ConnectionSettingsSick settings)
        {
            if (settings == null) return;

            UpdateSetting("ProductSickIP", settings.Ip);
            UpdateSetting("ProductSickPort", settings.Port);

            UpdateSetting("ProductScanType", ScanType.SickIP.ToString());
        }
        static public void UpdateProductCOMScannerConnectionConfiguration(ConnectionSettingsCom settings)
        {
            if (settings == null) return;

            settings.COM = settings.COM.Contains("COM") ? settings.COM : $"COM{settings.COM}";

            UpdateSetting("ProductComPort", settings.COM);
            UpdateSetting("ProductBaudRate", settings.Speed);
            UpdateSetting("ProductParity", settings.Parity);
            UpdateSetting("ProductDataBits", settings.DataBits);
            UpdateSetting("ProductStopBits", settings.StopBits);

            UpdateSetting("ProductScanType", ScanType.COM.ToString());
        }

        static public void UpdateComponentSickScannerConnectionConfiguration(ConnectionSettingsSick settings)
        {
            if (settings == null) return;

            UpdateSetting("ComponentSickIP", settings.Ip);
            UpdateSetting("ComponentSickPort", settings.Port);
        }
        static public void UpdateComponentCOMScannerConnectionConfiguration(ConnectionSettingsCom settings)
        {
            if (settings == null) return;

            settings.COM = settings.COM.Contains("COM") ? settings.COM : $"COM{settings.COM}";

            UpdateSetting("ComponentComPort", settings.COM);
            UpdateSetting("ComponentBaudRate", settings.Speed);
            UpdateSetting("ComponentParity", settings.Parity);
            UpdateSetting("ComponentDataBits", settings.DataBits);
            UpdateSetting("ComponentStopBits", settings.StopBits);
        }
    }
}
