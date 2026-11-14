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
            void Check()
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
    }
}
