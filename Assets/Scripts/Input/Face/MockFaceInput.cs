using System.Threading.Tasks;
using UnityEngine;
using KeyInput = UnityEngine.Input;

namespace Ganryujima.Input.Face
{
    /// <summary>Dependency-free keyboard provider. Never opens a webcam.</summary>
    public sealed class MockFaceInput : IFaceInput
    {
        private bool active;
        private int leanDirection;
        private FaceState state;

        public Task start()
        {
            active = true;
            leanDirection = 0;
            state = new FaceState { tracking = true, calibrated = true, composure = FaceConfig.MockCalmComposure, gaze = Gaze.center };
            return Task.CompletedTask;
        }

        public void stop() { active = false; leanDirection = 0; state = default; }
        public Task calibrate(int durationMs = FaceConfig.CalibrationDurationMs)
        {
            state.calibrated = active;
            return Task.CompletedTask;
        }

        public void tick()
        {
            state.leanTrigger = 0;
            state.kiai = state.nod = state.shake = false;
            state.timestamp = Time.realtimeSinceStartupAsDouble * 1000.0;
            if (!active) return;
            if (!Application.isFocused)
            {
                state.leanX = 0;
                leanDirection = 0;
                state.composure = FaceConfig.MockCalmComposure;
                return;
            }
            state.leanX = (KeyInput.GetKey(KeyCode.E) ? 1 : 0) - (KeyInput.GetKey(KeyCode.Q) ? 1 : 0);
            int nextDirection = (int)state.leanX;
            if (nextDirection != 0 && nextDirection != leanDirection) state.leanTrigger = nextDirection;
            leanDirection = nextDirection;
            state.kiai = KeyInput.GetKeyDown(KeyCode.F);
            state.nod = KeyInput.GetKeyDown(KeyCode.Y);
            state.shake = KeyInput.GetKeyDown(KeyCode.N);
            state.composure = KeyInput.GetKey(KeyCode.C) ? FaceConfig.MockLowComposure : FaceConfig.MockCalmComposure;
            if (KeyInput.GetKeyDown(KeyCode.Z)) state.gaze = Gaze.left;
            if (KeyInput.GetKeyDown(KeyCode.X)) state.gaze = Gaze.center;
            if (KeyInput.GetKeyDown(KeyCode.V)) state.gaze = Gaze.right;
            if (KeyInput.GetKeyDown(KeyCode.G)) state.wink = state.wink == Wink.none ? Wink.left : Wink.none;
        }

        public FaceState getState() => state;
    }
}
