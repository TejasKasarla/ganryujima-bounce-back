using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Ganryujima.Core;
using Ganryujima.Combat;
using Ganryujima.World;
using Ganryujima.Input.Face;

namespace Ganryujima.Editor
{
    public static class PolishPlayChecks
    {
        public static void Capture()
        {
            PolishSetup.Prepare();
            CombatChecks.Verify(); OpponentChecks.Verify(); FaceGameplayChecks.Verify(); FinisherChecks.Verify();
            StoryChecks.Verify(); MeditationChecks.Verify(); ContentChecks.Verify();
            ProjectSetup.CreateScene(); SessionState.SetBool("GanryujimaPolishCapture", true); EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool("GanryujimaPolishCapture", false)) return;
            double deadline = EditorApplication.timeSinceStartup + 100;
            EditorApplication.update += Check;
            async void Check()
            {
                var game = UnityEngine.Object.FindFirstObjectByType<GameLoop>();
                if (EditorApplication.isPlaying && game != null && game.FramesTicked >= 20)
                {
                    EditorApplication.update -= Check; SessionState.SetBool("GanryujimaPolishCapture", false);
                    try { await Verify(game); Debug.Log("POLISH INTEGRATION PASS: pause freezes combat/story/meditation/finisher, face ticks continue, preferences round-trip, sourced audio and bounded particles exist, post FX supported."); EditorApplication.Exit(0); }
                    catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
                }
                else if (EditorApplication.timeSinceStartup > deadline) { Debug.LogError("Polish capture timeout"); EditorApplication.Exit(1); }
            }
        }
        private static async Task Verify(GameLoop game)
        {
            void Assert(bool value, string message) { if (!value) throw new Exception(message); }
            string output = Environment.GetEnvironmentVariable("GANRYUJIMA_CAPTURE_DIR") ?? Path.GetFullPath("work/polish-captures"); Directory.CreateDirectory(output);
            Assert(game.Audio.HasLicensedClips, "Licensed atmosphere/clash clips missing");
            Assert(game.Audio.HasBattleMusic, "CC0 battle soundtrack missing");
            var particles = UnityEngine.Object.FindFirstObjectByType<ArenaParticles>(); Assert(particles != null && particles.Capacity == 72, "Particle pool missing or unbounded");
            game.BeginJourney(); game.SetPaused(true);
            float age = game.Story.Age; int frames = game.FramesTicked;
            await Task.Delay(160);
            Assert(game.Paused && AudioListener.pause && game.Story.Age == age && game.FramesTicked > frames, "Story pause or live face loop failed");
            game.SetPaused(false); ContentChecks.AdvanceScenes(game.Story); game.Story.Choose(0); ContentChecks.AdvanceScenes(game.Story);
            game.SetPaused(true); age = game.Opponent.StateAge;
            game.Combat.BeginGuard(); game.Combat.EndGuard(); float health = game.Combat.Health, stamina = game.Combat.Stamina;
            await Task.Delay(160);
            Assert(game.Opponent.StateAge == age && game.Combat.Health == health && game.Combat.Stamina == stamina, "Combat advanced while paused");
            game.SetPaused(false); game.Story.ResolveDuel(false, 1); ContentChecks.AdvanceScenes(game.Story); game.Story.Choose(0);
            Assert(game.Story.Current.kind == "meditation", "Meditation route unavailable");
            game.Meditation.Tick(.3f, new FaceState { tracking = true, calibrated = true, composure = 1 }, false);
            game.SetPaused(true); float calm = game.Meditation.CalmSeconds;
            await Task.Delay(160); Assert(game.Meditation.CalmSeconds == calm, "Healing advanced while paused");
            game.BeginJourney(); ContentChecks.AdvanceScenes(game.Story); game.Story.Choose(0); ContentChecks.AdvanceScenes(game.Story);
            game.SetPaused(true);
            for (int i = 0; i < 3; i++) { game.Combat.Tick(3, 2, 0); game.Combat.BeginCharge(); game.Combat.Tick(.56f, 2, 0); game.Combat.ReleaseStrike(); game.Combat.Tick(.9f, 2, 0); }
            Assert(game.Finisher.TryBegin(game.Combat), "Cannot start test finisher");
            game.SetPaused(true); float elapsed = game.Finisher.Timeline.Elapsed;
            await Task.Delay(160); Assert(game.Finisher.Paused && game.Finisher.Timeline.Elapsed == elapsed, "Cinematic advanced while paused");
            game.BeginJourney(); Assert(!game.Finisher.Active && !game.Paused && !AudioListener.pause, "New journey did not clear paused cinematic");
            float originalVolume = GameSettings.Volume, originalSensitivity = GameSettings.Sensitivity; int originalQuality = GameSettings.Quality;
            GameSettings.Volume = .37f; GameSettings.Sensitivity = 3.1f; GameSettings.Quality = 0; GameSettings.Save(); GameSettings.Load();
            Assert(Mathf.Approximately(GameSettings.Volume, .37f) && Mathf.Approximately(AudioListener.volume, .37f) && GameSettings.Sensitivity == 3.1f && QualitySettings.shadowDistance == 30, "Saved settings round-trip failed");
            GameSettings.Volume = originalVolume; GameSettings.Sensitivity = originalSensitivity; GameSettings.Quality = originalQuality; GameSettings.Save();
            Assert(Resources.Load<Shader>("Shaders/InkAtmosphere").isSupported && Resources.Load<Shader>("Shaders/DriftingPetal").isSupported, "Polish shader unsupported");
            ContentChecks.AdvanceScenes(game.Story); game.Story.Choose(0); ContentChecks.AdvanceScenes(game.Story); game.SetPaused(true);
            var camera = Camera.main; camera.transform.position = new Vector3(0, GameConfig.EyeHeight, -.6f); camera.transform.rotation = Quaternion.Euler(5, 0, 0);
            VisualCapture.Save(camera, Path.Combine(output, "arima-polish.png"));
            game.Story.ResolveDuel(true, 2); ContentChecks.AdvanceScenes(game.Story); game.Story.Choose(0); ContentChecks.AdvanceScenes(game.Story); game.Story.Choose(2); ContentChecks.AdvanceScenes(game.Story);
            game.Story.ResolveDuel(true, 3); ContentChecks.AdvanceScenes(game.Story); game.Story.Choose(0); ContentChecks.AdvanceScenes(game.Story); game.Story.Choose(0); ContentChecks.AdvanceScenes(game.Story);
            game.SetPaused(false); await Task.Delay(700); game.SetPaused(true);
            Assert(game.Audio.SeaMix > 0, "Beach ambience transition failed");
            camera.transform.position = new Vector3(0, GameConfig.EyeHeight, -.6f); camera.transform.rotation = Quaternion.Euler(5, 0, 0);
            VisualCapture.Save(camera, Path.Combine(output, "kojiro-polish.png"));
        }
    }
}
