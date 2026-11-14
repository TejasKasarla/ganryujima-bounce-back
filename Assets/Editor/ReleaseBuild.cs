using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Ganryujima.Editor
{
    public static class ReleaseBuild
    {
        [MenuItem("Ganryujima/Build Windows demo")]
        public static void Windows()
        {
            ContentSetup.Prepare();
            PolishSetup.Prepare();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { ProjectSetup.ScenePath };
            string output = Path.GetFullPath(Environment.GetEnvironmentVariable("GANRYUJIMA_BUILD_DIR") ?? "Builds/Windows");
            Directory.CreateDirectory(output);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = UnityEngine.FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true; // Face bridge stays alive when its companion tracker is focused.
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = scenes, locationPathName = Path.Combine(output, "Ganryujima.exe"),
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
            File.WriteAllText(Path.Combine(output, "Play with webcam.cmd"), "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"Ganryujima.exe\" --camera\r\n");
            File.WriteAllText(Path.Combine(output, "READ ME.txt"),
                "GANRYUJIMA: BOUNCE BACK\r\n\r\nRun Ganryujima.exe for keyboard/mouse play.\r\nRun Play with webcam.cmd for the local companion face tracker; start its camera, allow permission and calibrate. F1 shows the preview.\r\n\r\nWASD move; mouse look/stance; LMB strike (hold for heavy); RMB guard/parry; 1-4 stance.\r\nQ/E dodge, F kiai, Space retreat/advance, 1-3 story choices, Y/N binary, Esc pause/settings, R restart duel.\r\nMeditation: relax your face for five seconds; in keyboard mode release C to be calm.\r\n\r\nKeep this whole folder together, including Ganryujima_Data and GanryujimaFace.\r\nSee CREDITS.md for third-party assets. The start screen remains Dev A's pending integration.\r\n");
            File.Copy("README.md", Path.Combine(output, "PROJECT README.md"), true);
            File.Copy("CREDITS.md", Path.Combine(output, "CREDITS.md"), true);
            foreach (string directory in new[] { "Documentation/Licenses", "Assets/Art/Samurai", "Assets/Resources/Art/Coast", "Assets/Resources/Audio/Finisher", "Assets/Resources/Audio/Meditation", "Assets/Resources/Audio/Atmosphere", "Assets/Resources/Audio/Battle" })
            {
                string creditFolder = Path.Combine(output, "Credits", directory.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(creditFolder);
                foreach (string file in Directory.GetFiles(directory))
                {
                    string name = Path.GetFileName(file);
                    if (!name.EndsWith(".meta") && (name.EndsWith(".txt") || name.EndsWith(".md") || name.EndsWith(".json")))
                        File.Copy(file, Path.Combine(creditFolder, name), true);
                }
            }
            UnityEngine.Debug.Log($"GANRYUJIMA WINDOWS BUILD PASS: {report.summary.totalSize / (1024 * 1024)} MB at {output}");
        }
    }
}
