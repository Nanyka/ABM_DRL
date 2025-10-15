using Unity.Sentis;

namespace Sugarscape
{
    public interface IObservationProvider
    {
        float[] GetVisualObs();     // returns the full current obs vector
        float[] GetFloatObs(); 
        Worker GetWorker();
    }
}