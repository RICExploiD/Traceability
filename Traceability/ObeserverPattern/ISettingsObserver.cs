namespace Traceability.ObeserverPattern
{
    public interface ISettingsObserver
    {
        void UpdateConfigUI();
        void ReconnectToComponentScanner(ScanType scanType);
        void ReconnectToProductScanner(ScanType scanType);
        void ReconnectToOPCTags(ConnectionPLCType connType);
    }
}
