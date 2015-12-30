namespace testmvvp.Classes.Interfaces
{
    using System.Threading.Tasks;

    public interface IRFMControl
    {
        void Start();
        void Stop();
        //void InitAll();
        void Dispose();
    }
}