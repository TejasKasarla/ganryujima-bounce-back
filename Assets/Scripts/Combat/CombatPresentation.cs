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
        private readonly Material ringMaterial;
        private readonly LineRenderer ring;
        private float ringAge = float.MaxValue;
        private float hitStop, shake, sparkAge = float.MaxValue;
        private float visualClock;
        public bool Stopped => hitStop > 0;
        public Vector3 CameraOffset { get; private set; }

        public CombatPresentation(CombatModel combat, Camera camera, Transform sword, Transform target)
        {
            this.combat = combat; this.camera = camera; this.sword = sword; this.target = target;
            sparkMaterial = new Material(Resources.Load<Material>("Materials/Flat")) { color = new Color(1f, 0.74f, 0.24f) };
            ringMaterial = new Material(Resources.Load<Shader>("Shaders/KiaiRing"));
            ring = new GameObject("Kiai shockwave (pooled)").AddComponent<LineRenderer>();
            ring.sharedMaterial = ringMaterial; ring.useWorldSpace = false; ring.loop = true;
            ring.positionCount = 64; ring.widthMultiplier = 0.045f;
            for (int i=0;i<64;i++) { float angle=i*Mathf.PI*2/64; ring.SetPosition(i,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))); }
            ring.gameObject.SetActive(false);
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

        public void FinisherImpact() => OnImpact(Impact.HeavyHit);

        private void OnImpact(Impact impact)
        {
            if (impact == Impact.Miss || impact == Impact.Dodge || impact == Impact.Evade) return;
            if (impact == Impact.Kiai)
            {
                shake = GameConfig.ShakeDuration;
                ringAge = 0;
                ring.transform.position = new Vector3(camera.transform.position.x,0.08f,camera.transform.position.z);
                ring.transform.localScale = Vector3.one * 0.1f;
                ring.startColor = ring.endColor = new Color(1,.83f,.5f,.7f);
                ring.gameObject.SetActive(true);
                return;
            }
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
            ringAge += realDt;
            if (ringAge >= GameConfig.KiaiEffectDuration) ring.gameObject.SetActive(false);
            else
            {
                float progress = ringAge / GameConfig.KiaiEffectDuration;
                ring.transform.localScale = Vector3.one * Mathf.Lerp(.1f,GameConfig.KiaiRange,progress);
                ring.startColor = ring.endColor = new Color(1,.83f,.5f,(1-progress)*.7f);
            }
            hitStop = Mathf.Max(0, hitStop - realDt);
            shake = Mathf.Max(0, shake - realDt);
            visualClock += realDt;
            float amplitude = GameSettings.Shake ? GameConfig.ShakeAmplitude * shake / GameConfig.ShakeDuration : 0;
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
            string motionName = combat.Swinging ? (combat.Heavy ? "Sword_Attack" : "Sword_Attack_Standing") : "Sword_Idle";
            // Strike contact maps to the end of the wind-up, independently of recovery length.
            float contact = combat.Heavy ? GameConfig.HeavyWindup / (GameConfig.HeavyWindup + GameConfig.HeavyRecovery)
                : GameConfig.LightWindup / (GameConfig.LightWindup + GameConfig.LightRecovery);
            float phase = combat.Swinging ? (combat.SwingProgress < contact ? Mathf.Lerp(0, .48f, combat.SwingProgress / contact)
                : Mathf.Lerp(.48f, 1, (combat.SwingProgress - contact) / (1 - contact))) : Mathf.Repeat(visualClock / 1.666667f, 1);
            var motion = SwordMotion.Sample(motionName, phase);
            float weight = combat.Swinging ? .48f : combat.Guarding ? .025f : .08f;
            position += Vector3.ClampMagnitude(motion.hand, .65f) * weight;
            if (combat.DodgeRemaining > 0) position += new Vector3(-combat.DodgeDirection * .10f, -.08f, -.05f);
            Quaternion poseRotation = Quaternion.Euler(angles) * Quaternion.Slerp(Quaternion.identity, motion.rotation, weight * .6f);
            sword.localPosition = Vector3.Lerp(sword.localPosition, position, 1 - Mathf.Exp(-18 * dt));
            sword.localRotation = Quaternion.Slerp(sword.localRotation, poseRotation, 1 - Mathf.Exp(-18 * dt));
        }

        public void Reset()
        {
            hitStop = shake = 0;
            ringAge = float.MaxValue; ring.gameObject.SetActive(false);
            sparkAge = float.MaxValue;
            CameraOffset = Vector3.zero;
            foreach (var spark in sparks) spark.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            combat.Impacted -= OnImpact;
            Object.Destroy(sparkMaterial);
            Object.Destroy(ringMaterial);
            if (ring != null) Object.Destroy(ring.gameObject);
        }
    }
}
