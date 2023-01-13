using System.Windows;

namespace Traceability.MTKWorkPage
{
    using Services;
    partial class MTKWorkPage
    {
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WorkPageCore.ReconnectToComponentScanner(AppSettings.ComponentScanType);
            //OperatorNotifyProcessor();
            WorkPageCore.StartOperatorNotifyProcess();
            ProductScanned();
        }
    }
}
