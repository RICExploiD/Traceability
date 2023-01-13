using System.Windows;

namespace Traceability.IndividualWorkPage
{
    using Services;
    partial class IndividualWorkPage
    {
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WorkPageCore.ReconnectToComponentScanner(AppSettings.ComponentScanType);
            WorkPageCore.StartOperatorNotifyProcess();
            //OperatorNotifyProcessor();
            ProductScanned();
        }

    }
}
