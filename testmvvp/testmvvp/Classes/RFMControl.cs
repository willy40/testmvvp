namespace testmvvp.Classes
{
    using SPIExceptptions;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using testmvvp.Classes.Interfaces;
    using testmvvp.Enums;
    using Windows.Devices.Enumeration;
    using Windows.Devices.Gpio;
    using Windows.Devices.Spi;

    public class RFMControl : IRFMControl, IDisposable
    {
        public const int MinPacketSize = 4;
        public const int MaxPacketSize = 127;

        private const byte SPI_CHIP_SELECT_LINE = 0;        /* Chip select line to use*/
        private const string SPI_CONTROLLER_NAME = "SPI0";

        private double _frqScaleFactor = (1.0 / Stopwatch.Frequency);

        private long _endingTime = 0;
        private long _startingTime = 0;
        private double _irqTimeout = 0;

        private GpioPin _irqPin;
        private GpioPin _testPin;
        private SpiDevice _spiRfm;

        #region Properties
        private byte[] RfmCmdReadBuffer = new byte[2];

        private byte[] RfmCmdWriteBuffer = new byte[2];

        private int SpiBufferPos { get; set; }

        private RF12IrqState IrqState { get; set; }

        private byte DataToRead { get; set; }

        private byte DataReaded { get; set; }

        private byte ByteReceived { get; set; }

        public ushort CalculatedCRC { get; set; }

        public bool IsInitialized { get; private set; }

        #endregion

        public RFMControl()
        {
            SpiBufferPos = 0;
            DataToRead = 0;
            InitSpi();
            IrqInit();
        }

        public async void Start()
        {
            await Task.Delay(1);
            Rfm12BInit();
            _irqPin.ValueChanged += Pin_ValueChanged;

            SetState_Idle();
            RfmResetFiFo();

        }

        public async void Stop()
        {
            await Task.Delay(1);
            _irqPin.ValueChanged -= Pin_ValueChanged;
        }

        public void Dispose()
        {
            _irqPin.ValueChanged -= Pin_ValueChanged;
            _spiRfm = null;
            _irqPin = null;
            _testPin = null;
        }

        #region Helpers
        private void SetState_Idle()
        {
            SpiBufferPos = 0;
            DataToRead = MinPacketSize;
            IrqState = RF12IrqState.Idle;
        }

        private void SetState_Header()
        {
            IrqState = RF12IrqState.Header;
        }

        private void SetState_DataLength()
        {
            IrqState = RF12IrqState.DataLength;
            DataReaded = 0;
        }

        private void SetState_Data()
        {
            IrqState = RF12IrqState.Data;
        }

        private void SetState_CrcLow()
        {
            IrqState = RF12IrqState.Crc_LowByte;
        }

        private void SetState_CrcHight()
        {
            IrqState = RF12IrqState.Crc_HightByte;
        }

        private bool RFMDataArrived(ushort stat)
        {
            
            return (stat & (ushort)RF12Status.RFM12_STATUS_FFIT) == (ushort)RF12Status.RFM12_STATUS_FFIT;
        }

        private void Rfm12BInit()
        {
            RF12Cmd(0x80d8);   // EL, EF, 433band, 12.5pF
            RF12Cmd(0x82D8);   // EX, DC         \\ NONE
            RF12Cmd(0xa640);   // 434MHz
            RF12Cmd(0xc623);   // 19.2kbps
            RF12Cmd(0x94a0);   // VDI, FAST, 137kHz, 0dBm, -103dBm
            RF12Cmd(0xc2ac);   // AL, S, DQD4 
            RF12Cmd(0xca80);   //
            RF12Cmd(0xced4);   // Synchro D4
            RF12Cmd(0xc483);   // A1, FI, OE, EN
            RF12Cmd(0x9853);   // 90kHz, 21db
            RF12Cmd(0xcc17);   // 
            Task.Delay(1);
            RF12Cmd(0xc060);   //
            RF12Cmd(0xe000);   // 
            RF12Cmd(0xc800);   // 
        }

        private void RfmResetFiFo()
        {
            RF12Cmd(0xca81); //Reset FIFO
            RF12Cmd(0xca83);
            RF12Cmd(0x0000);

            SetState_Idle();
        }

        private ushort RF12Cmd(ushort command)
        {
            ushort ret = 0;

            RfmCmdWriteBuffer[0] = (byte)(command >> 8);
            RfmCmdWriteBuffer[1] = (byte)(command & 0xff);

            _spiRfm.TransferFullDuplex(RfmCmdWriteBuffer, RfmCmdReadBuffer);

            ret = (ushort)(RfmCmdReadBuffer[0] << 8 | RfmCmdReadBuffer[1]);
            return ret;
        }

        private async void InitSpi()
        {
            var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
            settings.ClockFrequency = 250000;   /* 0.25MHz clock rate                                        */
            settings.Mode = SpiMode.Mode0;      /* The ADC expects idle-low clock polarity so we use Mode0  */
            settings.DataBitLength = 8;

            try
            {
                string spiAqs = SpiDevice.GetDeviceSelector(SPI_CONTROLLER_NAME);
                var deviceInfo = await DeviceInformation.FindAllAsync(spiAqs);
                _spiRfm = await SpiDevice.FromIdAsync(deviceInfo[0].Id, settings);

                if (_spiRfm == null)
                {
                    throw (new SpiControllerFoundException());
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void IrqInit()
        {
            var gpio = GpioController.GetDefault();

            if (gpio == null)
            {
                throw new GpioException();
            }

            _irqPin = gpio.OpenPin(25);
            _testPin = gpio.OpenPin(21);

            _irqPin.DebounceTimeout = new TimeSpan(0, 0, 0, 0, 0);
            _irqPin.SetDriveMode(GpioPinDriveMode.Input);
            _testPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        private void Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            IRFMPacket tmpRfmPacket = null;
            long ticks = 0;

            ushort stat = RF12Cmd(0x0000);

            if (args.Edge == GpioPinEdge.FallingEdge)
            {
                _startingTime = Stopwatch.GetTimestamp();
                _irqPin.ValueChanged -= Pin_ValueChanged;

                do
                {
                    if (RFMDataArrived(stat))
                    {
                        ProcessData(ref tmpRfmPacket);
                    }
                    else
                    {
                        return;
                        //Task.Delay(TimeSpan.FromMilliseconds(0.1));
                        //ticks++;
                    }
                    //MesureTimeOut();

                    //if (_irqTimeout > 7)
                    //{
                    //    BreakReading();
                    //    return;
                    //}

                } while (DataToRead > 0);

                SetState_Idle();
                RfmResetFiFo();

                MesureTimeOut();
                Debug.Write(string.Format("Time: {0}\n", _irqTimeout.ToString()));

                if (tmpRfmPacket.IsPacketCorrect())
                {
                    string result = System.Text.Encoding.ASCII.GetString(tmpRfmPacket.GetBufferArray(), 0, 4);
                }

                sender.ValueChanged += Pin_ValueChanged;
            }
        }

        private void ProcessData(ref IRFMPacket tmpRfmPacket)
        {
            ByteReceived = (byte)(RF12Cmd(0xb000) & 0x00ff);

            switch (IrqState)
            {
                case RF12IrqState.Idle:
                    {
                        tmpRfmPacket = new RFMPacket()
                        {
                            Header = ByteReceived
                        };

                        SetState_DataLength();
                    }
                    break;

                case RF12IrqState.DataLength:
                    {
                        if (ByteReceived > MaxPacketSize)
                        {
                            BreakReading();
                            return;
                        }

                        tmpRfmPacket.DataLength = ByteReceived;

                        //increase total data lenght of packet data lenght
                        DataToRead += tmpRfmPacket.DataLength;

                        SetState_Data();
                    }
                    break;

                case RF12IrqState.Data:
                    {
                        if (DataReaded++ < tmpRfmPacket.DataLength)
                        {
                            tmpRfmPacket.SetData(ByteReceived);
                        }
                        else
                        {
                            tmpRfmPacket.ReceivedCRC = (ushort)(ByteReceived);
                            SetState_CrcHight();
                        }
                    }
                    break;

                case RF12IrqState.Crc_HightByte:
                    {
                        tmpRfmPacket.ReceivedCRC |= (ushort)((ByteReceived << 8) & 0xff00);
                    }

                    break;
            }

            DataToRead--;

            //_startingTime = Stopwatch.GetTimestamp();
            //while (_irqPin.Read() == GpioPinValue.H)
            //{
            //                        MesureTimeOut();
            //    if (_irqTimeout > 1)
            //    {
            //        Debug.Write(string.Format("TimeOut: {0}\n", _irqTimeout.ToString()));
            //        BreakReading(sender);
            //        return;
            //    }
            //}
        }

        private void MesureTimeOut()
        {
            long endTime = Stopwatch.GetTimestamp();
            _irqTimeout = ( endTime- _startingTime) * _frqScaleFactor;
            //_startingTime = endTime;
        }

        private void BreakReading()
        {
            RfmResetFiFo();
            _irqPin.ValueChanged += Pin_ValueChanged;
        }
        #endregion
    }
}