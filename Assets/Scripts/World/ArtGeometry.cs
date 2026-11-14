using System.Collections.Generic;
using UnityEngine;

namespace Ganryujima.World
{
    public static class ArtGeometry
    {
        public static float GroundHeight(float x, float z)
        {
            float edge = Mathf.SmoothStep(0, 1, (new Vector2(x, z).magnitude - 14) / 12);
            float dunes = (Mathf.PerlinNoise(x * 0.055f + 14, z * 0.055f + 6) * 1.8f + Mathf.Sin(z * 0.1f) * 0.25f) * edge;
            return dunes - Mathf.Max(0, x - 24) * 0.16f;
        }

        public static Mesh Ground()
        {
            const int side = 101;
            var vertices = new Vector3[side * side];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(side - 1) * (side - 1) * 6];
            int t = 0;
            for (int z = 0; z < side; z++) for (int x = 0; x < side; x++)
            {
                int i = z * side + x;
                float px = (x - 50) * 2f, pz = (z - 50) * 2f;
                vertices[i] = new Vector3(px, GroundHeight(px, pz), pz);
                uv[i] = new Vector2(px, pz) * 0.23f;
                if (x == side - 1 || z == side - 1) continue;
                triangles[t++] = i; triangles[t++] = i + side; triangles[t++] = i + 1;
                triangles[t++] = i + 1; triangles[t++] = i + side; triangles[t++] = i + side + 1;
            }
            return Finish("Sculpted dunes", vertices, triangles, uv);
        }

        public static Mesh Rock(float seed)
        {
            const int rows = 16, columns = 24;
            var vertices = new Vector3[(rows + 1) * (columns + 1)];
            var uv = new Vector2[vertices.Length];
            var indices = new List<int>();
            for (int y = 0; y <= rows; y++) for (int x = 0; x <= columns; x++)
            {
                float a = x * Mathf.PI * 2 / columns, b = y * Mathf.PI / rows;
                var direction = new Vector3(Mathf.Sin(b) * Mathf.Cos(a), Mathf.Cos(b), Mathf.Sin(b) * Mathf.Sin(a));
                float noise = Mathf.PerlinNoise(direction.x * 3.1f + seed, direction.y * 3.1f + direction.z * 1.7f + 40);
                int i = y * (columns + 1) + x;
                vertices[i] = direction * (0.9f + noise * 0.18f);
                uv[i] = new Vector2(x / (float)columns * 2, y / (float)rows);
                if (x == columns || y == rows) continue;
                int j = i + columns + 1;
                indices.Add(i); indices.Add(i + 1); indices.Add(j);
                indices.Add(i + 1); indices.Add(j + 1); indices.Add(j);
            }
            return Finish("Weathered granite", vertices, indices.ToArray(), uv);
        }

        public static Mesh Ridge(float seed, float height)
        {
            const int columns = 100, rows = 6;
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var uv = new Vector2[vertices.Length];
            var indices = new List<int>();
            for (int z = 0; z <= rows; z++) for (int x = 0; x <= columns; x++)
            {
                float px = (x - 50) * 3f;
                float peak = 0.3f + Mathf.PerlinNoise(x * 0.1f + seed, seed) * 0.7f;
                peak += Mathf.PerlinNoise(x * 0.37f, seed + 70) * 0.18f;
                float profile = Mathf.Sin(z / (float)rows * Mathf.PI);
                int i = z * (columns + 1) + x;
                vertices[i] = new Vector3(px, peak * height * profile - 3, z * 7);
                uv[i] = new Vector2(x / 10f, z);
                if (x == columns || z == rows) continue;
                int j = i + columns + 1;
                indices.Add(i); indices.Add(j); indices.Add(i + 1);
                indices.Add(i + 1); indices.Add(j); indices.Add(j + 1);
            }
            return Finish("Mountain silhouette", vertices, indices.ToArray(), uv);
        }

