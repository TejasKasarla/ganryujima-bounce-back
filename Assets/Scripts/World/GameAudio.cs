using System;
using UnityEngine;
using Ganryujima.Core;
using Ganryujima.Combat;

namespace Ganryujima.World
{
    /// <summary>Ambient loops, fight music and combat sounds.</summary>
    public sealed class GameAudio : IDisposable
    {
        private readonly GameObject root;
        private readonly GeneratedResources owner;
        private readonly CombatModel combat;
        private readonly AudioSource wind, sea, effects, music, feet, battle;
        private readonly AudioClip clash, slash, taiko, flute, kiai, step, thud;
        private readonly System.Random random = new System.Random(1712);
        private float seaMix, seaTarget, noteTimer = 7, stepDistance, drumCooldown;
        private Vector3 lastPosition;
        private bool havePosition, battleWanted;
        private OpponentState lastState;
        public bool HasLicensedClips => wind.clip != null && sea.clip != null && clash != null;
        public float SeaMix => seaMix;
        public bool HasBattleMusic => battle.clip != null;

        public GameAudio(Transform parent, CombatModel model)
        {
            combat = model;
            root = new GameObject("Soundscape / wind, sea, steel and breath");
            root.transform.SetParent(parent, false);
            owner = root.AddComponent<GeneratedResources>();
            wind = Source("Wind");
            sea = Source("Sea");
            effects = Source("Combat cues");
            music = Source("Taiko and flute");
            feet = Source("Footwork");
            battle = Source("CC0 live taiko battle loop");
            battle.volume = 0;
            battle.loop = true;
            battle.clip = MakeLoop(Resources.Load<AudioClip>("Audio/Battle/TaikoBattle"));
            battle.priority = 32;
            wind.clip = MakeLoop(Resources.Load<AudioClip>("Audio/Atmosphere/Wind"));
            sea.clip = MakeLoop(Resources.Load<AudioClip>("Audio/Atmosphere/Ocean"));
            wind.loop = sea.loop = true;
            wind.volume = sea.volume = 0;
            if (wind.clip != null) wind.Play();
            if (sea.clip != null) sea.Play();
            clash = Resources.Load<AudioClip>("Audio/Atmosphere/Clash");
            slash = Resources.Load<AudioClip>("Audio/Finisher/SwordSlash");
            taiko = Synth("Authored taiko", 1.5f, (t, n) => (Mathf.Sin(2 * Mathf.PI * (58 * t + 4 * (1 - Mathf.Exp(-22 * t)))) * .7f + n * Mathf.Exp(-t * 55) * .28f) * Mathf.Exp(-t * 4));
            flute = Synth("Authored breath flute", 3.2f, (t, n) =>
                (Mathf.Sin(2 * Mathf.PI * 293.66f * t + .018f * Mathf.Sin(2 * Mathf.PI * 4.6f * t)) * .34f
                + Mathf.Sin(2 * Mathf.PI * 587.32f * t) * .045f + n * .035f) * Mathf.Sin(Mathf.PI * t / 3.2f));
            kiai = Synth("Authored kiai breath", .55f, (t, n) =>
                (Mathf.Sin(2 * Mathf.PI * (132 * t - 26 * t * t)) * .22f + Mathf.Sin(2 * Mathf.PI * 740 * t) * .10f + n * .3f) * Mathf.Pow(Mathf.Sin(Mathf.PI * t / .55f), 2));
            step = Synth("Authored sand step", .16f, (t, n) => n * .4f * Mathf.Exp(-t * 34) + Mathf.Sin(2 * Mathf.PI * 95 * t) * .18f * Mathf.Exp(-t * 40));
            thud = Synth("Authored dull impact", .32f, (t, n) => (n * .3f + Mathf.Sin(2 * Mathf.PI * 82 * t) * .4f) * Mathf.Exp(-t * 19));
            combat.Impacted += OnImpact;
        }
        private AudioSource Source(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            return source;
        }
        private AudioClip Synth(string name, float seconds, Func<float, float, float> sample)
        {
            const int rate = 22050;
            var data = new float[(int)(seconds * rate)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Clamp01(t / .008f) * Mathf.Clamp01((seconds - t) / .015f);
                data[i] = Mathf.Clamp(sample(t, (float)random.NextDouble() * 2 - 1) * envelope, -.95f, .95f);
            }
            var clip = owner.Own(AudioClip.Create(name, data.Length, 1, rate, false));
            clip.SetData(data, 0);
            return clip;
        }
        private AudioClip MakeLoop(AudioClip original)
        {
            if (original == null) return null;
            original.LoadAudioData();
            var data = new float[original.samples * original.channels];
            if (!original.GetData(data, 0)) return original;
            // Remove codec padding before crossfading the loop seam.
            int channels = original.channels, first = 0, last = original.samples - 1;
            bool Quiet(int frame)
            {
                for (int c = 0; c < channels; c++)
                    if (Mathf.Abs(data[frame * channels + c]) > .0003f) return false;
                return true;
            }
            while (first < last && Quiet(first)) first++;
            while (last > first && Quiet(last)) last--;
            int count = last - first + 1, overlap = Mathf.Min(original.frequency / 3, count / 4), length = count - overlap;
            if (length <= 0) return original;
            var loop = new float[length * channels];
            for (int f = 0; f < length; f++)
                for (int c = 0; c < channels; c++)
                {
                    float value = data[(first + overlap + f) * channels + c];
                    if (f >= length - overlap)
                    {
                        int j = f - (length - overlap);
                        float mix = (float)j / overlap;
                        value = Mathf.Lerp(value, data[(first + j) * channels + c], mix);
                    }
                    loop[f * channels + c] = value;
                }
            var result = owner.Own(AudioClip.Create(original.name + " (crossfaded loop)", length, channels, original.frequency, false));
            result.SetData(loop, 0);
            return result;
        }
        public void SetScene(string arena, string kind)
        {
            seaTarget = arena.StartsWith("beach", StringComparison.Ordinal) ? 1 : .08f;
            noteTimer = kind == "ending" ? 1 : kind == "meditation" ? 5 : 10;
            if (kind == "duel")
            {
                music.pitch = .85f;
                music.PlayOneShot(taiko, GameSettings.Music * .55f);
            }
            battleWanted = kind == "duel";
            if (battleWanted && battle.clip != null)
            {
                battle.time = 0;
                battle.Play();
            }
            havePosition = false;
            stepDistance = 0;
        }
        public void Tick(float dt, bool walking, Vector3 position, ArimaOpponent opponent, bool narrative)
        {
            seaMix = Mathf.MoveTowards(seaMix, seaTarget, dt * .28f);
            wind.volume = GameSettings.Ambience * .45f;
            sea.volume = GameSettings.Ambience * seaMix * .8f;
            // Music belongs to the encounter, not cursor capture, movement or focus.
            bool fighting = battleWanted && !combat.Over;
            if (fighting && battle.clip != null && !battle.isPlaying && !AudioListener.pause) battle.Play();
            battle.volume = Mathf.MoveTowards(battle.volume, fighting ? GameSettings.Music * .9f : 0, dt);
            music.volume = 1;
            effects.volume = feet.volume = 1;
            noteTimer -= dt;
            drumCooldown = Mathf.Max(0, drumCooldown - dt);
            if (narrative && noteTimer <= 0)
            {
                noteTimer = 12 + (float)random.NextDouble() * 8;
                music.pitch = random.Next(2) == 0 ? 1 : .75f;
                music.PlayOneShot(flute, GameSettings.Music * .25f);
            }
            if (!narrative && walking && opponent.State != lastState && opponent.Telling && drumCooldown <= 0)
            {
                music.pitch = .9f;
                music.PlayOneShot(taiko, GameSettings.Music * .32f);
                drumCooldown = 5;
            }
            lastState = opponent.State;
            if (walking && havePosition)
            {
                stepDistance += Vector3.Distance(position, lastPosition);
                if (stepDistance >= .95f)
                {
                    stepDistance = 0;
                    feet.pitch = .9f + (float)random.NextDouble() * .2f;
                    feet.PlayOneShot(step, .32f);
                }
            }
            else stepDistance = 0;
            lastPosition = position;
            havePosition = true;
        }
        private void OnImpact(Impact impact)
        {
            effects.pitch = .92f + (float)random.NextDouble() * .16f;
            switch (impact)
            {
                case Impact.Parry: effects.pitch = 1.12f; Play(clash, 1); break;
                case Impact.Block: case Impact.TargetBlock: Play(clash, .75f); break;
                case Impact.TargetHit: case Impact.HeavyHit: case Impact.CounterHit: Play(slash, .46f); Play(thud, .35f); break;
                case Impact.PlayerHit: case Impact.GuardBreak: effects.pitch = .78f; Play(thud, .8f); break;
                case Impact.Kiai: effects.pitch = 1; Play(kiai, .85f); break;
                case Impact.Miss: effects.pitch = 1.18f; Play(slash, .17f); break;
                case Impact.Dodge: case Impact.Evade: Play(step, .28f); break;
            }
        }
        private void Play(AudioClip clip, float volume) { if (clip != null) effects.PlayOneShot(clip, volume); }
        public void Dispose() { combat.Impacted -= OnImpact; if (root != null) UnityEngine.Object.Destroy(root); }
    }
}
