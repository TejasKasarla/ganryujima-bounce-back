using System.Threading.Tasks;
using UnityEngine;
using KeyInput = UnityEngine.Input;

namespace Ganryujima.Input.Face
{
    /// <summary>Temporary keyboard provider. Never opens a webcam.</summary>
    public sealed class MockFaceInput : IFaceInput
    {
        private bool active;
        private FaceState state;

        public Task start()
        {
            active = true;
            state = new FaceState { tracking = true, calibrated = true, composure = 0.95f, gaze = Gaze.center };
            return Task.CompletedTask;
        }

        public void stop() { active = false; state = default; }
        public Task calibrate(int durationMs = 5000)
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
                state.composure = 0.95f;
                return;
            }
            state.leanX = (KeyInput.GetKey(KeyCode.E) ? 1 : 0) - (KeyInput.GetKey(KeyCode.Q) ? 1 : 0);
            if (KeyInput.GetKeyDown(KeyCode.Q)) state.leanTrigger = -1;
            if (KeyInput.GetKeyDown(KeyCode.E)) state.leanTrigger = 1;
            state.kiai = KeyInput.GetKeyDown(KeyCode.F);
            state.nod = KeyInput.GetKeyDown(KeyCode.Y);
            state.shake = KeyInput.GetKeyDown(KeyCode.N);
            state.composure = KeyInput.GetKey(KeyCode.C) ? 0.15f : 0.95f;
            if (KeyInput.GetKeyDown(KeyCode.Z)) state.gaze = Gaze.left;
            if (KeyInput.GetKeyDown(KeyCode.X)) state.gaze = Gaze.center;
            if (KeyInput.GetKeyDown(KeyCode.V)) state.gaze = Gaze.right;
            if (KeyInput.GetKeyDown(KeyCode.G)) state.wink = state.wink == Wink.none ? Wink.left : Wink.none;
        }

        public FaceState getState() => state;
    }
}