        public static Mesh Tube(IList<Vector3> points, float radius, float endRadius, int sides = 8)
        {
            var vertices = new Vector3[points.Count * (sides + 1)];
            var uv = new Vector2[vertices.Length];
            var indices = new List<int>();
            for (int p = 0; p < points.Count; p++)
            {
                Vector3 forward = (points[Mathf.Min(p + 1, points.Count - 1)] - points[Mathf.Max(0, p - 1)]).normalized;
                Vector3 right = Vector3.Cross(forward, Mathf.Abs(forward.y) > 0.9f ? Vector3.forward : Vector3.up).normalized;
                Vector3 up = Vector3.Cross(right, forward).normalized;
                float r = Mathf.Lerp(radius, endRadius, p / (float)(points.Count - 1));
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2;
                    int i = p * (sides + 1) + s;
                    vertices[i] = points[p] + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * r;
                    uv[i] = new Vector2(s / (float)sides, p / (float)(points.Count - 1) * 3);
                    if (s == sides || p == points.Count - 1) continue;
                    int j = i + sides + 1;
                    indices.Add(i); indices.Add(j); indices.Add(i + 1);
                    indices.Add(i + 1); indices.Add(j); indices.Add(j + 1);
                }
            }
            return Finish("Shaped timber", vertices, indices.ToArray(), uv);
        }

        public static Mesh Canopy(float seed)
        {
            var random = new System.Random((int)seed + 1);
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var indices = new List<int>();
            for (int card = 0; card < 28; card++)
            {
                float a = card * 2.39996f;
                float radius = Mathf.Sqrt((float)random.NextDouble()) * 0.8f;
                var p = new Vector3(Mathf.Cos(a) * radius, (float)random.NextDouble() * 0.4f, Mathf.Sin(a) * radius);
                var rotation = Quaternion.Euler(25 + card % 3 * 25, card * 61, card % 7 * 3);
                float width = 0.42f + (float)random.NextDouble() * 0.45f;
                Vector3 right = rotation * Vector3.right * width, up = rotation * Vector3.up * width;
                int i = vertices.Count;
                vertices.Add(p - right - up); vertices.Add(p + right - up); vertices.Add(p + right + up); vertices.Add(p - right + up);
                uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.one); uv.Add(Vector2.up);
                indices.Add(i); indices.Add(i + 2); indices.Add(i + 1);
                indices.Add(i); indices.Add(i + 3); indices.Add(i + 2);
            }
            return DoubleSided("Pine needle sprays", vertices.ToArray(), indices.ToArray(), uv.ToArray());
        }

        public static Mesh Blade(float length, float width, float curvature)
        {
            const int rows = 32;
            var vertices = new Vector3[(rows + 1) * 4];
            var uv = new Vector2[vertices.Length];
            var indices = new List<int>();
            for (int r = 0; r <= rows; r++)
            {
                float t = r / (float)rows;
                float w = width * Mathf.Lerp(1, 0.72f, t) * (r == rows ? 0.015f : 1);
                float center = t * t * curvature;
                vertices[r * 4] = new Vector3(center - w / 2, length * t, 0);
                vertices[r * 4 + 1] = new Vector3(center, length * t, width * 0.1f);
                vertices[r * 4 + 2] = new Vector3(center + w / 2, length * t, 0);
                vertices[r * 4 + 3] = new Vector3(center, length * t, -width * 0.1f);
                for (int k = 0; k < 4; k++)
                {
                    uv[r * 4 + k] = new Vector2(k / 3f, t);
                    if (r == rows) continue;
                    int a = r * 4 + k, b = r * 4 + (k + 1) % 4;
                    indices.Add(a); indices.Add(b); indices.Add(a + 4);
                    indices.Add(b); indices.Add(b + 4); indices.Add(a + 4);
                }
            }
            return Finish("Forged curved blade", vertices, indices.ToArray(), uv);
        }

        public static Mesh DoubleSided(string name, Vector3[] vertices, int[] triangles, Vector2[] uv)
        {
            int n = vertices.Length;
            var allVertices = new Vector3[n * 2]; var allUv = new Vector2[n * 2]; var allIndices = new int[triangles.Length * 2];
            System.Array.Copy(vertices, allVertices, n); System.Array.Copy(vertices, 0, allVertices, n, n);
            System.Array.Copy(uv, allUv, n); System.Array.Copy(uv, 0, allUv, n, n);
            System.Array.Copy(triangles, allIndices, triangles.Length);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                allIndices[triangles.Length + i] = triangles[i] + n;
                allIndices[triangles.Length + i + 1] = triangles[i + 2] + n;
                allIndices[triangles.Length + i + 2] = triangles[i + 1] + n;
            }
            return Finish(name, allVertices, allIndices, allUv);
        }

        public static Mesh Finish(string name, Vector3[] vertices, int[] triangles, Vector2[] uv)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Length > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.uv = uv;
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            return mesh;
        }
    }
}
