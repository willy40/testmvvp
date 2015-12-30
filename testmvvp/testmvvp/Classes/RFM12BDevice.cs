namespace testmvvp.Classes
{
    using System;
    using System.Threading.Tasks;
    using Classes.Interfaces;

    public class RFDevice : IRFDevice, IDisposable
    {
        private IRFMControl _rfmControl;

        public bool IsInitialized
        {
            get;
            private set;
        }

        public bool IsDisposed
        {
            get;
            private set;
        }

        public RFDevice()
        {
            _rfmControl = new RFMControl();
        }

        //public event EventHandler<CompassReading> CompassReadingChangedEvent;

        public async Task Start()
        {
            if (!IsInitialized)
            {

                IsInitialized = true;
            }

            _rfmControl.Start();
        }

        public async Task Stop()
        {
            if(IsInitialized)
            {
                _rfmControl.Stop();
            }
        }

        public void Dispose()
        {
            _rfmControl.Dispose();
            IsDisposed = true;
        }
    }
}
