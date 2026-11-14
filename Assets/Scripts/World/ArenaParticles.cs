using UnityEngine;
using Ganryujima.Core;

namespace Ganryujima.World
{
    /// <summary>A single bounded pool, suspended with gameplay; no per-frame instantiation.</summary>
    public sealed class ArenaParticles : MonoBehaviour
    {
        private ParticleSystem dust;
        private bool beach;
        public int Capacity => dust.main.maxParticles;
        public void Initialize()
        {
            var owner = gameObject.AddComponent<GeneratedResources>();
            var texture = owner.Own(new Texture2D(32, 32, TextureFormat.RGBA32, false));
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float dx = (x - 15.5f) / 15.5f, dy = (y - 15.5f) / 15.5f;
                float a = Mathf.Clamp01((1 - dx * dx - dy * dy) * 3);
                pixels[y * 32 + x] = new Color(1, 1, 1, a * .7f);
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            var material = owner.Own(new Material(Resources.Load<Shader>("Shaders/DriftingPetal"))); material.mainTexture = texture;
            dust = gameObject.AddComponent<ParticleSystem>(); dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main; main.loop = true; main.playOnAwake = false; main.duration = 12; main.startLifetime = new ParticleSystem.MinMaxCurve(7, 12);
            main.maxParticles = 72; main.startSpeed = .06f; main.startSize = new ParticleSystem.MinMaxCurve(.025f, .075f);
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.useUnscaledTime = true;
            main.startRotation3D = true; main.startRotationX = new ParticleSystem.MinMaxCurve(0, Mathf.PI); main.startRotationY = new ParticleSystem.MinMaxCurve(0, Mathf.PI);
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = dust.emission; emission.rateOverTime = 5;
            var shape = dust.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(21, 4, 21);
            var velocity = dust.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = .34f; velocity.y = -.22f; velocity.z = .12f;
            var rotation = dust.rotationOverLifetime; rotation.enabled = true; rotation.z = .7f;
            var fade = dust.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.65f, .2f), new GradientAlphaKey(.65f, .7f), new GradientAlphaKey(0, 1) }); fade.color = gradient;
            var renderer = GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            transform.position = new Vector3(0, 3, 0); SetScene("court");
        }
        public void SetScene(string arena)
        {
            if (dust == null) return;
            beach = arena.StartsWith("beach"); dust.Clear();
            var main = dust.main;
            main.startColor = beach ? new Color(.89f, .83f, .65f, .35f) : new Color(.88f, .64f, .53f, .65f);
            main.startSize = beach ? new ParticleSystem.MinMaxCurve(.012f, .035f) : new ParticleSystem.MinMaxCurve(.025f, .075f);
        }
        public void SetPaused(bool paused)
        {
            if (dust == null) return;
            if (!GameSettings.Particles) { if (dust.isPlaying || dust.isPaused) dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
            else if (paused) { if (dust.isPlaying) dust.Pause(); }
            else if (!dust.isPlaying) dust.Play();
        }
    }
}
