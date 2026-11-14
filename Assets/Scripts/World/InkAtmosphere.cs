using UnityEngine;
using Ganryujima.Core;

namespace Ganryujima.World
{
    [RequireComponent(typeof(Camera))]
    public sealed class InkAtmosphere : MonoBehaviour
    {
        private Material material;
        private void Awake()
        {
            var shader = Resources.Load<Shader>("Shaders/InkAtmosphere");
            if (shader == null || !shader.isSupported) return;
            material = new Material(shader);
            material.SetFloat("_Grain", VisualConfig.Grain);
            material.SetFloat("_Vignette", VisualConfig.Vignette);
            material.SetFloat("_Bloom", VisualConfig.Bloom);
            material.SetFloat("_Saturation", VisualConfig.Saturation);
        }
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (material == null) Graphics.Blit(source, destination);
            else Graphics.Blit(source, destination, material);
        }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
