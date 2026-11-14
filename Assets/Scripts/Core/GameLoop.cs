using System;
using UnityEngine;
using Ganryujima.Input.Face;
using Ganryujima.World;
using KeyInput = UnityEngine.Input;

namespace Ganryujima.Core
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameLoop : MonoBehaviour
    {
        private IFaceInput face;
        private FaceState state;
        private CharacterController player;
        private Camera view;
        private bool ready;
        private bool entered;
        private float yaw;
        private float pitch;
        private float verticalVelocity;
        private string inputError;
        public int FramesTicked { get; private set; }

        private async void Awake()
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
            ArenaBuilder.Build();
            var body = new GameObject("Player");
            body.transform.position = new Vector3(0, 0, GameConfig.SpawnDistance);
            player = body.AddComponent<CharacterController>();
            player.height = 1.8f;
            player.radius = GameConfig.PlayerRadius;
            player.center = new Vector3(0, 0.9f, 0);
            view = new GameObject("First Person Camera").AddComponent<Camera>();
            view.tag = "MainCamera";
            view.transform.SetParent(body.transform, false);
            view.transform.localPosition = Vector3.up * GameConfig.EyeHeight;
            view.fieldOfView = GameConfig.FieldOfView;
            view.nearClipPlane = 0.05f;
            view.farClipPlane = 150;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(0.82f, 0.81f, 0.75f);
            view.gameObject.AddComponent<AudioListener>();
            ArenaBuilder.BuildKatana(view.transform);
            face = FaceInputFactory.createFaceInput();
            try { await face.start(); }
            catch (Exception error) { inputError = "Face provider failed: " + error.Message; Debug.LogException(error); }
            if (this == null) { face.stop(); return; }
            ready = true;
            SetCapture(false);
        }

        private void Update()
        {
            if (!ready) return;
            face.tick(); // First gameplay action, once per frame, including entry gate.
            state = face.getState();
            FramesTicked++;
            if (KeyInput.GetKeyDown(KeyCode.Escape)) SetCapture(false);
            if (!entered || Cursor.lockState != CursorLockMode.Locked || !Application.isFocused) return;
            yaw += KeyInput.GetAxisRaw("Mouse X") * GameConfig.MouseSensitivity;
            pitch = Mathf.Clamp(pitch - KeyInput.GetAxisRaw("Mouse Y") * GameConfig.MouseSensitivity, -GameConfig.MaximumPitch, GameConfig.MaximumPitch);
            player.transform.rotation = Quaternion.Euler(0, yaw, 0);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            float x = (KeyInput.GetKey(KeyCode.D) ? 1 : 0) - (KeyInput.GetKey(KeyCode.A) ? 1 : 0);
            float z = (KeyInput.GetKey(KeyCode.W) ? 1 : 0) - (KeyInput.GetKey(KeyCode.S) ? 1 : 0);
            var direction = Vector3.ClampMagnitude(new Vector3(x, 0, z), 1);
            float dt = Mathf.Min(Time.deltaTime, GameConfig.MaximumDelta);
            var horizontal = player.transform.TransformDirection(direction) * (GameConfig.WalkSpeed * dt);
            var target = player.transform.position + horizontal;
            var limited = Vector2.ClampMagnitude(new Vector2(target.x, target.z), GameConfig.ArenaRadius - GameConfig.PlayerRadius);
            horizontal.x = limited.x - player.transform.position.x;
            horizontal.z = limited.y - player.transform.position.z;
            if (player.isGrounded && verticalVelocity < 0) verticalVelocity = GameConfig.GroundedVelocity;
            verticalVelocity += GameConfig.Gravity * dt;
            player.Move(horizontal + Vector3.up * (verticalVelocity * dt));
        }

        private void SetCapture(bool capture)
        {
            entered = capture;
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
        }
        private void OnApplicationFocus(bool focus) { if (!focus) SetCapture(false); }
        private void OnDisable() { SetCapture(false); }
        private void OnDestroy() { face?.stop(); }

        private void OnGUI()
        {
            float scale = Mathf.Max(0.6f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            var heading = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            var small = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            GUI.Label(new Rect(30, 22, 600, 40), "GANRYUJIMA  /  BOUNCE BACK", heading);
            GUI.Label(new Rect(30, 56, 600, 30), "01 — THE PRACTICE GROUND · UNITY", small);
            GUI.Box(new Rect(25, height - 152, 650, 105), GUIContent.none);
            GUI.Label(new Rect(40, height - 145, 625, 100),
                $"KEYBOARD FACE MOCK · {(state.tracking ? "TRACKING" : "OFFLINE")} · {(state.calibrated ? "CALIBRATED" : "UNCALIBRATED")}\n" +
                $"Composure {state.composure:F2} · Lean {state.leanX:F1} · Gaze {state.gaze} · Wink {state.wink}\n" +
                $"Pulse: lean {state.leanTrigger} / kiai {state.kiai} / nod {state.nod} / shake {state.shake}", small);
            GUI.Label(new Rect(30, height - 38, width - 60, 30), "WASD footwork · Mouse look · Esc release     |     Combat arrives in milestone 2", small);
            if (entered && Cursor.lockState == CursorLockMode.Locked)
            {
                GUI.Label(new Rect(width / 2 - 4, height / 2 - 10, 20, 25), "·", heading);
                return;
            }
            float left = (width - 560) / 2;
            GUI.Box(new Rect(left - 25, 140, 610, 425), GUIContent.none);
            GUI.Label(new Rect(left, 165, 550, 50), "Before the first strike.", new GUIStyle(heading) { fontSize = 34 });
            GUI.Label(new Rect(left, 225, 540, 80), "A quiet place to find your footing.\nExplore the practice ground. No webcam required.", small);
            if (GUI.Button(new Rect(left, 310, 540, 48), ready ? "Enter the practice ground" : "Starting input…") && ready) SetCapture(true);
            GUI.Label(new Rect(left, 380, 540, 115), "Mock face controls (readout only):\nQ / E lean · F kiai · Y / N nod / shake\nHold C low composure · Z / X / V gaze · G wink\n\nMouse and keyboard required. Click Game view first.", small);
            if (!string.IsNullOrEmpty(inputError)) GUI.Label(new Rect(left, 510, 540, 50), inputError, small);
        }
    }
}
