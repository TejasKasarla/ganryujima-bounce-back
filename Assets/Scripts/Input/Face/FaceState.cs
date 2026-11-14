using System.Threading.Tasks;

namespace Ganryujima.Input.Face
{
    public enum Gaze { left, center, right, unknown }
    public enum Wink { none, left, right }

    // Value snapshot: changing a returned copy cannot mutate provider state.
    // Temporary C# port of shared contract; Dev A owns this folder at merge.
    public struct FaceState
    {
        public bool tracking;
        public bool calibrated;
        public float leanX;          // -1 .. +1
        public int leanTrigger;      // -1, 0, +1; one-frame event
        public bool kiai;            // one-frame event
        public float composure;      // 0 .. 1
        public bool nod;             // one-frame event
        public bool shake;           // one-frame event
        public Wink wink;
        public Gaze gaze;
        public float headYaw;        // degrees
        public float headPitch;      // degrees
        public float headRoll;       // degrees
        public double timestamp;     // milliseconds
    }

    public interface IFaceInput
    {
        Task start();                // Must never throw; failure => tracking=false.
        void stop();
        Task calibrate(int durationMs = 5000);
        void tick();                 // Once at start of each game frame.
        FaceState getState();        // Cheap value snapshot.
    }
}
