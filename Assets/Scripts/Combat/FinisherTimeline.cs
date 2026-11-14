using UnityEngine;
using Ganryujima.Core;

namespace Ganryujima.Combat
{
    /// <summary>One cinematic per victory, independent of damage and the global Unity clock.</summary>
    public sealed class FinisherTimeline
    {
        public bool Started { get; private set; }
        public bool Active { get; private set; }
        public bool Complete => Started && !Active;
        public float Elapsed { get; private set; }
        public bool ImpactThisTick { get; private set; }
        public bool Begin(CombatModel combat)
        {
            if (Started || combat.TargetHealth > 0 || combat.Health <= 0) return false;
            Started = Active = true; Elapsed = 0; ImpactThisTick = false;
            return true;
        }
        public void Tick(float dt, bool paused, bool skip)
        {
            ImpactThisTick = false;
            if (!Active || paused) return;
            if (skip) { Active = false; Elapsed = GameConfig.FinisherDuration; return; }
            float before = Elapsed;
            Elapsed = Mathf.Min(GameConfig.FinisherDuration,Elapsed + Mathf.Max(0,dt));
            ImpactThisTick = before < GameConfig.FinisherImpactAt && Elapsed >= GameConfig.FinisherImpactAt;
            if (Elapsed >= GameConfig.FinisherDuration) Active = false;
        }
        public void Reset() { Started = Active = ImpactThisTick = false; Elapsed = 0; }
    }
}
