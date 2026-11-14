using UnityEngine;
using Ganryujima.Core;

namespace Ganryujima.World
{
    /// <summary>Built-in rendering and local procedural geometry only.</summary>
    public static class ArenaBuilder
    {
        public static Transform Build()
        {
            var root = new GameObject("Procedural Practice Ground").transform;
            var sand = Material("Sand", new Color(0.69f, 0.66f, 0.57f));
            var ink = Material("Ink", new Color(0.11f, 0.15f, 0.14f));
            var stone = Material("Stone", new Color(0.36f, 0.4f, 0.36f));
            var red = Material("Vermilion", new Color(0.48f, 0.12f, 0.1f));
            Shape("Sand", PrimitiveType.Cube, root, new Vector3(0, -0.2f, 0), new Vector3(180, 0.4f, 180), sand, true);
            Ring(root, GameConfig.ArenaRadius, ink);
            Ring(root, 4, ink);
            var gate = new GameObject("Torii").transform;
            gate.SetParent(root, false);
            gate.position = new Vector3(0, 0, 16);
            Shape("Left pillar", PrimitiveType.Cube, gate, new Vector3(-2.6f, 2.9f, 0), new Vector3(0.42f, 5.8f, 0.45f), red);
            Shape("Right pillar", PrimitiveType.Cube, gate, new Vector3(2.6f, 2.9f, 0), new Vector3(0.42f, 5.8f, 0.45f), red);
            Shape("Crown", PrimitiveType.Cube, gate, new Vector3(0, 5.8f, 0), new Vector3(7.2f, 0.35f, 0.65f), red);
            Shape("Crossbeam", PrimitiveType.Cube, gate, new Vector3(0, 4.8f, 0), new Vector3(6.2f, 0.25f, 0.4f), red);
            Shape("Center", PrimitiveType.Cube, gate, new Vector3(0, 5.2f, 0), new Vector3(0.2f, 0.8f, 0.2f), red);
            var marker = new GameObject("Practice Target").transform;
            marker.SetParent(root, false);
            marker.position = new Vector3(0, 0, 2);
            Shape("Post", PrimitiveType.Cylinder, marker, new Vector3(0, 0.9f, 0), new Vector3(0.28f, 0.9f, 0.28f), ink, true);
            Shape("Straw wrap", PrimitiveType.Cylinder, marker, new Vector3(0, 1.2f, 0), new Vector3(0.64f, 0.45f, 0.64f), stone, true);
            Shape("Band", PrimitiveType.Cylinder, marker, new Vector3(0, 1.3f, 0), new Vector3(0.66f, 0.045f, 0.66f), red);
            for (int i = 0; i < 38; i++)
            {
                float angle = i * 2.39996f;
                float radius = GameConfig.ArenaRadius + 2 + i % 7 * 1.7f;
                var rock = Shape("Boundary stone", PrimitiveType.Sphere, root,
                    new Vector3(Mathf.Sin(angle) * radius, 0.3f, Mathf.Cos(angle) * radius),
                    new Vector3(1 + i % 3 * 0.6f, 0.8f + i % 4 * 0.4f, 1.2f), stone);
                rock.localRotation = Quaternion.Euler(i * 13, i * 27, i * 7);
            }
            for (int i = 0; i < 9; i++)
                Shape("Distant ridge", PrimitiveType.Sphere, root, new Vector3((i - 4) * 18, 0, 65 + i % 2 * 9), new Vector3(35, 20 + i % 4 * 6, 22), stone);
            var sun = new Material(Resources.Load<Material>("Materials/Flat")) { color = new Color(0.64f, 0.24f, 0.16f) };
            Shape("Red sun", PrimitiveType.Sphere, root, new Vector3(18, 10, 52), Vector3.one * 7, sun);
            var light = new GameObject("Dawn light").AddComponent<Light>();
            light.transform.SetParent(root);
            light.type = LightType.Directional;
            light.color = new Color(1, 0.94f, 0.8f);
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(35, -30, 0);
            RenderSettings.ambientLight = new Color(0.64f, 0.65f, 0.6f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.82f, 0.81f, 0.75f);
            RenderSettings.fogStartDistance = 20;
            RenderSettings.fogEndDistance = 90;
            RenderSettings.skybox = null;
            return root;
        }

        public static Transform BuildKatana(Transform camera)
        {
            var sword = new GameObject("Katana").transform;
            sword.SetParent(camera, false);
            sword.localPosition = new Vector3(0.34f, -0.43f, 0.62f);
            sword.localRotation = Quaternion.Euler(12, 0, 24);
            var dark = Material("Grip", new Color(0.12f, 0.16f, 0.15f));
            var steel = Material("Steel", new Color(0.8f, 0.84f, 0.8f));
            steel.SetFloat("_Metallic", 0.7f);
            steel.SetFloat("_Glossiness", 0.65f);
            Shape("Blade", PrimitiveType.Cube, sword, new Vector3(0, 0.6f, 0), new Vector3(0.033f, 0.88f, 0.013f), steel);
            Shape("Grip", PrimitiveType.Cylinder, sword, Vector3.zero, new Vector3(0.05f, 0.13f, 0.05f), dark);
            Shape("Guard", PrimitiveType.Cylinder, sword, new Vector3(0, 0.145f, 0), new Vector3(0.13f, 0.009f, 0.13f), dark);
            return sword;
        }

        // Resources templates keep shaders/variants referenced in standalone builds.
        private static Material Material(string name, Color color) => new Material(Resources.Load<Material>("Materials/Surface")) { name = name, color = color };

        private static Transform Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool collision = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) { var collider = go.GetComponent<Collider>(); collider.enabled = false; Object.Destroy(collider); }
            return go.transform;
        }

        private static void Ring(Transform parent, float radius, Material ink)
        {
            const int segments = 96;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                var mark = Shape("Court boundary", PrimitiveType.Cube, parent,
                    new Vector3(Mathf.Sin(angle) * radius, 0.008f, Mathf.Cos(angle) * radius),
                    new Vector3(radius * Mathf.PI * 2 / segments + 0.01f, 0.01f, 0.025f), ink);
                mark.localRotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);
            }
        }
    }
}
