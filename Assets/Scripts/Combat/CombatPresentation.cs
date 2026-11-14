using UnityEngine;
using Ganryujima.Core;
using Ganryujima.World;

namespace Ganryujima.Combat
{
    /// <summary>Bounded spark pool, local hit-stop, camera feedback, and procedural sword poses.</summary>
    public sealed class CombatPresentation
    {
        private readonly CombatModel combat;
        private readonly Camera camera;
        private readonly Transform sword, target;
        private readonly Transform[] sparks = new Transform[GameConfig.SparkCount];
        private readonly Vector3[] velocities = new Vector3[GameConfig.SparkCount];
        private readonly Material sparkMaterial;
        private float hitStop, shake, sparkAge = float.MaxValue;
        private float visualClock;
        public bool Stopped => hitStop > 0;
        public Vector3 CameraOffset { get; private set; }

        public CombatPresentation(CombatModel combat, Camera camera, Transform sword, Transform target)
        {
            this.combat = combat; this.camera = camera; this.sword = sword; this.target = target;
            sparkMaterial = new Material(Resources.Load<Material>("Materials/Flat")) { color = new Color(1f, 0.74f, 0.24f) };
            var pool = new GameObject("Clash Sparks (pooled)").transform;
            for (int i = 0; i < sparks.Length; i++)
            {
                var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spark.name = "Spark";
                spark.transform.SetParent(pool, false);
                spark.GetComponent<Collider>().enabled = false;
                Object.Destroy(spark.GetComponent<Collider>());
                spark.GetComponent<Renderer>().sharedMaterial = sparkMaterial;
                spark.SetActive(false);
                sparks[i] = spark.transform;
            }
            combat.Impacted += OnImpact;
        }

        private void OnImpact(Impact impact)
        {
            if (impact == Impact.Miss) return;
            hitStop = impact == Impact.Parry ? GameConfig.ParryHitStop : impact == Impact.HeavyHit ? GameConfig.HeavyHitStop : GameConfig.HitStop;
            shake = GameConfig.ShakeDuration;
            bool onTarget = impact == Impact.TargetHit || impact == Impact.HeavyHit || impact == Impact.CounterHit;
            Vector3 point = onTarget ? target.position + Vector3.up * 1.35f : camera.transform.position + camera.transform.forward * 0.85f - camera.transform.up * 0.12f;
            if (impact == Impact.PlayerHit || impact == Impact.GuardBreak) return;
            sparkAge = 0;
            for (int i = 0; i < sparks.Length; i++)
            {
                float a = i * 2.39996f;
                velocities[i] = new Vector3(Mathf.Sin(a), 0.4f + i % 4 * 0.25f, Mathf.Cos(a)) * GameConfig.SparkSpeed;
                sparks[i].position = point;
                sparks[i].gameObject.SetActive(true);
            }
        }

        public void Tick(float realDt, float gameDt)
        {
            hitStop = Mathf.Max(0, hitStop - realDt);
            shake = Mathf.Max(0, shake - realDt);
            visualClock += realDt;
            float amplitude = GameConfig.ShakeAmplitude * shake / GameConfig.ShakeDuration;
            CameraOffset = new Vector3(Mathf.Sin(visualClock * 97), Mathf.Cos(visualClock * 113), 0) * amplitude;
            PoseSword(gameDt);
            sparkAge += realDt;
            for (int i = 0; i < sparks.Length; i++)
            {
                if (sparkAge >= GameConfig.SparkDuration) { sparks[i].gameObject.SetActive(false); continue; }
                velocities[i] += Vector3.down * (9f * realDt);
                sparks[i].position += velocities[i] * realDt;
                sparks[i].localScale = new Vector3(0.014f, 0.014f, 0.11f) * (1 - sparkAge / GameConfig.SparkDuration);
                sparks[i].rotation = Quaternion.LookRotation(velocities[i]);
            }
        }

        private void PoseSword(float dt)
        {
            Stance stance = combat.Swinging ? combat.AttackStance : combat.Stance;
            Vector3 position = new Vector3(0.42f, -0.55f, 0.85f);
            Vector3 angles = stance == Stance.Upper ? new Vector3(5, -10, 5) :
                stance == Stance.Lower ? new Vector3(45, 0, 35) :
                stance == Stance.Side ? new Vector3(15, -25, 65) : new Vector3(18, -8, -12);
            if (stance == Stance.Upper) position.y += 0.16f;
            if (stance == Stance.Lower) position.y -= 0.08f;
            if (combat.Guarding) { position = new Vector3(0.16f, -0.4f, 0.85f); angles += new Vector3(-15, 10, 40); }
            if (combat.Charging) angles += new Vector3(-25, -15, -25) * combat.ChargeProgress;
            if (combat.Swinging)
            {
                float p = combat.SwingProgress;
                float cut = Mathf.Sin(p * Mathf.PI);
                angles += new Vector3(-45 + 115 * p, 25 * cut, -65 + 130 * p) * cut;
                position.z += cut * 0.16f;
            }
            sword.localPosition = Vector3.Lerp(sword.localPosition, position, 1 - Mathf.Exp(-18 * dt));
            sword.localRotation = Quaternion.Slerp(sword.localRotation, Quaternion.Euler(angles), 1 - Mathf.Exp(-18 * dt));
        }

        public void Reset()
        {
            hitStop = shake = 0;
            sparkAge = float.MaxValue;
            CameraOffset = Vector3.zero;
            foreach (var spark in sparks) spark.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            combat.Impacted -= OnImpact;
            Object.Destroy(sparkMaterial);
        }
    }
}
