using System.Threading.Tasks;

namespace Ganryujima.Input.Face
{
    public interface IFaceInput
    {
        Task start();
        void stop();
        Task calibrate(int durationMs = FaceConfig.CalibrationDurationMs);
        void tick();
        FaceState getState();
    }
}