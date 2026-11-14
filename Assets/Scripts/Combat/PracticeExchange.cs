using UnityEngine;
using Ganryujima.Core;

namespace Ganryujima.Combat
{
    /// <summary>A fixed metronome for testing combat, not opponent AI. No FSM or decisions.</summary>
    public sealed class PracticeExchange
    {
        private readonly CombatModel combat;
        private float remaining;
        private int cycle;
        public Stance Zone => (Stance)(cycle % 4);
        public bool Telling { get; private set; }
        public float TellProgress => Telling ? 1 - Mathf.Clamp01(remaining / GameConfig.PracticeTell) : 0;

        public PracticeExchange(CombatModel combat) { this.combat = combat; Reset(); }
        public void Reset() { remaining = GameConfig.PracticeInterval; cycle = 0; Telling = false; }

        public void Tick(float dt, float distance, float angle)
        {
            if (dt <= 0 || combat.Over) return;
            // Restart the tell if the player leaves the practice area.
            if (distance > GameConfig.PracticeActivationRange) { remaining = GameConfig.PracticeInterval; Telling = false; return; }
            if (combat.RecoilRemaining > 0) { remaining = GameConfig.PracticeInterval; Telling = false; return; }
            remaining -= dt;
            Telling = remaining <= GameConfig.PracticeTell;
            if (remaining > 0) return;
            combat.ReceiveStrike(Zone, distance, angle);
            cycle++;
            remaining = GameConfig.PracticeInterval;
            Telling = false;
        }
    }
}
