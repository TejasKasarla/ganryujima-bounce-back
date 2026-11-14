using System;
using UnityEngine;
using Ganryujima.Input.Face;
using Ganryujima.World;
using Ganryujima.Combat;
using Ganryujima.UI;
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
        private Transform target;
        private CombatModel combat;
        private ArimaOpponent opponent;
        private ArimaArt opponentArt;
        public ArimaOpponent Opponent => opponent;
        private CombatPresentation presentation;
        private FaceGameplay faceGameplay;
        private InkAtmosphere atmosphere;
        private FinishingBlow finisher;
        public FinishingBlow Finisher => finisher;
        public FaceGameplay FaceGameplay => faceGameplay;
        private bool ready, entered, debugHud;
        private float yaw, pitch, verticalVelocity;
        private Vector2 stanceMotion;
        private string inputError;
        public int FramesTicked { get; private set; }
        public CombatModel Combat => combat;
        private bool Captured => entered && Cursor.lockState == CursorLockMode.Locked && Application.isFocused;
        private float Distance => target == null || player == null ? 0 : Vector2.Distance(
            new Vector2(player.transform.position.x, player.transform.position.z), new Vector2(target.position.x, target.position.z));
        private float AimAngle => Vector3.Angle(view.transform.forward, target.position + Vector3.up * 1.3f - view.transform.position);

        private async void Awake()
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
            Transform arena = ArenaBuilder.Build();
            arena.Find("Practice Target").gameObject.SetActive(false);
            opponentArt = new ArimaArt(arena);
            target = opponentArt.Root;
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
            view.farClipPlane = 320;
            view.clearFlags = CameraClearFlags.Skybox;
            view.allowHDR = true;
            atmosphere = view.gameObject.AddComponent<InkAtmosphere>();
            view.backgroundColor = new Color(0.82f, 0.81f, 0.75f);
            view.gameObject.AddComponent<AudioListener>();
            Transform sword = ArenaBuilder.BuildKatana(view.transform);
            combat = new CombatModel();
            opponent = new ArimaOpponent(combat);
            faceGameplay = new FaceGameplay(combat, opponent);
            opponentArt.Tick(opponent, 1);
            presentation = new CombatPresentation(combat, view, sword, target);
            finisher = new FinishingBlow(view,sword,target,presentation);
            face = FaceInputFactory.createFaceInput();
            try { await face.start(); }
            catch (Exception error) { inputError = "Face provider failed: " + error.Message; Debug.LogException(error); }
            if (this == null) { face.stop(); return; }
            ready = true;
            SetCapture(false);
        }

        private void Update()
        {
            // Unity cannot serialize interface/model instances across a script reload in Play Mode.
            // Keep a stale session quiet until Play is restarted rather than spamming exceptions.
            if (!ready || face == null || combat == null || opponent == null || presentation == null || faceGameplay == null || finisher == null) return;
            face.tick(); // First gameplay action, once per frame, including paused/released frames.
            state = face.getState();
            FramesTicked++;
            if (finisher.Active)
            {
                faceGameplay.Observe(state,false);
                if (KeyInput.GetKeyDown(KeyCode.Escape)) finisher.TogglePause();
                bool focused=Application.isFocused;
                if (!focused && !finisher.Paused) finisher.TogglePause();
                finisher.Tick(Mathf.Min(Time.unscaledDeltaTime,GameConfig.MaximumDelta),focused && KeyInput.GetKeyDown(KeyCode.Space));
                if (!finisher.Active) SetCapture(false);
                return;
            }
            if (KeyInput.GetKeyDown(KeyCode.F2)) debugHud = !debugHud; // F1 belongs to Dev A's camera diagnostics.
            if (KeyInput.GetKeyDown(KeyCode.Escape)) SetCapture(false);
            if (KeyInput.GetKeyDown(KeyCode.R)) Restart();
            faceGameplay.Observe(state, Captured);
            atmosphere.SetComposure(combat.Composure);
            if (!Captured) return; // Freeze combat and opponent timers together; no global timeScale.
            float realDt = Mathf.Min(Time.unscaledDeltaTime, GameConfig.MaximumDelta);
            faceGameplay.Advance(realDt, Distance, AimAngle);
            float dt = presentation.Stopped ? 0 : realDt * faceGameplay.LocalTimeScale;
            if (dt > 0 && !combat.Over)
            {
                ReadLookAndCombat(dt);
                faceGameplay.ApplyGestures(true, Distance, AimAngle);
                MovePlayer(dt);
                combat.Tick(dt, Distance, AimAngle);
                opponent.Tick(dt, new Vector2(player.transform.position.x, player.transform.position.z), AimAngle);
                opponentArt.Tick(opponent, dt);
                if (finisher.TryBegin(combat)) { SetCapture(false); return; }
            }
            presentation.Tick(realDt, dt);
            view.transform.localPosition = Vector3.up * GameConfig.EyeHeight + presentation.CameraOffset + Vector3.right * (faceGameplay.Lean * GameConfig.LeanCameraOffset);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, -faceGameplay.Lean * GameConfig.LeanCameraRoll);
            if (combat.Over) SetCapture(false);
        }

        private void ReadLookAndCombat(float dt)
        {
            var mouse = new Vector2(KeyInput.GetAxisRaw("Mouse X"), KeyInput.GetAxisRaw("Mouse Y"));
            yaw += mouse.x * GameConfig.MouseSensitivity;
            pitch = Mathf.Clamp(pitch - mouse.y * GameConfig.MouseSensitivity, -GameConfig.MaximumPitch, GameConfig.MaximumPitch);
            player.transform.rotation = Quaternion.Euler(0, yaw, 0);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            stanceMotion = Vector2.Lerp(stanceMotion, Vector2.zero, 1 - Mathf.Exp(-GameConfig.StanceMouseDecay * dt));
            stanceMotion += mouse;
            if (stanceMotion.magnitude >= GameConfig.StanceMouseThreshold)
            {
                combat.Stance = Mathf.Abs(stanceMotion.y) > Mathf.Abs(stanceMotion.x)
                    ? (stanceMotion.y > 0 ? Stance.Upper : Stance.Lower)
                    : (stanceMotion.x > 0 ? Stance.Side : Stance.Middle);
                stanceMotion = Vector2.zero;
            }
            if (KeyInput.GetKeyDown(KeyCode.Alpha1)) combat.Stance = Stance.Upper;
            if (KeyInput.GetKeyDown(KeyCode.Alpha2)) combat.Stance = Stance.Middle;
            if (KeyInput.GetKeyDown(KeyCode.Alpha3)) combat.Stance = Stance.Lower;
            if (KeyInput.GetKeyDown(KeyCode.Alpha4)) combat.Stance = Stance.Side;
            // Releases are reconciled from held state too, so a release during hit-stop cannot stick.
            if (combat.Guarding && !KeyInput.GetMouseButton(1)) combat.EndGuard();
            if (combat.Charging && !KeyInput.GetMouseButton(0)) combat.ReleaseStrike();
            if (KeyInput.GetMouseButtonDown(1)) combat.BeginGuard();
            if (KeyInput.GetMouseButtonDown(0)) combat.BeginCharge();
            if (KeyInput.GetKeyDown(KeyCode.Space)) combat.TryRetreat();
        }

        private void MovePlayer(float dt)
        {
            float x = (KeyInput.GetKey(KeyCode.D) ? 1 : 0) - (KeyInput.GetKey(KeyCode.A) ? 1 : 0);
            float z = (KeyInput.GetKey(KeyCode.W) ? 1 : 0) - (KeyInput.GetKey(KeyCode.S) ? 1 : 0);
            var direction = Vector3.ClampMagnitude(new Vector3(x, 0, z), 1);
            Vector3 velocity = combat.DodgeRemaining > 0 ? player.transform.right * (combat.DodgeDirection * GameConfig.DodgeSpeed)
                : combat.RetreatRemaining > 0 ? -player.transform.forward * GameConfig.RetreatSpeed
                : player.transform.TransformDirection(direction) * (GameConfig.WalkSpeed * combat.MovementScale);
            var horizontal = velocity * dt;
            var next = player.transform.position + horizontal;
            var limited = Vector2.ClampMagnitude(new Vector2(next.x, next.z), GameConfig.ArenaRadius - GameConfig.PlayerRadius);
            Vector2 separation = limited - opponent.Position;
            float minimum = GameConfig.PlayerRadius + GameConfig.ArimaRadius;
            if (separation.magnitude < minimum)
            {
                if (separation.sqrMagnitude < 0.0001f) separation = -opponent.Forward;
                limited = opponent.Position + separation.normalized * minimum;
                limited = Vector2.ClampMagnitude(limited, GameConfig.ArenaRadius - GameConfig.PlayerRadius);
            }
            horizontal.x = limited.x - player.transform.position.x;
            horizontal.z = limited.y - player.transform.position.z;
            if (player.isGrounded && verticalVelocity < 0) verticalVelocity = GameConfig.GroundedVelocity;
            verticalVelocity += GameConfig.Gravity * dt;
            player.Move(horizontal + Vector3.up * (verticalVelocity * dt));
        }

        private void Enter() { if (ready && !combat.Over) SetCapture(true); }
        private void Restart()
        {
            if (!ready) return;
            finisher.Reset();
            combat.Reset(); opponent.Reset(); opponentArt.Tick(opponent, 1); presentation.Reset();
            faceGameplay.Reset();
            player.enabled = false;
            player.transform.SetPositionAndRotation(new Vector3(0, 0, GameConfig.SpawnDistance), Quaternion.identity);
            player.enabled = true;
            yaw = pitch = verticalVelocity = 0;
            stanceMotion = Vector2.zero;
            view.transform.localPosition = Vector3.up * GameConfig.EyeHeight;
            view.transform.localRotation = Quaternion.identity;
            SetCapture(true);
        }

        private void SetCapture(bool capture)
        {
            entered = capture;
            if (!capture) { combat?.CancelHeldInput(); faceGameplay?.Observe(state, false); stanceMotion = Vector2.zero; }
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
        }
        private void OnApplicationFocus(bool focus) { if (!focus) SetCapture(false); }
        private void OnDisable() { SetCapture(false); }
        private void OnDestroy() { face?.stop(); faceGameplay?.Dispose(); opponent?.Dispose(); presentation?.Dispose(); }
        private void OnGUI()
        {
            if (ready && (face == null || combat == null || presentation == null || faceGameplay == null || finisher == null))
            {
                GUI.Box(new Rect(25, 25, 590, 60), "Scripts changed during Play. Stop Play, then press Play again to reload the court.");
                return;
            }
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            try
            {
                GUI.depth = 10; // Keep Dev A's corner webcam overlay above the gameplay/entry panels.
                if (finisher != null && finisher.Active) { FinisherHud.Draw(finisher); return; }
                CombatHud.Draw(combat, opponent, state, ready, Captured, Distance, Enter, Restart, inputError, debugHud, faceGameplay);
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth; }
        }
    }
}
