namespace testmvvp.Sensors
{
    public struct CompassReading
    {
        public CompassReading(double heading)
        {
            Heading = heading;
        }

        public double Heading { get; }
    }
}
