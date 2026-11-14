using System;
using UnityEngine;

namespace Ganryujima.World
{
    /// <summary>Resampled Quaternius CC0 motion. Gameplay clocks own phase; no animation root motion owns collision.</summary>
    public static class SwordMotion
    {
        [Serializable] public sealed class Library { public Clip[] clips; }
        [Serializable] public sealed class Clip { public string name; public float duration; public Frame[] frames; }
        [Serializable] public struct Frame { public Vector3 hand; public Quaternion rotation, torso; }
        private static Library library;
        public static bool Available => GetLibrary() != null;
        private static Library GetLibrary()
        {
            if (library == null)
            {
                var asset = Resources.Load<TextAsset>("Animation/SwordMotions");
                if (asset != null) library = JsonUtility.FromJson<Library>(asset.text);
            }
            return library;
        }
        public static Frame Sample(string name, float phase)
        {
            var data = GetLibrary();
            if (data != null) foreach (var clip in data.clips)
            {
                if (clip.name != name) continue;
                float frame = Mathf.Clamp01(phase) * (clip.frames.Length - 1);
                int a = Mathf.FloorToInt(frame), b = Mathf.Min(a + 1, clip.frames.Length - 1);
                float blend = frame - a;
                return new Frame { hand = Vector3.Lerp(clip.frames[a].hand, clip.frames[b].hand, blend),
                    rotation = Quaternion.Slerp(clip.frames[a].rotation, clip.frames[b].rotation, blend),
                    torso = Quaternion.Slerp(clip.frames[a].torso, clip.frames[b].torso, blend) };
            }
            return new Frame { rotation = Quaternion.identity, torso = Quaternion.identity };
        }
    }
}
