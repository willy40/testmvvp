namespace SPIExceptptions
{
    using System;

    public class SpiControllerFoundException : Exception
    {
        public SpiControllerFoundException()
            : base("Could not find the SPI controller")
        { }
    }
}
