using UnityEditor;
using UnityEngine;

namespace Ganryujima.Editor
{
    public static class PolishSetup
    {
        public static void Prepare()
        {
            foreach (string name in new[] { "Wind", "Ocean", "Clash" })
            {
                string path = "Assets/Resources/Audio/Atmosphere/" + name + ".mp3";
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = .8f;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }
    }
}
