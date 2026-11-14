using System;
using UnityEngine;
using KeyInput = UnityEngine.Input;

namespace Ganryujima.Input.Face
{
    [DefaultExecutionOrder(1000)]
    public sealed class WebcamPreview : MonoBehaviour
    {
        private IFaceInput mounted;
        private FaceInput camera;
        private Texture2D texture;
        private byte[] lastPreview;
        private bool visible;
        private double flashUntil;
        private string flash = "";

        public void Mount(IFaceInput provider) { mounted = provider; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<WebcamPreview>() != null) return;
            var host = new GameObject("Face webcam preview");
            DontDestroyOnLoad(host);
            host.AddComponent<WebcamPreview>();
        }

        private void Update()
        {
            if (KeyInput.GetKeyDown(KeyCode.F1)) visible = !visible;
            IFaceInput source = mounted ?? FaceInputFactory.Active;
            FaceInput next = FaceInputFactory.CameraOf(source);
            if (camera != next) { if (camera != null) camera.PreviewRequested = false; camera = next; }
            if (camera != null) camera.PreviewRequested = visible;
            if (!visible || source == null) return;
            FaceState state = source.getState();
            string pulse = state.kiai ? "KIAI" : state.nod ? "NOD" : state.shake ? "SHAKE" : state.leanTrigger < 0 ? "LEAN LEFT" : state.leanTrigger > 0 ? "LEAN RIGHT" : "";
            if (pulse.Length > 0) { flash = pulse; flashUntil = Time.realtimeSinceStartupAsDouble * 1000 + FaceConfig.EventFlashMs; }
            byte[] bytes = camera?.PreviewFrame;
            if (bytes == null || ReferenceEquals(bytes, lastPreview)) return;
            lastPreview = bytes;
            if (texture == null) texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try { ImageConversion.LoadImage(texture, bytes, false); } catch (Exception) { }
        }

        private void OnGUI()
        {
            IFaceInput source = mounted ?? FaceInputFactory.Active;
            if (!visible || source == null) return;
            FaceState state = source.getState();
            float width = Math.Min(320, Math.Max(160, Screen.width - 32));
            float imageHeight = width * 0.75f;
            float left = Math.Max(8, Screen.width - width - 24);
            float top = 18;
            Color originalColor = GUI.color;
            var small = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            GUI.Box(new Rect(left - 8, top - 8, width + 16, imageHeight + 160), GUIContent.none);
            string mode = camera == null || !camera.IsReady ? "KEYBOARD" : state.tracking ? "TRACKING" : "SEARCHING";
            GUI.Label(new Rect(left, top, width, 22), "FACE / " + mode, small);
            var image = new Rect(left, top + 26, width, imageHeight);
            if (texture != null) GUI.DrawTextureWithTexCoords(image, texture, new Rect(1, 0, -1, 1));
            else { GUI.color = new Color(0.08f, 0.1f, 0.09f); GUI.DrawTexture(image, Texture2D.whiteTexture); GUI.color = originalColor; }
            if (state.tracking && camera?.PreviewLandmarks != null)
            {
                GUI.color = new Color(0.48f, 0.91f, 0.73f);
                float[] points = camera.PreviewLandmarks;
                for (int point = 0; point + 2 < points.Length; point += 3)
                    GUI.DrawTexture(new Rect(image.x + (1 - points[point]) * width, image.y + points[point + 1] * imageHeight, 2, 2), Texture2D.whiteTexture);
                GUI.color = originalColor;
            }
            if (camera != null && camera.IsCalibrating)
            {
                var countdown = new GUIStyle(small) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
                GUI.Label(image, Math.Ceiling(camera.CalibrationRemainingMs / 1000).ToString(), countdown);
            }
            float details = image.y + imageHeight + 8;
            GUI.Label(new Rect(left, details, width, 38), $"Lean {state.leanX:F2}   Composure {state.composure:F2}\nYaw {state.headYaw:F1}   Pitch {state.headPitch:F1}   Roll {state.headRoll:F1}", small);
            GUI.Label(new Rect(left, details + 40, width, 20), $"Gaze {state.gaze}   Wink {state.wink}", small);
            if (Time.realtimeSinceStartupAsDouble * 1000 < flashUntil)
            {
                GUI.color = new Color(1, 0.35f, 0.35f);
                GUI.Label(new Rect(left, details + 61, width / 2, 24), flash, small);
                GUI.color = originalColor;
            }
            bool enabled = GUI.enabled;
            GUI.enabled = camera != null && camera.IsReady && !camera.IsCalibrating;
            if (GUI.Button(new Rect(left + width - 100, details + 62, 100, 26), "Calibrate")) _ = camera.calibrate();
            GUI.enabled = enabled;
            GUI.Label(new Rect(left, details + 94, width, 30), camera?.Error ?? "Keyboard simulation", small);
            GUI.color = originalColor;
        }

        private void OnDisable() { if (camera != null) camera.PreviewRequested = false; }
        private void OnDestroy() { OnDisable(); if (texture != null) Destroy(texture); }
    }
}