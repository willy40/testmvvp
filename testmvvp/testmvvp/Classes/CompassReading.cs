namespace testmvvp.Classes
{
    public struct CompassReading
    {
        public CompassReading(string heading)
        {
            Heading = heading;
        }

        public string Heading { get; }
    }
}
