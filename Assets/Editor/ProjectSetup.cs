using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ganryujima.Core;

namespace Ganryujima.Editor
{
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/PracticeGround.unity";

        [MenuItem("Ganryujima/Create practice scene")]
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Resources/Materials");
            AssetDatabase.Refresh();
            EnsureMaterial("Surface", "Standard");
            EnsureMaterial("Flat", "Unlit/Color");
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Ganryujima Game").AddComponent<GameLoop>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "Ganryujima";
            PlayerSettings.productName = "Ganryujima - Bounce Back";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            AssetDatabase.SaveAssets();
            Debug.Log("GANRYUJIMA: practice scene created.");
        }

        private static void EnsureMaterial(string name, string shaderName)
        {
            string path = "Assets/Resources/Materials/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
                AssetDatabase.CreateAsset(new Material(Shader.Find(shaderName)), path);
        }

        public static void Smoke()
        {
            CreateScene();
            SessionState.SetBool("GanryujimaSmoke", true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void ResumeSmoke()
        {
            if (!SessionState.GetBool("GanryujimaSmoke", false)) return;
            double deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update += Check;
            void Check()
            {
                if (EditorApplication.isPlaying)
                {
                    var game = Object.FindFirstObjectByType<GameLoop>();
                    if (game != null && game.FramesTicked >= 10)
                    {
                        if (Camera.main == null || Object.FindFirstObjectByType<CharacterController>() == null)
                            Finish(1, "Missing first-person components.");
                        else Finish(0, "GANRYUJIMA SMOKE PASS: Play Mode boots, camera/controller exist, face loop ticks 10 frames.");
                    }
                }
                if (EditorApplication.timeSinceStartup > deadline) Finish(1, "Smoke timeout: game did not tick.");
            }
            void Finish(int code, string message)
            {
                SessionState.SetBool("GanryujimaSmoke", false);
                EditorApplication.update -= Check;
                Debug.Log(message);
                EditorApplication.Exit(code);
            }
        }
    }
}
