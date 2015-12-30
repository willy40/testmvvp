namespace testmvvp.Classes.Interfaces
{
    using System.Threading.Tasks;

    public interface IRFDevice
    {
        //event EventHandler<CompassReading> CompassReadingChangedEvent;
        bool IsInitialized { get; }
        bool IsDisposed { get; }
        Task Start();
        Task Stop();
    }
}