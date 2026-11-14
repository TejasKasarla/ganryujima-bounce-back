using UnityEngine;

namespace Ganryujima.Core
{
    /// <summary>Presentation preferences only; combat tuning and face thresholds stay in their configs.</summary>
    public static class GameSettings
    {
        public static float Volume = .8f, Ambience = .65f, Music = .45f, Sensitivity = GameConfig.MouseSensitivity;
        public static bool Shake = true, PostEffects = true, Particles = true;
        public static int Quality = 1;
        public static void Load()
        {
            Volume = PlayerPrefs.GetFloat("GBB.Volume", .8f);
            Ambience = PlayerPrefs.GetFloat("GBB.Ambience", .65f);
            Music = PlayerPrefs.GetFloat("GBB.Music", .45f);
            Sensitivity = PlayerPrefs.GetFloat("GBB.Sensitivity", GameConfig.MouseSensitivity);
            Shake = PlayerPrefs.GetInt("GBB.Shake", 1) != 0;
            PostEffects = PlayerPrefs.GetInt("GBB.PostEffects", 1) != 0;
            Particles = PlayerPrefs.GetInt("GBB.Particles", 1) != 0;
            Quality = PlayerPrefs.GetInt("GBB.Quality", 1);
            Apply();
        }
        public static void Apply()
        {
            Volume = Mathf.Clamp01(Volume); Ambience = Mathf.Clamp01(Ambience); Music = Mathf.Clamp01(Music);
            Sensitivity = Mathf.Clamp(Sensitivity, .4f, 5);
            Quality = Mathf.Clamp(Quality, 0, 2);
            AudioListener.volume = Volume;
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
            QualitySettings.shadows = Quality == 0 ? ShadowQuality.HardOnly : ShadowQuality.All;
            QualitySettings.shadowResolution = Quality == 2 ? ShadowResolution.High : ShadowResolution.Medium;
            QualitySettings.shadowDistance = Quality == 0 ? 30 : Quality == 1 ? 48 : VisualConfig.ShadowDistance;
            QualitySettings.shadowCascades = Quality == 2 ? 4 : 2;
            QualitySettings.antiAliasing = Quality == 0 ? 0 : Quality == 1 ? 2 : 4;
            QualitySettings.pixelLightCount = Quality == 0 ? 1 : 2;
        }
        public static void Save()
        {
            Apply();
            PlayerPrefs.SetFloat("GBB.Volume", Volume); PlayerPrefs.SetFloat("GBB.Ambience", Ambience);
            PlayerPrefs.SetFloat("GBB.Music", Music); PlayerPrefs.SetFloat("GBB.Sensitivity", Sensitivity);
            PlayerPrefs.SetInt("GBB.Shake", Shake ? 1 : 0); PlayerPrefs.SetInt("GBB.PostEffects", PostEffects ? 1 : 0);
            PlayerPrefs.SetInt("GBB.Particles", Particles ? 1 : 0); PlayerPrefs.SetInt("GBB.Quality", Quality);
            PlayerPrefs.Save();
        }
    }
}
