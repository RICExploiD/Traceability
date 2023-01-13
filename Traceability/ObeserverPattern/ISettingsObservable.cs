using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Traceability.ObeserverPattern
{
    public interface ISettingsObservable
    {
        void InitObserver(ISettingsObserver window);
    }
}
