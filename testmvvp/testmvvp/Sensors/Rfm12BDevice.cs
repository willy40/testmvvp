namespace testmvvp.Sensors
{
    using Enums;
    using System;
    using System.Threading.Tasks;
    using Windows.Devices.Enumeration;
    using Windows.Devices.Gpio;
    using Windows.Devices.Spi;

    public class RFM12BDevice : IRFM12BDevice, IDisposable
    {
        private const byte SPI_CHIP_SELECT_LINE = 0;        /* Chip select line to use*/
        private const string SPI_CONTROLLER_NAME = "SPI0";

        private GpioPin _irqPin;
        private SpiDevice _spiRfm;

        private byte[] _rfmCmdReadBuffer = new byte[2];
        private byte[] _rfmCmdWriteBuffer = new byte[2];
        private byte[] _spiRWBffer = new byte[127];

        private int _spiBuferPos = 0;

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
                settings.ClockFrequency = 2500000;   /* 0.25MHz clock rate                                        */
                settings.Mode = SpiMode.Mode0;      /* The ADC expects idle-low clock polarity so we use Mode0  */
                settings.DataBitLength = 8;

                string spiAqs = SpiDevice.GetDeviceSelector(SPI_CONTROLLER_NAME);
                var deviceInfo = await DeviceInformation.FindAllAsync(spiAqs);
                _spiRfm = await SpiDevice.FromIdAsync(deviceInfo[0].Id, settings);

                Rfm12BInit();
                RfmResetFiFo();
                InitIrq();

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
            return (stat & (ushort)RF12Status.RFM12_STATUS_FFIT) > 0;
        }

        private void InitIrq()
        {
            var gpio = GpioController.GetDefault();

            if (gpio == null)
            {
                throw new Exception("There is no GPIO controller on this device");
            }

            _irqPin = GpioController.GetDefault().OpenPin(25);
            _irqPin.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 0);
            _irqPin.SetDriveMode(GpioPinDriveMode.Input);
            _irqPin.ValueChanged += Pin_ValueChanged;
        }

        private void Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (args.Edge == GpioPinEdge.FallingEdge)
            {
                do
                {
                    if (RFMDataArrived())
                    {
                        sender.ValueChanged -= Pin_ValueChanged;
                        if (_spiBuferPos > 127)
                        {
                            RfmResetFiFo();
                            _spiBuferPos = 0;
                        }
                        else
                        {
                            ushort data = RF12Cmd(0xb000);
                            _spiRWBffer[_spiBuferPos] = (byte)(data & 0x00ff);
                            _spiBuferPos++;
                        }
                    }
                } while (_spiBuferPos < 12);

                sender.ValueChanged += Pin_ValueChanged;
                RfmResetFiFo();

                string result = System.Text.Encoding.ASCII.GetString(_spiRWBffer, 2, 4);
            }
        }
    }
}
