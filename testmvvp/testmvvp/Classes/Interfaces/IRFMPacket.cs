namespace testmvvp.Classes.Interfaces
{
    public interface IRFMPacket
    { 
        byte Header { get; set; }

        byte DataLength { get; set; }

        ushort ReceivedCRC { get; set; }

        byte[] GetBufferArray();
        bool IsPacketCorrect();
        void SetData(byte data);
    }
}