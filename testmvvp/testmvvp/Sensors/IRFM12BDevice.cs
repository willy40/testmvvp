namespace testmvvp.Sensors
{
    using System;
    using System.Threading.Tasks;

    public interface IRFM12BDevice
    {
        event EventHandler<CompassReading> CompassReadingChangedEvent;
        bool IsInitialized { get; }
        bool IsDisposed { get; }
        Task Start();
    }
}