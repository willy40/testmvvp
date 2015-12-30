namespace testmvvp.Enums
{
    enum RF12Status : ushort
    {
        RFM12_CMD_STATUS = 0x0000,
        RFM12_STATUS_RGIT = 0x8000,
        RFM12_STATUS_FFIT = 0x8000,
        RFM12_STATUS_POR = 0x4000,
        RFM12_STATUS_RGUR = 0x2000,
        RFM12_STATUS_FFOV = 0x2000,
        RFM12_STATUS_WKUP = 0x1000,
        RFM12_STATUS_EXT = 0x0800,
        RFM12_STATUS_LBD = 0x0400,
        RFM12_STATUS_FFEM = 0x0200,
        RFM12_STATUS_ATS = 0x0100,
        RFM12_STATUS_RSSI = 0x0100,
        RFM12_STATUS_DQD = 0x0080,
        RFM12_STATUS_CRL = 0x0040,
        RFM12_STATUS_ATGL = 0x0020,
    }
}
