namespace Traceability.ObeserverPattern
{
    public interface IWorkPageObserver
    {
        void EventProductBarcodeReaded(string barcode);
        void ObserveLogAction(string logText);
        void ObserveOPCCangoPermit(bool cangoPermit = true);
        void ObserveCOMConnection(bool isConnected);
        void ObserveScannedComponent(string barcode);
        void ObserveSickConnection(bool isConnected);
        void UpdateSettingsButton(ProductionLine line);
        void UpdateWMDetailsView(bool isShown = true);
    }
}
