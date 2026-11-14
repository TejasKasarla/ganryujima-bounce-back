using System;
using UnityEngine;
using Ganryujima.Combat;
using Ganryujima.Core;

namespace Ganryujima.Editor
{
    /// <summary>Deterministic combat regressions, runnable without external test packages.</summary>
    public static class CombatChecks
    {
        private const float Near = 2f;
        private static int assertions;

        [UnityEditor.MenuItem("Ganryujima/Check combat rules")]
        public static void Verify()
        {
            assertions = 0;
            var c = new CombatModel();
            int hits = 0;
            c.Impacted += impact => { if (impact == Impact.TargetHit) hits++; };
            Check(c.BeginCharge() && c.ReleaseStrike(), "Light strike starts");
            Check(!c.BeginCharge() && !c.BeginGuard(), "Attack locks out overlapping actions");
            c.Tick(GameConfig.LightWindup + 0.001f, Near, 0);
            Equal(c.TargetHealth, GameConfig.MaximumHealth - GameConfig.LightDamage, "Light damage");
            c.Tick(GameConfig.LightRecovery + 0.01f, Near, 0);
            Check(hits == 1, "Each swing resolves once");
            Equal(c.Stamina, GameConfig.MaximumStamina - GameConfig.LightCost, "Light stamina cost");

            c.Reset(); c.BeginCharge(); c.ReleaseStrike();
            c.Tick(1, GameConfig.HeavyReach + 1, 0);
            Equal(c.TargetHealth, GameConfig.MaximumHealth, "Miss outside reach");
            c.Reset(); c.BeginCharge(); c.ReleaseStrike(); c.Tick(1, Near, GameConfig.HitAngle + 1);
            Equal(c.TargetHealth, GameConfig.MaximumHealth, "Miss outside aim cone");

            c.Reset(); c.BeginCharge(); c.Tick(GameConfig.HeavyChargeTime + 0.01f, Near, 0);
            Check(c.ChargeProgress == 1 && c.ReleaseStrike() && c.Heavy, "Hold produces heavy");
            c.Tick(GameConfig.HeavyWindup + 0.01f, Near, 0);
            Equal(c.TargetHealth, GameConfig.MaximumHealth - GameConfig.HeavyDamage, "Heavy damage");
            Equal(c.Stamina, GameConfig.MaximumStamina - GameConfig.HeavyCost, "Heavy cost");

            c.Reset();
            for (int i = 0; i < 5; i++) { c.BeginCharge(); c.ReleaseStrike(); c.Tick(GameConfig.LightWindup + GameConfig.LightRecovery + 0.001f, 10, 0); }
            Check(c.BeginCharge(), "Can charge with enough for light");
            c.Tick(GameConfig.HeavyChargeTime + 0.01f, 10, 0);
            Check(!c.ReleaseStrike() && !c.Swinging, "Unaffordable heavy cannot attack for free");
            float spent = c.Stamina;
            c.Tick(GameConfig.RegenDelay + 0.01f, Near, 0);
            Check(c.Stamina > spent && c.Stamina <= GameConfig.MaximumStamina, "Delayed stamina regeneration");

            c.Reset(); c.Stance = Stance.Upper; Check(c.BeginGuard(), "Guard starts");
            c.Tick(GameConfig.ParryWindow * 0.5f, Near, 0);
            c.ReceiveStrike(Stance.Upper, Near, 0);
            Check(c.Parries == 1 && c.RecoilRemaining > 0, "Timed matching guard parries and recoils");
            Equal(c.Health, GameConfig.MaximumHealth, "Parry prevents health damage");
            c.Reset(); c.Stance = Stance.Upper; c.BeginGuard();
            c.Tick(GameConfig.ParryWindow + 0.01f, Near, 0);
            c.ReceiveStrike(Stance.Upper, Near, 0);
            Check(c.Parries == 0 && c.Stamina < GameConfig.MaximumStamina - GameConfig.GuardHitCost, "Late guard blocks at stamina cost");
            Equal(c.Health, GameConfig.MaximumHealth, "Block prevents health damage");

            c.Reset(); c.Stance = Stance.Lower; c.BeginGuard(); c.ReceiveStrike(Stance.Upper, Near, 0);
            Equal(c.Health, GameConfig.MaximumHealth - GameConfig.PracticeDamage, "Wrong stance does not block");
            c.Reset(); c.Stance = Stance.Upper; c.BeginGuard(); c.ReceiveStrike(Stance.Upper, Near, 180);
            Equal(c.Health, GameConfig.MaximumHealth - GameConfig.PracticeDamage, "Cannot guard behind the player");
            c.Reset(); c.ReceiveStrike(Stance.Upper, GameConfig.PracticeReach + 1, 0);
            Equal(c.Health, GameConfig.MaximumHealth, "Retreat outside reach avoids a hit");

            c.Reset(); c.Stance = Stance.Upper; c.BeginGuard(); c.Tick(GameConfig.ParryWindow + 0.01f, Near, 0);
            for (int i = 0; i < 5; i++) c.ReceiveStrike(Stance.Upper, Near, 0);
            Check(c.BrokenRemaining > 0 && !c.Guarding && !c.BeginGuard(), "Stamina depletion breaks and locks guard");
            Equal(c.Health, GameConfig.MaximumHealth - GameConfig.GuardBreakDamage, "Broken guard chip damage");
            c.Tick(GameConfig.GuardBreakDuration + 0.01f, Near, 0);
            Check(c.BrokenRemaining == 0 && c.BeginGuard(), "Guard recovers after break");

            c.Reset(); c.BeginGuard(); c.EndGuard();
            Check(!c.BeginGuard(), "Guard spam has rearm delay");
            c.Tick(GameConfig.GuardRearm + 0.01f, Near, 0);
            Check(c.BeginGuard(), "Guard rearm expires");
            Check(!c.BeginCharge(), "Cannot charge while guarding");
            c.CancelHeldInput(); Check(!c.Charging && !c.Guarding, "Pause cancels held actions");

            c.Reset(); Check(c.TryRetreat(), "Retreat starts");
            Equal(c.Stamina, GameConfig.MaximumStamina - GameConfig.RetreatCost, "Retreat costs stamina");
            c.ReceiveStrike(Stance.Middle, Near, 0);
            Equal(c.Health, GameConfig.MaximumHealth - GameConfig.PracticeDamage, "Footwork retreat has no i-frames");
            c.Tick(GameConfig.RetreatDuration + 0.01f, Near, 0);
            Check(!c.TryRetreat(), "Retreat cooldown prevents repeats");

            c.Reset(); c.BeginCharge(); c.Tick(0, Near, 0);
            Equal(c.Charge, 0, "Paused clock does not charge");
            c.Reset(); var practice = new PracticeExchange(c);
            practice.Tick(GameConfig.PracticeInterval, 10, 0);
            Check(!practice.Telling, "Practice timer waits outside activation range");
            practice.Tick(GameConfig.PracticeInterval - 0.05f, Near, 0);
            Check(practice.Telling && practice.Zone == Stance.Upper, "Practice announces first strike zone");
            c.Stance = Stance.Upper; c.BeginGuard(); c.Tick(0.06f, Near, 0); practice.Tick(0.06f, Near, 0);
            Check(c.Parries == 1 && practice.Zone == Stance.Middle, "Fixed practice strike uses real parry rules");

            c.Reset();
            for (int i = 0; i < 6; i++) c.ReceiveStrike(Stance.Middle, Near, 0);
            Check(c.Over && c.Health == 0 && !c.BeginCharge() && !c.BeginGuard(), "Defeat clamps health and ends actions");
            c.Reset(); Check(!c.Over && c.Parries == 0 && !c.Swinging && !c.Guarding, "Retry resets combat state");
            Debug.Log($"GANRYUJIMA COMBAT CHECKS PASS: {assertions} assertions.");
        }

        private static void Check(bool condition, string name)
        {
            assertions++;
            if (!condition) throw new Exception("Combat check failed: " + name);
        }
        private static void Equal(float actual, float expected, string name) { Check(Mathf.Abs(actual - expected) < 0.01f, name + $" ({actual} vs {expected})"); }
    }
}
