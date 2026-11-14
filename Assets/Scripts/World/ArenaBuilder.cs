using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Ganryujima.Core;

namespace Ganryujima.World
{
    /// <summary>Hand-shaped coastal practice court. All art is generated locally.</summary>
    public static class ArenaBuilder
    {
        public static Transform Build()
        {
            var root = new GameObject("Coastal Practice Ground").transform;
            var assets = root.gameObject.AddComponent<GeneratedResources>();
            var sand = ArtMaterials.Surface(assets, "Raked coastal sand", new Color(0.74f, 0.68f, 0.55f), "sand", 0.1f);
            var stone = ArtMaterials.Surface(assets, "Salt-weathered granite", new Color(0.45f, 0.47f, 0.43f), "stone", 0.18f);
            var wood = ArtMaterials.Surface(assets, "Old cedar", new Color(0.24f, 0.20f, 0.15f), "wood", 0.2f);
            var red = ArtMaterials.Surface(assets, "Weathered vermilion", new Color(0.69f, 0.22f, 0.13f), "wood", 0.2f);
            var ink = ArtMaterials.Surface(assets, "Charcoal lacquer", new Color(0.09f, 0.12f, 0.115f), "wood", 0.3f);
            var straw = ArtMaterials.Surface(assets, "Dry rice straw", new Color(0.68f, 0.56f, 0.35f), "wood", 0.08f);
            var rope = ArtMaterials.Surface(assets, "Hemp binding", new Color(0.55f, 0.46f, 0.3f), "cloth", 0.05f);
            var pine = ArtMaterials.Needles(assets);
            var grass = ArtMaterials.Surface(assets, "Dune grass", new Color(0.44f, 0.43f, 0.30f), "cloth", 0.05f);

            var terrain = MeshObject("Sand and wind-shaped dunes", root, assets.Own(ArtGeometry.Ground()), sand);
            terrain.gameObject.AddComponent<MeshCollider>().sharedMesh = terrain.GetComponent<MeshFilter>().sharedMesh;
            var water = assets.Own(new Material(Resources.Load<Shader>("Shaders/CoastalWater")));
            Primitive("Quiet inlet", PrimitiveType.Plane, root, new Vector3(100, -0.75f, 55), new Vector3(45, 1, 50), water);
            for (int i = 0; i < 3; i++)
            {
                var ridge = MeshObject("Distant mountain wash", root, assets.Own(ArtGeometry.Ridge(12 + i * 16, 26 + i * 14)),
                    ArtMaterials.Surface(assets, "Mountain haze", new Color(0.32f + i * 0.09f, 0.38f + i * 0.08f, 0.38f + i * 0.08f), null));
                ridge.localPosition = new Vector3(-15 + i * 18, 0, 75 + i * 45);
            }

            var random = new System.Random(1703);
            var rocks = new Mesh[7];
            for (int i = 0; i < rocks.Length; i++) rocks[i] = assets.Own(ArtGeometry.Rock(i * 17 + 9));
            for (int i = 0; i < 32; i++)
            {
                float a = i * 2.39996f;
                float radius = 18f + (float)random.NextDouble() * 18;
                float x = Mathf.Sin(a) * radius, z = Mathf.Cos(a) * radius;
                var rock = MeshObject("Granite outcrop", root, rocks[i % rocks.Length], stone);
                rock.localPosition = new Vector3(x, ArtGeometry.GroundHeight(x, z) + 0.15f, z);
                rock.localScale = new Vector3(0.8f + i % 4 * 0.6f, 0.5f + i % 3 * 0.35f, 1.3f + i % 5 * 0.25f);
                rock.localRotation = Quaternion.Euler(i * 13, i * 29, i * 7);
            }
            for (int i = 0; i < 28; i++)
            {
                float a = i * Mathf.PI * 2 / 28;
                var stoneMark = MeshObject("Worn court edge stone", root, rocks[i % rocks.Length], stone);
                stoneMark.localPosition = new Vector3(Mathf.Sin(a) * 13.05f, 0.03f, Mathf.Cos(a) * 13.05f);
                stoneMark.localScale = new Vector3(0.23f + i % 3 * 0.06f, 0.10f, 0.4f);
                stoneMark.localRotation = Quaternion.Euler(0, i * 39, 0);
            }
            for (int i = 0; i < 6; i++)
            {
                var path = MeshObject("Old shrine stepping stone", root, rocks[i % rocks.Length], stone);
                path.localPosition = new Vector3(Mathf.Sin(i * 3) * 0.25f, 0.04f, 6.5f + i * 1.5f);
                path.localScale = new Vector3(0.8f, 0.09f, 0.55f);
                path.localRotation = Quaternion.Euler(0, i * 27, 0);
            }
            BuildGate(root, assets, red, ink, stone, rope);
            BuildPost(root, assets, wood, straw, rope, red);
            Pine(root, assets, new Vector3(-12, 0, 11), 8, 3, wood, pine);
            Pine(root, assets, new Vector3(-18, 0, -3), 10, 9, wood, pine);
            Pine(root, assets, new Vector3(14, 0, 18), 8.5f, 19, wood, pine);
            Pine(root, assets, new Vector3(-25, 0, 30), 10, 27, wood, pine);
            Pine(root, assets, new Vector3(23, 0, 37), 12, 39, wood, pine);
            Lantern(root, assets, new Vector3(-6.2f, 0, 7.5f), stone, ink);
            Lantern(root, assets, new Vector3(6.2f, 0, 7.5f), stone, ink);
            BuildGrass(root, assets, grass);

            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = VisualConfig.ShadowDistance;
            QualitySettings.shadowCascades = 2;
            QualitySettings.antiAliasing = 4;
            var light = new GameObject("Low dawn sun").AddComponent<Light>();
            light.transform.SetParent(root);
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.88f, 0.67f);
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.8f;
            light.shadowBias = 0.035f;
            light.shadowNormalBias = 0.22f;
            light.transform.rotation = Quaternion.LookRotation(-new Vector3(0.34f, 0.30f, 0.94f).normalized);
            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.63f, 0.65f);
            RenderSettings.ambientEquatorColor = new Color(0.43f, 0.44f, 0.39f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.23f, 0.18f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.73f, 0.74f, 0.68f);
            RenderSettings.fogDensity = VisualConfig.FogDensity;
            RenderSettings.skybox = assets.Own(new Material(Resources.Load<Shader>("Shaders/DawnSky")));
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.65f;
            var fill = new GameObject("Cool sky bounce").AddComponent<Light>();
            fill.transform.SetParent(root); fill.type = LightType.Directional;
            fill.color = new Color(0.70f, 0.81f, 0.88f); fill.intensity = 0.35f;
            fill.transform.rotation = Quaternion.Euler(28, 165, 0);
            DynamicGI.UpdateEnvironment();
            return root;
        }

        private static void BuildGate(Transform parent, GeneratedResources assets, Material red, Material ink, Material stone, Material rope)
        {
            var gate = new GameObject("Weathered shrine torii").transform;
            gate.SetParent(parent, false);
            gate.localPosition = new Vector3(0, 0, 17);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Primitive("Granite foundation", PrimitiveType.Cylinder, gate, new Vector3(sign * 2.65f, 0.16f, 0), new Vector3(0.78f, 0.16f, 0.78f), stone);
                Tube("Tapered cedar pillar", gate, assets, new[] { new Vector3(sign * 2.65f, 0.3f, 0), new Vector3(sign * 2.47f, 5.6f, 0) }, 0.24f, 0.19f, red, 16);
                Primitive("Black pillar foot", PrimitiveType.Cylinder, gate, new Vector3(sign * 2.64f, 0.55f, 0), new Vector3(0.51f, 0.3f, 0.51f), ink);
            }
            Primitive("Lower tie beam", PrimitiveType.Cube, gate, new Vector3(0, 4.62f, 0), new Vector3(6.5f, 0.24f, 0.34f), red);
            Primitive("Central strut", PrimitiveType.Cube, gate, new Vector3(0, 5, 0), new Vector3(0.21f, 0.7f, 0.22f), ink);
            var curve = new Vector3[25];
            for (int i = 0; i < curve.Length; i++)
            {
                float x = i / 24f * 2 - 1;
                curve[i] = new Vector3(x * 3.65f, 5.52f + Mathf.Pow(Mathf.Abs(x), 3) * 0.34f, 0);
            }
            // A rectangular curved roof reads as a crafted beam rather than stacked cubes.
            Tube("Curved lacquer crown", gate, assets, curve, 0.22f, 0.22f, ink, 4);
            var ropeLine = new Vector3[33];
            for (int i = 0; i < ropeLine.Length; i++) { float x = i / 32f * 2 - 1; ropeLine[i] = new Vector3(x * 2.4f, 4.32f - (1 - x * x) * 0.32f, -0.22f); }
            Tube("Hemp rope", gate, assets, ropeLine, 0.045f, 0.045f, rope, 8);
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * 0.8f;
                Tube("Rope tassel", gate, assets, new[] { new Vector3(x, 4.0f + x * x * 0.05f, -0.22f), new Vector3(x + 0.035f, 3.63f + x * x * 0.05f, -0.22f) }, 0.04f, 0.06f, rope, 7);
            }
        }

        private static void BuildPost(Transform parent, GeneratedResources assets, Material wood, Material straw, Material rope, Material red)
        {
            var target = new GameObject("Practice Target").transform;
            target.SetParent(parent, false); target.localPosition = new Vector3(0, 0, 2);
            Primitive("Cedar post", PrimitiveType.Cylinder, target, new Vector3(0, 0.94f, 0), new Vector3(0.22f, 0.94f, 0.22f), wood, true);
            Primitive("Bound straw core", PrimitiveType.Cylinder, target, new Vector3(0, 1.12f, 0), new Vector3(0.56f, 0.53f, 0.56f), straw, true);
            for (int i = 0; i < 56; i++)
            {
                float a = i * Mathf.PI * 2 / 56;
                float radius = 0.285f + Mathf.Sin(i * 7) * 0.008f;
                Tube("Individual straw stalks", target, assets, new[] {
                    new Vector3(Mathf.Cos(a) * radius, 0.58f + Mathf.Sin(i) * 0.04f, Mathf.Sin(a) * radius),
                    new Vector3(Mathf.Cos(a) * radius, 1.62f + Mathf.Sin(i * 4) * 0.06f, Mathf.Sin(a) * radius) }, 0.008f, 0.004f, straw, 4);
            }
            for (int row = 0; row < 5; row++)
            {
                var binding = new Vector3[65];
                for (int i = 0; i < binding.Length; i++) { float a = i * Mathf.PI * 2 / 64; binding[i] = new Vector3(Mathf.Cos(a) * 0.3f, 0.72f + row * 0.18f + Mathf.Sin(a * 4) * 0.008f, Mathf.Sin(a) * 0.3f); }
                Tube("Braided bindings", target, assets, binding, 0.014f, 0.014f, row == 3 ? red : rope, 6);
            }
        }

        private static void Pine(Transform parent, GeneratedResources assets, Vector3 position, float height, int seed, Material bark, Material needles)
        {
            var tree = new GameObject("Wind-sculpted black pine").transform;
            tree.SetParent(parent, false);
            tree.localPosition = position + Vector3.up * ArtGeometry.GroundHeight(position.x, position.z);
            Tube("Bent trunk", tree, assets, new[] { Vector3.zero, new Vector3(-0.18f, height * 0.36f, 0.12f), new Vector3(0.45f, height * 0.72f, 0.06f), new Vector3(0.2f, height, 0) }, 0.28f, 0.07f, bark, 12);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.39996f + seed;
                float y = height * (0.42f + i * 0.06f);
                float reach = height * (0.34f - i * 0.022f);
                Vector3 end = new Vector3(Mathf.Cos(a) * reach, y + 0.4f, Mathf.Sin(a) * reach);
                Tube("Pine bough", tree, assets, new[] { new Vector3(0.2f, y - 0.3f, 0), Vector3.Scale(end, new Vector3(0.65f, 1, 0.65f)), end }, 0.09f, 0.025f, bark, 7);
                var canopy = MeshObject("Needle canopy", tree, assets.Own(ArtGeometry.Canopy(seed + i * 4)), needles);
                canopy.localPosition = end;
                canopy.localScale = new Vector3(reach * 0.82f, 1.2f, reach * 0.65f);
                canopy.localRotation = Quaternion.Euler(-5, i * 43, 8);
            }
        }

        private static void Lantern(Transform parent, GeneratedResources assets, Vector3 p, Material stone, Material dark)
        {
            var root = new GameObject("Stone lantern").transform; root.SetParent(parent, false); root.localPosition = p;
            Primitive("Lantern base", PrimitiveType.Cylinder, root, new Vector3(0, 0.1f, 0), new Vector3(0.75f, 0.1f, 0.75f), stone);
            Primitive("Stem", PrimitiveType.Cylinder, root, new Vector3(0, 0.5f, 0), new Vector3(0.25f, 0.4f, 0.25f), stone);
            Primitive("Shelf", PrimitiveType.Cylinder, root, new Vector3(0, 0.92f, 0), new Vector3(0.66f, 0.06f, 0.66f), stone);
            for (int i = 0; i < 4; i++) Primitive("Window pillar", PrimitiveType.Cube, root,
                new Vector3((i % 2 == 0 ? -1 : 1) * 0.2f, 1.15f, (i < 2 ? -1 : 1) * 0.2f), new Vector3(0.08f, 0.44f, 0.08f), stone);
            Primitive("Pagoda cap", PrimitiveType.Cylinder, root, new Vector3(0, 1.4f, 0), new Vector3(0.85f, 0.08f, 0.85f), dark);
            Primitive("Finial", PrimitiveType.Sphere, root, new Vector3(0, 1.55f, 0), Vector3.one * 0.16f, stone);
            var glow = assets.Own(new Material(Resources.Load<Material>("Materials/Flat")) { color = new Color(1, 0.55f, 0.2f) });
            Primitive("Lantern ember", PrimitiveType.Sphere, root, new Vector3(0, 1.1f, 0), Vector3.one * 0.09f, glow);
            var lamp = new GameObject("Warm candle").AddComponent<Light>(); lamp.transform.SetParent(root, false); lamp.transform.localPosition = Vector3.up * 1.1f;
            lamp.type = LightType.Point; lamp.color = new Color(1, 0.6f, 0.3f); lamp.intensity = 0.8f; lamp.range = 2.3f;
        }

        private static void BuildGrass(Transform parent, GeneratedResources assets, Material material)
        {
            var random = new System.Random(61);
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int i = 0; i < 3200; i++)
            {
                float a = (i / 24) * 2.39996f;
                float radius = 14.2f + (i / 24) % 11 * 1.1f;
                float x = Mathf.Sin(a) * radius + (float)random.NextDouble() * 1.8f, z = Mathf.Cos(a) * radius + (float)random.NextDouble() * 1.8f;
                if (x > 25 || (Mathf.Abs(x) < 4 && z > 5)) continue;
                Vector3 p = new Vector3(x, ArtGeometry.GroundHeight(x, z) + 0.01f, z);
                float h = 0.15f + (float)random.NextDouble() * 0.4f;
                for (int cross = 0; cross < 2; cross++)
                {
                    float angle = a + cross * Mathf.PI / 2;
                    Vector3 right = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 0.015f;
                    int start = vertices.Count;
                    vertices.Add(p - right); vertices.Add(p + right); vertices.Add(p + new Vector3(0.14f, h, 0));
                    uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.up);
                    triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                }
            }
            MeshObject("Coastal grasses", parent, assets.Own(ArtGeometry.DoubleSided("Dune grass field", vertices.ToArray(), triangles.ToArray(), uv.ToArray())), material);
        }

        public static Transform BuildKatana(Transform camera) => KatanaArt.Build(camera);

        public static Transform MeshObject(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go.transform;
        }

        public static Transform Tube(string name, Transform parent, GeneratedResources assets, IList<Vector3> points, float radius, float endRadius, Material material, int sides = 8)
            => MeshObject(name, parent, assets.Own(ArtGeometry.Tube(points, radius, endRadius, sides)), material);

        public static Transform Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool collision = false)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) { var collider = go.GetComponent<Collider>(); collider.enabled = false; Object.Destroy(collider); }
            return go.transform;
        }
    }
}
