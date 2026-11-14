#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Ganryujima.Input.Face.Editor
{
    [InitializeOnLoad]
    public static class FaceTools
    {
        private const string CameraMenu = "Ganryujima/Face/Use camera next Play";
        private const string SessionKey = "Ganryujima.Face.Camera";
        private static FaceInput standalone;

        static FaceTools()
        {
            FaceInputFactory.CameraOverride = SessionState.GetBool(SessionKey, false);
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.quitting += Stop;
            EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.ExitingEditMode) Stop(); };
        }

        [MenuItem(CameraMenu)]
        private static void ToggleCamera()
        {
            FaceInputFactory.CameraOverride = !FaceInputFactory.CameraOverride;
            SessionState.SetBool(SessionKey, FaceInputFactory.CameraOverride);
        }

        [MenuItem(CameraMenu, true)]
        private static bool ValidateCamera()
        {
            Menu.SetChecked(CameraMenu, FaceInputFactory.CameraOverride);
            return !EditorApplication.isPlaying;
        }

        [MenuItem("Ganryujima/Face/Open standalone test")]
        private static async void OpenTest()
        {
            FaceInput active = FaceInputFactory.CameraOf(FaceInputFactory.Active);
            if (active != null && active.IsRunning) { Application.OpenURL(active.Address); return; }
            Stop();
            standalone = new FaceInput();
            await standalone.start();
            if (!standalone.IsRunning) { Debug.LogWarning(standalone.Error); Stop(); return; }
            EditorApplication.update += Tick;
        }

        [MenuItem("Ganryujima/Face/Stop standalone test")]
        private static void Stop()
        {
            EditorApplication.update -= Tick;
            standalone?.stop();
            standalone = null;
        }

        private static void Tick() => standalone?.tick();

        [PostProcessBuild(100)]
        private static void CopyLocalAssets(BuildTarget target, string location)
        {
            if (target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneWindows64 && target != BuildTarget.StandaloneLinux64 && target != BuildTarget.StandaloneOSX) return;
            string source = Path.Combine(Application.dataPath, "Scripts", "Input", "Face");
            string destination = target == BuildTarget.StandaloneOSX ? Path.Combine(location, "Contents", "GanryujimaFace") : Path.Combine(Path.GetDirectoryName(location), "GanryujimaFace");
            Directory.CreateDirectory(destination);
            foreach (string folder in new[] { "Web", "Models" })
            {
                string path = Path.Combine(source, folder);
                if (!Directory.Exists(path)) continue;
                foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                    string relative = file.Substring(source.Length + 1);
                    string output = Path.Combine(destination, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(output));
                    File.Copy(file, output, true);
                }
            }
            string page = Path.Combine(Path.GetDirectoryName(Application.dataPath), "public", "face-test.html");
            if (File.Exists(page)) File.Copy(page, Path.Combine(destination, "face-test.html"), true);
            if (!File.Exists(Path.Combine(destination, "Models", "face_landmarker.task")))
                Debug.LogWarning("Face model not packaged. Run Assets/Scripts/Input/Face/Tools/setup-face.ps1 before building camera mode.");
        }
    }
}
#endif