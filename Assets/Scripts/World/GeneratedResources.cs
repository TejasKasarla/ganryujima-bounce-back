using System.Collections.Generic;
using UnityEngine;

namespace Ganryujima.World
{
    /// <summary>Owns procedural native assets across Play Mode and scene reloads.</summary>
    public sealed class GeneratedResources : MonoBehaviour
    {
        private readonly List<Object> assets = new List<Object>();
        public T Own<T>(T asset) where T : Object { assets.Add(asset); return asset; }
        private void OnDestroy() { foreach (var asset in assets) if (asset != null) Destroy(asset); assets.Clear(); }
    }
}
