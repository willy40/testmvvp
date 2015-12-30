namespace testmvvp.Enums
{
    public enum RF12IrqState
    {
        Idle =0,
        Header,
        DataLength,
        Data,
        Crc_LowByte,
        Crc_HightByte
    }
}
