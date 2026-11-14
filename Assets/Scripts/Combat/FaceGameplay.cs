using System;
using UnityEngine;
using Ganryujima.Core;
using Ganryujima.Input.Face;

namespace Ganryujima.Combat
{
    /// <summary>Dev B's consumer of IFaceInput snapshots. Never accesses camera/provider internals.</summary>
    public sealed class FaceGameplay : IDisposable
    {
        private readonly CombatModel combat;
        private readonly ArimaOpponent opponent;
        private FaceState state;
        private int pendingDodge;
        private bool pendingKiai, active;
        private float dodgeBuffer, kiaiBuffer, focusTime, clashRemaining, clashScale = 1;
        public bool HasSignal { get; private set; }
        public float Lean { get; private set; }
        public bool GazeFocused => HasSignal && focusTime >= GameConfig.GazeFocusDuration;
        public float LocalTimeScale => clashRemaining > 0 ? clashScale : 1;
        public bool BufferedGesture => pendingDodge != 0 || pendingKiai;
        public string FeintHint => opponent.ReadTell(GazeFocused);

        public FaceGameplay(CombatModel combat, ArimaOpponent opponent)
        {
            this.combat = combat; this.opponent = opponent;
            combat.Impacted += OnImpact;
        }

        public void Observe(FaceState snapshot, bool gameplayActive)
        {
            state = snapshot;
            active = gameplayActive && !combat.Over;
            HasSignal = snapshot.tracking && snapshot.calibrated;
            combat.SetComposure(HasSignal ? snapshot.composure : GameConfig.NeutralComposure);
            if (!active || !HasSignal)
            {
                pendingDodge = 0; pendingKiai = false; focusTime = 0; Lean = 0;
                if (!HasSignal) clashRemaining = 0;
                return;
            }
            if (snapshot.leanTrigger == -1 || snapshot.leanTrigger == 1)
            { pendingDodge = snapshot.leanTrigger * GameConfig.LeanDirection; dodgeBuffer = GameConfig.GestureBuffer; }
            if (snapshot.kiai) { pendingKiai = true; kiaiBuffer = GameConfig.GestureBuffer; }
        }

        // This clock runs through local hit-stop, but never while the game is paused.
        public void Advance(float realDt, float distance, float aimAngle)
        {
            if (!active || realDt <= 0) return;
            clashRemaining = Mathf.Max(0, clashRemaining - realDt);
            dodgeBuffer -= realDt; kiaiBuffer -= realDt;
            if (dodgeBuffer <= 0) pendingDodge = 0;
            if (kiaiBuffer <= 0) pendingKiai = false;
            bool focusing = HasSignal && state.gaze == Gaze.center && distance <= GameConfig.GazeFocusRange && aimAngle <= GameConfig.GazeFocusAngle;
            focusTime = focusing ? Mathf.Min(GameConfig.GazeFocusDuration, focusTime + realDt) : 0;
            float target = HasSignal && !float.IsNaN(state.leanX) && !float.IsInfinity(state.leanX) ? Mathf.Clamp(state.leanX,-1,1) : 0;
            Lean = Mathf.Lerp(Lean, target * GameConfig.LeanDirection, 1 - Mathf.Exp(-GameConfig.LeanSmoothing * realDt));
        }

        public void ApplyGestures(bool canAct, float distance, float aimAngle)
        {
            if (!active || !HasSignal || !canAct) return;
            // Consume each pulse once. Only hit-stop buffers input; cooldowns do not queue repeated actions.
            if (pendingDodge != 0) { combat.TryDodge(pendingDodge); pendingDodge = 0; }
            if (pendingKiai)
            {
                if (combat.TryKiai()) opponent.ReceiveKiai(distance, aimAngle);
                pendingKiai = false;
            }
        }

        private void OnImpact(Impact impact)
        {
            if (!active || !HasSignal || combat.Composure < GameConfig.CalmThreshold) return;
            if (impact != Impact.Parry && impact != Impact.Block && impact != Impact.TargetBlock) return;
            clashRemaining = GameConfig.CalmClashDuration;
            clashScale = Mathf.Lerp(1,GameConfig.CalmClashScale,
                Mathf.InverseLerp(GameConfig.CalmThreshold,1,combat.Composure));
        }
        public void Reset()
        {
            state = default; active = HasSignal = pendingKiai = false;
            pendingDodge = 0; Lean = focusTime = dodgeBuffer = kiaiBuffer = clashRemaining = 0;
            clashScale = 1; combat.SetComposure(GameConfig.NeutralComposure);
        }
        public void Dispose() { combat.Impacted -= OnImpact; }
    }
}
