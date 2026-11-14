using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Ganryujima.Core;

namespace Ganryujima.Editor
{
    /// <summary>Captures actual Unity camera renders, not concept art.</summary>
    public static class VisualCapture
    {
        public static void Run()
        {
            CombatChecks.Verify();
            OpponentChecks.Verify();
            ProjectSetup.CreateScene();
            SessionState.SetBool("GanryujimaCapture", true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool("GanryujimaCapture", false)) return;
            double deadline = EditorApplication.timeSinceStartup + 100;
            EditorApplication.update += Check;
            async void Check()
            {
                var game = UnityEngine.Object.FindFirstObjectByType<GameLoop>();
                if (EditorApplication.isPlaying && game != null && game.FramesTicked >= 20)
                {
                    EditorApplication.update -= Check;
                    SessionState.SetBool("GanryujimaCapture", false);
                    try
                    {
                        string directory = Environment.GetEnvironmentVariable("GANRYUJIMA_CAPTURE_DIR") ?? Path.GetFullPath("work/captures");
                        Directory.CreateDirectory(directory);
                        var camera = Camera.main;
                        var atmosphere = camera.GetComponent<Ganryujima.World.InkAtmosphere>();
                        if (atmosphere != null) atmosphere.enabled = false;
                        Save(camera, Path.Combine(directory, "ungraded.png"));
                        if (atmosphere != null) atmosphere.enabled = true;
                        Save(camera, Path.Combine(directory, "court.png"));
                        camera.transform.position = new Vector3(0, GameConfig.EyeHeight, -0.5f);
                        camera.transform.rotation = Quaternion.Euler(6, 0, 0);
                        Save(camera, Path.Combine(directory, "duel.png"));
                        await CaptureOpponentPoses(game, camera, directory);
                        Debug.Log("GANRYUJIMA VISUAL CAPTURE PASS: " + directory);
                        EditorApplication.Exit(0);
                    }
                    catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
                }
                if (EditorApplication.timeSinceStartup > deadline)
                {
                    SessionState.SetBool("GanryujimaCapture", false);
                    Debug.LogError("Visual capture timed out.");
                    EditorApplication.Exit(1);
                }
            }
        }

        private static void Save(Camera camera, string path)
        {
            var output = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            output.Create();
            camera.targetTexture = output;
            camera.aspect = 1600f / 900;
            camera.Render();
            RenderTexture.active = output;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.Destroy(image);
            output.Release();
            UnityEngine.Object.Destroy(output);
        }

        private static async System.Threading.Tasks.Task CaptureOpponentPoses(GameLoop game, Camera camera, string directory)
        {
            var rig = UnityEngine.Object.FindFirstObjectByType<Ganryujima.World.SamuraiRig>();
            if (rig == null || rig.Model == null || rig.Walk == null) throw new Exception("Imported samurai rig or walk clip missing.");
            var ai=game.Opponent; var combat=game.Combat;
            void Step()
            {
                combat.Tick(.02f,ai.Position.magnitude,0);
                ai.Tick(.02f,Vector2.zero,0);
                rig.transform.position=new Vector3(ai.Position.x,0,ai.Position.y);
                rig.transform.rotation=Quaternion.LookRotation(new Vector3(ai.Forward.x,0,ai.Forward.y));
                rig.Pose(ai,.02f);
            }
            void Until(Ganryujima.Combat.OpponentState state)
            {
                int limit=1000;
                while(ai.State!=state && --limit>0) Step();
                if(limit<=0) throw new Exception("Pose capture could not reach "+state);
            }
            combat.Reset(); ai.Reset();
            // Focus these captures on skin deformation, without inactive presentation timers leaving sparks visible.
            GameObject.Find("Clash Sparks (pooled)").SetActive(false);
            Until(Ganryujima.Combat.OpponentState.Circle);
            for(int i=0;i<20;i++) Step();
            await System.Threading.Tasks.Task.Delay(100); // Let Unity update skinned-renderer matrices before each pose render.
            Save(camera,Path.Combine(directory,"walk.png"));
            Until(Ganryujima.Combat.OpponentState.Windup);
            for(int i=0;i<35;i++) Step();
            await System.Threading.Tasks.Task.Delay(100);
            Save(camera,Path.Combine(directory,"windup.png"));
            Until(Ganryujima.Combat.OpponentState.Strike);
            combat.Stance=ai.Zone; combat.BeginGuard();
            Until(Ganryujima.Combat.OpponentState.Counter);
            await System.Threading.Tasks.Task.Delay(100);
            Save(camera,Path.Combine(directory,"counter.png"));
            Debug.Log("SAMURAI POSE CHECK PASS: imported model, walk, windup, parry and counter render.");
        }
    }
}
