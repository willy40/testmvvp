namespace testmvvp.Sensors
{
    using Enums;
    using System;
    using System.Linq;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.Devices.Enumeration;
    using Windows.Devices.Gpio;
    using Windows.Devices.Spi;

    public class RFM12BDevice : IRFM12BDevice, IDisposable
    {
        private const byte SPI_CHIP_SELECT_LINE = 0;        /* Chip select line to use*/
        private const string SPI_CONTROLLER_NAME = "SPI0";

        private GpioPin _irqPin;
        private GpioPin _testPin;

        private SpiDevice _spiRfm;

        private byte[] _rfmCmdReadBuffer = new byte[2];
        private byte[] _rfmCmdWriteBuffer = new byte[2];
        private byte[] _spiRWBffer = new byte[127];
        private ushort _dataReceived=0;

        private volatile int _spiBuferPos = 0;


        double[] buftime = new double[300];

        long EndingTime = 0;
        long StartingTime = 0;

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

        public event EventHandler<CompassReading> CompassReadingChangedEvent;

        public async Task Start()
        {
            if (!IsInitialized)
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
                settings.ClockFrequency = 250000;   /* 0.25MHz clock rate                                        */
                settings.Mode = SpiMode.Mode0;      /* The ADC expects idle-low clock polarity so we use Mode0  */
                settings.DataBitLength = 8;

                string spiAqs = SpiDevice.GetDeviceSelector(SPI_CONTROLLER_NAME);
                var deviceInfo = await DeviceInformation.FindAllAsync(spiAqs);
                _spiRfm = await SpiDevice.FromIdAsync(deviceInfo[0].Id, settings);

                Rfm12BInit();
                InitIrq();
                RfmResetFiFo();

                IsInitialized = true;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private void Rfm12BInit()
        {
            ushort stat = 0;
            stat = RF12Cmd(0x80d8);   // EL, EF, 433band, 12.5pF
            stat = RF12Cmd(0x82D8);   // EX, DC         \\ NONE
            stat = RF12Cmd(0xa640);   // 434MHz
            stat = RF12Cmd(0xc623);   // 19.2kbps
            stat = RF12Cmd(0x94a0);   // VDI, FAST, 137kHz, 0dBm, -103dBm
            stat = RF12Cmd(0xc2ac);   // AL, S, DQD4 
            stat = RF12Cmd(0xca80);   //
            stat = RF12Cmd(0xced4);   // Synchro D4
            stat = RF12Cmd(0xc483);   // A1, FI, OE, EN
            stat = RF12Cmd(0x9853);   // 90kHz, 21db
            stat = RF12Cmd(0xcc17);   // 
            stat = RF12Cmd(0xc060);   //
            stat = RF12Cmd(0xe000);   // 
            stat = RF12Cmd(0xc800);   // 
        }

        private void RfmResetFiFo()
        {
            RF12Cmd(0xca81); //Reset FIFO
            RF12Cmd(0xca83);
            RF12Cmd(0x0000);
            _spiBuferPos = 0;
        }

        private ushort RF12Cmd(ushort command)
        {
            ushort ret = 0;

            _rfmCmdWriteBuffer[0] = (byte)(command >> 8);
            _rfmCmdWriteBuffer[1] = (byte)(command & 0xff);

            _spiRfm.TransferFullDuplex(_rfmCmdWriteBuffer, _rfmCmdReadBuffer);

            ret = (ushort)(_rfmCmdReadBuffer[0] << 8 | _rfmCmdReadBuffer[1]);
            return ret;
        }

        private bool RFMDataArrived()
        {
            ushort stat = RF12Cmd(0x0000);
            return (stat & (ushort)RF12Status.RFM12_STATUS_FFIT) == (ushort)RF12Status.RFM12_STATUS_FFIT;
        }

        private void InitIrq()
        {
            var gpio = GpioController.GetDefault();

            if (gpio == null)
            {
                throw new Exception("There is no GPIO controller on this device");
            }

            _irqPin = GpioController.GetDefault().OpenPin(25);
            _testPin = GpioController.GetDefault().OpenPin(21);

            _irqPin.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 0);
            _irqPin.SetDriveMode(GpioPinDriveMode.InputPullDown);
            _irqPin.ValueChanged += Pin_ValueChanged;

            _testPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        private void Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (args.Edge == GpioPinEdge.FallingEdge)
            {
                sender.ValueChanged -= Pin_ValueChanged;
                StartingTime = Stopwatch.GetTimestamp();

                do
                {
                    if (RFMDataArrived())
                    {
                        if (_spiBuferPos > 127)
                        {
                            RfmResetFiFo();
                            sender.ValueChanged += Pin_ValueChanged;
                            return;
                        }
                        else
                        {
                            _dataReceived = RF12Cmd(0xb000);

                            //bitbang on pin 21
                            _testPin.Write(GpioPinValue.High);
                            _testPin.Write(GpioPinValue.Low);

                            _spiRWBffer[_spiBuferPos] = (byte)(_dataReceived & 0x00ff);
                            _spiBuferPos++;
                        }
                    }
                    //else
                    //{
                    //    sender.ValueChanged += Pin_ValueChanged;
                    //    //RfmResetFiFo();
                    //    return;
                    //}

                    //System.Threading.Tasks.Task.Delay(15);

                } while (_spiBuferPos < 12);

                RfmResetFiFo();
                string result = System.Text.Encoding.ASCII.GetString(_spiRWBffer, 2, 4);

  //              EndingTime = Stopwatch.GetTimestamp();
  //              double se = (EndingTime - StartingTime) * (1.0 / Stopwatch.Frequency);

                if (CompassReadingChangedEvent != null)
                {
                    CompassReadingChangedEvent(this, new CompassReading(result));
                }
                sender.ValueChanged += Pin_ValueChanged;
            }
        }

        private void MesureTime()
        {
            EndingTime = Stopwatch.GetTimestamp();
            buftime[_spiBuferPos] = (EndingTime - StartingTime) * (1.0 / Stopwatch.Frequency);
            //StartingTime = EndingTime;
        }
    }
}
