namespace testmvvp.Classes
{
    public abstract class BaseCRC16
    {
        protected ushort _crc;

        public ushort crc16_update(ushort crc, byte a)
        {
            int i;
            crc ^= a;
            for (i = 0; i < 8; ++i)
            {
                if ((crc & 1) > 0)
                    crc = (ushort)((crc >> 1) ^ 0xA001);
                else
                    crc = (ushort)(crc >> 1);
            }
            return crc;
        }
    }
}
