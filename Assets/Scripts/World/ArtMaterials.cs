using UnityEngine;
using Ganryujima.Core;

namespace Ganryujima.World
{
    public static class ArtMaterials
    {
        private static float TileNoise(float u, float v, float frequency)
        {
            return Mathf.Lerp(
                Mathf.Lerp(Mathf.PerlinNoise(u * frequency + 20, v * frequency + 40), Mathf.PerlinNoise((u - 1) * frequency + 20, v * frequency + 40), u),
                Mathf.Lerp(Mathf.PerlinNoise(u * frequency + 20, (v - 1) * frequency + 40), Mathf.PerlinNoise((u - 1) * frequency + 20, (v - 1) * frequency + 40), u), v);
        }
        public static Material Needles(GeneratedResources owner)
        {
            const int size = 512;
            var pixels = new Color[size * size];
            var random = new System.Random(101);
            void Line(Vector2 start, Vector2 end, float thickness, Color color)
            {
                float length = Vector2.Distance(start, end);
                int steps = Mathf.CeilToInt(length * 2);
                for (int step = 0; step <= steps; step++)
                {
                    Vector2 point = Vector2.Lerp(start, end, step / (float)Mathf.Max(1, steps));
                    int radius = Mathf.CeilToInt(thickness);
                    for (int y = -radius; y <= radius; y++) for (int x = -radius; x <= radius; x++)
                    {
                        int px = Mathf.RoundToInt(point.x) + x, py = Mathf.RoundToInt(point.y) + y;
                        if (px < 0 || py < 0 || px >= size || py >= size || x * x + y * y > thickness * thickness) continue;
                        pixels[py * size + px] = color;
                    }
                }
            }
            for (int branch = 0; branch < 15; branch++)
            {
                float a = branch * 2.39996f;
                Vector2 origin = new Vector2(256, 190 + branch * 4);
                Vector2 tip = origin + new Vector2(Mathf.Cos(a) * 190, Mathf.Sin(a) * 115 + 90);
                Line(origin, tip, 1.4f, new Color(0.7f, 0.65f, 0.5f, 1));
                Vector2 direction = (tip - origin).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x);
                for (int needle = 0; needle < 38; needle++)
                {
                    float t = needle / 38f;
                    Vector2 point = Vector2.Lerp(origin, tip, t);
                    float length = 16 + (float)random.NextDouble() * 32;
                    Color color = Color.Lerp(new Color(0.44f, 0.5f, 0.36f, 1), new Color(0.8f, 0.84f, 0.6f, 1), (float)random.NextDouble());
                    Line(point, point + (normal + direction * 0.65f) * length, 1, color);
                    Line(point, point + (-normal + direction * 0.65f) * length, 1, color);
                }
            }
            var texture = owner.Own(new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "Procedural pine needle spray", wrapMode = TextureWrapMode.Clamp });
            texture.SetPixels(pixels); texture.Apply(true, true);
            var material = owner.Own(new Material(Resources.Load<Shader>("Shaders/PineNeedles")));
            material.mainTexture = texture;
            material.color = new Color(0.40f, 0.48f, 0.35f);
            return material;
        }

        public static Material Surface(GeneratedResources owner, string name, Color tint, string pattern, float smoothness = 0.15f, float metallic = 0)
        {
            var material = owner.Own(new Material(Resources.Load<Material>("Materials/Surface")) { name = name, color = tint });
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.enableInstancing = true;
            if (pattern == null) return material;
            int size = VisualConfig.TextureSize;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float broad = TileNoise(u, v, 8);
                float fine = TileNoise(u, v, 120) - 0.5f;
                float value = 0.76f + broad * 0.22f + fine * 0.09f;
                if (pattern == "wood") value = 0.7f + broad * 0.17f + Mathf.Sin(u * Mathf.PI * 56 + broad * 16) * 0.1f + fine * 0.04f;
                if (pattern == "sand") value = 0.82f + broad * 0.13f + Mathf.Sin(v * Mathf.PI * 32 + broad * 12) * 0.02f + fine * 0.05f;
                if (pattern == "cloth") value = 0.8f + (Mathf.Sin(x * 2) + Mathf.Sin(y * 2)) * 0.04f + fine * 0.03f;
                pixels[y * size + x] = new Color(value, value * 0.99f, value * 0.96f);
            }
            var texture = owner.Own(new Texture2D(size, size, TextureFormat.RGB24, true) { name = name + " grain", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 });
            texture.SetPixels(pixels); texture.Apply(true, true);
            material.mainTexture = texture;
            return material;
        }
    }
}
