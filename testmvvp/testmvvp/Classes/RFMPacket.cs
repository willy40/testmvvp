namespace testmvvp.Classes
{
    using Interfaces;
    using System.Collections.Generic;
    using System.Linq;

    public class RFMPacket : BaseCRC16, IRFMPacket
    {
        private IList<byte> _buffer;
        private byte _header;
        private byte _dataLength;

        public byte Header
        {
            get
            {
                return _header;
            }
            set
            {
                _header = value;
                _crc = crc16_update(_crc, value);
            }
        }

        public byte DataLength
        {
            get
            {
                return _dataLength;
            }
            set
            {
                _dataLength = value;
                _crc = crc16_update(_crc, value);
            }
        }

        public ushort ReceivedCRC { get; set; }

        public byte[] GetBufferArray()
        {
            return _buffer.ToArray<byte>();
        }

        public void SetData(byte data)
        {
            _buffer.Add(data);
            _crc = crc16_update(_crc, data);
        }

        public bool IsPacketCorrect()
        {
            return _crc == ReceivedCRC;
        }

        public RFMPacket()
        {
            _buffer = new List<byte>();
            _crc = crc16_update(0xffff, 0xd4);
        }
    }
}
