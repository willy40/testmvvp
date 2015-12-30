namespace SPIExceptptions
{
    using System;

    public class GpioException : Exception
    {
        public GpioException()
            : base("GPIO does not exist on the current system.")
        { }
    }
}
