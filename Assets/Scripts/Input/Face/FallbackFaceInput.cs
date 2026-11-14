using System.Threading.Tasks;

namespace Ganryujima.Input.Face
{
    public sealed class FallbackFaceInput : IFaceInput
    {
        public FaceInput Camera { get; }
        public bool UsingMock => !Camera.IsReady;
        private readonly MockFaceInput mock = new MockFaceInput();
        private FaceState state;
        private bool active;

        public FallbackFaceInput() { Camera = new FaceInput(); }

        public async Task start()
        {
            await mock.start();
            await Camera.start();
            active = true;
            state = UsingMock ? mock.getState() : Camera.getState();
        }

        public void stop() { active = false; Camera.stop(); mock.stop(); state = default; }
        public Task calibrate(int durationMs = FaceConfig.CalibrationDurationMs) => UsingMock ? mock.calibrate(durationMs) : Camera.calibrate(durationMs);

        public void tick()
        {
            if (!active) return;
            Camera.tick();
            mock.tick();
            state = UsingMock ? mock.getState() : Camera.getState();
        }

        public FaceState getState() => state;
    }
}