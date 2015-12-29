namespace testmvvp.ViewModels
{
    using System;
    using Sensors;
    using Microsoft.Practices.Prism.Commands;
    using Microsoft.Practices.Prism.Mvvm;
    using System.Windows.Input;
    using Windows.ApplicationModel.Core;
    using Windows.UI.Core;

    public class MainPageViewModel : BindableBase
    {
        private IRFM12BDevice _rfmDevice;

        private CompassReading? _compassReading;

        private DelegateCommand _startCommand;
        private DelegateCommand _stopCommand;
        private bool IsStarted { get; set; }

        public MainPageViewModel(IRFM12BDevice rfm12Device)
        {
            _compassReading = new CompassReading("0.0");

            if (rfm12Device == null)
            {
                throw new ArgumentNullException(nameof(rfm12Device));
            }

            _rfmDevice = rfm12Device;
            _rfmDevice.Start();

            _rfmDevice.CompassReadingChangedEvent += async (e, cr) =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { CompassReading = cr; });
            };

            _startCommand = DelegateCommand.FromAsyncHandler(
                async () =>
                {
                    await _rfmDevice.Start();
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
                    await _rfmDevice.Start();
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