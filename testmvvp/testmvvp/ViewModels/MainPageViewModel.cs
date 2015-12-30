namespace testmvvp.ViewModels
{
    using Microsoft.Practices.Prism.Commands;
    using Microsoft.Practices.Prism.Mvvm;
    using System.Windows.Input;
    using Classes.Interfaces;
    using Classes;

    public class MainPageViewModel : BindableBase
    {
        private IRFDevice _rfDevice;

        private CompassReading? _compassReading;

        private DelegateCommand _startCommand;
        private DelegateCommand _stopCommand;
        private bool IsStarted { get; set; }

        public MainPageViewModel(IRFDevice rfDevice)
        {
            _compassReading = new CompassReading("0.0");
            if(rfDevice==null)
            {
                throw new System.Exception("Cannot create RFM Device");
            }

            _rfDevice = rfDevice;

            //_rfmcontrol.CompassReadingChangedEvent += async (e, cr) =>
            //{
            //    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { CompassReading = cr; });
            //};

            _startCommand = DelegateCommand.FromAsyncHandler(
                async () =>
                {
                    await rfDevice.Start();
                    IsStarted = true;
                    UpdateCommands();
                },
                () =>
                {
                    return true;
                });

            _stopCommand = new DelegateCommand(
                async () =>
                {
                    await rfDevice.Stop();
                    IsStarted = false;
                    UpdateCommands();
                },
                () =>
                {
                    return true;
                });
        }

        public ICommand StartCommand { get { return _startCommand; } }

        public ICommand StopCommand { get { return _stopCommand; } }

        public CompassReading CompassReading
        {
            get { return _compassReading.HasValue ? _compassReading.Value : new CompassReading(); }
            set { SetProperty(ref _compassReading, value); }
        }

        private void UpdateCommands()
        {
            _startCommand.RaiseCanExecuteChanged();
            _stopCommand.RaiseCanExecuteChanged();
        }
    }
}