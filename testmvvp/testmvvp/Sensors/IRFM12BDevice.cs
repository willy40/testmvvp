using I2CCompass.Sensors;
using System;
using System.Threading.Tasks;

namespace testmvvp.Sensors
{
    public interface IRFM12BDevice
    {
        event EventHandler<CompassReading> CompassReadingChangedEvent;
        bool IsInitialized { get; }
        bool IsDisposed { get; }
        Task Start();
    }
}