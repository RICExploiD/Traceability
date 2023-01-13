using System;

namespace Traceability.ObeserverPattern
{
    public interface IWorkPageObservable
    {
        void ProductScanned();
        void ClearTemporaryData();
        void UnsubscribeFromEvents();
    }
}
