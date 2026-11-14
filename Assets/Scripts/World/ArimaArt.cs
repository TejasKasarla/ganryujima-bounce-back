using UnityEngine;
using Ganryujima.Combat;

namespace Ganryujima.World
{
    public sealed class ArimaArt
    {
        public Transform Root { get; }
        private readonly SamuraiRig rig;
        public ArimaArt(Transform parent)
        {
            var prefab = Resources.Load<GameObject>("Characters/Arima");
            if (prefab == null) throw new System.InvalidOperationException("Arima prefab missing. Run Ganryujima/Prepare samurai character.");
            Root = Object.Instantiate(prefab, parent).transform;
            Root.name = "Arima Kihei";
            rig = Root.GetComponent<SamuraiRig>();
            rig.Initialize();
        }
        public void Tick(ArimaOpponent ai, float dt)
        {
            Root.position = new Vector3(ai.Position.x, 0, ai.Position.y);
            Root.rotation = Quaternion.LookRotation(new Vector3(ai.Forward.x, 0, ai.Forward.y));
            rig.Pose(ai, dt);
        }
    }
}
