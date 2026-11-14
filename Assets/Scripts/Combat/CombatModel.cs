using System;
using UnityEngine;
using Ganryujima.Core;

namespace Ganryujima.Combat
{
    public enum Stance { Upper, Middle, Lower, Side }
    public enum Impact { TargetHit, HeavyHit, Miss, Block, Parry, PlayerHit, GuardBreak, TargetBlock, CounterHit, Dodge, Evade, Kiai }
    public enum TargetResponse { Hit, Block, Counter }

    /// <summary>Deterministic rules, independent of Unity input, visuals, and face tracking.</summary>
    public sealed class CombatModel
    {
        public event Action<Impact> Impacted;
        public Func<bool, Stance, TargetResponse> ResolveTargetStrike;
        public float Health { get; private set; }
        public float Stamina { get; private set; }
        public float StaminaLimit { get; private set; }
        public int Wounds { get; private set; }
        public float TargetHealth { get; private set; }
        public float TargetHealthLimit { get; private set; }
        public Stance Stance { get; set; }
        public Stance AttackStance { get; private set; }
        public bool Guarding { get; private set; }
        public bool Charging { get; private set; }
        public bool Heavy { get; private set; }
        public float Charge { get; private set; }
        public float BrokenRemaining { get; private set; }
        public float RetreatRemaining { get; private set; }
        public float RecoilRemaining { get; private set; }
        public float Composure { get; private set; }
        public float ParryWindow => Guarding ? guardWindow : Mathf.Lerp(GameConfig.LowComposureParry, GameConfig.HighComposureParry, Composure);
        public float DodgeRemaining { get; private set; }
        public float InvulnerableRemaining { get; private set; }
        public float DodgeCooldown { get; private set; }
        public float KiaiCooldown { get; private set; }
        public int DodgeDirection { get; private set; }
        public int Parries { get; private set; }
        public string Message { get; private set; }
        public float MessageRemaining { get; private set; }
        public bool Over => Health <= 0 || TargetHealth <= 0;
        public bool Swinging => swingDuration > 0;
        public bool ParryOpen => Guarding && clock - guardStart <= guardWindow;
        public float ChargeProgress => Mathf.Clamp01(Charge / GameConfig.HeavyChargeTime);
        public float SwingProgress => Swinging ? Mathf.Clamp01(swingAge / swingDuration) : 0;
        public float MovementScale => (1-Wounds*GameConfig.WoundSpeedPenalty) * (Stamina < GameConfig.ExhaustedThreshold ? GameConfig.ExhaustedSpeed : 1) *
            (Guarding ? GameConfig.GuardSpeed : Charging ? GameConfig.ChargeSpeed : Swinging ? GameConfig.AttackSpeed : 1);
        private float clock, lastSpend, guardStart, guardLock, retreatCooldown, guardWindow;
        private float swingAge, swingDuration, strikeAt;
        private bool resolved;

        public CombatModel() { Reset(); }

        public void Reset(int wounds = 0,float targetHealth = GameConfig.MaximumHealth)
        {
            Health = GameConfig.MaximumHealth;
            TargetHealth = TargetHealthLimit = Mathf.Max(1,targetHealth);
            Wounds=Mathf.Clamp(wounds,0,GameConfig.MaximumWounds);
            StaminaLimit=GameConfig.MaximumStamina-Wounds*GameConfig.WoundStaminaPenalty;
            Stamina = StaminaLimit;
            Stance = AttackStance = Stance.Middle;
            Guarding = Charging = Heavy = resolved = false;
            Charge = BrokenRemaining = RetreatRemaining = RecoilRemaining = 0;
            clock = guardLock = retreatCooldown = swingAge = swingDuration = strikeAt = 0;
            lastSpend = guardStart = -100;
            Parries = 0;
            Composure = GameConfig.NeutralComposure;
            guardWindow = GameConfig.ParryWindow;
            DodgeRemaining = InvulnerableRemaining = DodgeCooldown = KiaiCooldown = 0;
            DodgeDirection = 0;
            Say("Close the distance. Match the tell, then time your guard.");
        }

        private bool Free => !Over && !Swinging && BrokenRemaining <= 0 && RetreatRemaining <= 0 && DodgeRemaining <= 0;

        public void SetComposure(float value)
        {
            Composure = float.IsNaN(value) || float.IsInfinity(value) ? GameConfig.NeutralComposure : Mathf.Clamp01(value);
        }

        public bool TryDodge(int direction)
        {
            if ((direction != -1 && direction != 1) || !Free || DodgeCooldown > 0 || Stamina < GameConfig.DodgeCost) return false;
            CancelHeldInput(); Spend(GameConfig.DodgeCost);
            DodgeDirection = direction;
            DodgeRemaining = GameConfig.DodgeDuration;
            InvulnerableRemaining = GameConfig.DodgeInvulnerability;
            DodgeCooldown = GameConfig.DodgeCooldown;
            Emit(Impact.Dodge, direction < 0 ? "DODGE LEFT" : "DODGE RIGHT");
            return true;
        }

        public bool TryKiai()
        {
            if (!Free || Charging || KiaiCooldown > 0 || Stamina < GameConfig.KiaiCost) return false;
            EndGuard(); Spend(GameConfig.KiaiCost);
            KiaiCooldown = GameConfig.KiaiCooldown;
            Emit(Impact.Kiai, "KIAI — break their rhythm!");
            return true;
        }

        public bool BeginCharge()
        {
            if (!Free || Guarding || Charging) return false;
            if (Stamina < GameConfig.LightCost) { Say("Catch your breath — low stamina."); return false; }
            Charging = true;
            Charge = 0;
            return true;
        }

        public bool ReleaseStrike()
        {
            if (!Charging) return false;
            Charging = false;
            Heavy = Charge >= GameConfig.HeavyChargeTime;
            float cost = Heavy ? GameConfig.HeavyCost : GameConfig.LightCost;
            if (Stamina < cost) { Say("Not enough stamina for that strike."); Charge = 0; return false; }
            Spend(cost);
            AttackStance = Stance;
            strikeAt = Heavy ? GameConfig.HeavyWindup : GameConfig.LightWindup;
            swingDuration = strikeAt + (Heavy ? GameConfig.HeavyRecovery : GameConfig.LightRecovery);
            swingAge = 0;
            resolved = false;
            Charge = 0;
            return true;
        }

        public bool BeginGuard()
        {
            if (!Free || Charging || Guarding || guardLock > 0 || Stamina < GameConfig.GuardMinimum) return false;
            guardWindow = ParryWindow; // Snapshot on press: changing composure cannot reopen an old guard.
            Guarding = true;
            guardStart = clock;
            return true;
        }

        public void EndGuard()
        {
            if (Guarding) guardLock = GameConfig.GuardRearm;
            Guarding = false;
        }

        public void CancelHeldInput() { Charging = false; Charge = 0; EndGuard(); }

        public bool TryRetreat()
        {
            if (!Free || Charging || retreatCooldown > 0 || Stamina < GameConfig.RetreatCost) return false;
            EndGuard();
            Spend(GameConfig.RetreatCost);
            RetreatRemaining = GameConfig.RetreatDuration;
            retreatCooldown = GameConfig.RetreatCooldown;
            return true; // Keyboard retreat is footwork only. Face dodge has its own i-frames.
        }

        public void Tick(float dt, float distance, float aimAngle)
        {
            if (dt <= 0 || Over) return;
            clock += dt;
            DodgeRemaining = Mathf.Max(0, DodgeRemaining - dt);
            InvulnerableRemaining = Mathf.Max(0, InvulnerableRemaining - dt);
            DodgeCooldown = Mathf.Max(0, DodgeCooldown - dt);
            KiaiCooldown = Mathf.Max(0, KiaiCooldown - dt);
            guardLock = Mathf.Max(0, guardLock - dt);
            retreatCooldown = Mathf.Max(0, retreatCooldown - dt);
            BrokenRemaining = Mathf.Max(0, BrokenRemaining - dt);
            RetreatRemaining = Mathf.Max(0, RetreatRemaining - dt);
            RecoilRemaining = Mathf.Max(0, RecoilRemaining - dt);
            MessageRemaining = Mathf.Max(0, MessageRemaining - dt);
            if (Charging) Charge = Mathf.Min(GameConfig.HeavyChargeTime, Charge + dt);
            if (Guarding)
            {
                Spend(GameConfig.GuardDrain * dt);
                if (Stamina <= 0) BreakGuard(false);
            }
            else if (!Charging && !Swinging && clock - lastSpend >= GameConfig.RegenDelay)
                Stamina = Mathf.Min(StaminaLimit, Stamina + GameConfig.StaminaRegen * dt);
            if (!Swinging) return;
            swingAge += dt;
            if (!resolved && swingAge >= strikeAt)
            {
                resolved = true;
                float reach = Heavy ? GameConfig.HeavyReach : GameConfig.LightReach;
                if (distance <= reach && aimAngle <= GameConfig.HitAngle)
                {
                    var response = ResolveTargetStrike?.Invoke(Heavy, AttackStance) ?? TargetResponse.Hit;
                    if (response == TargetResponse.Block) Emit(Impact.TargetBlock, "OPPONENT GUARDS — change zone or use a heavy cut.");
                    else
                    {
                        float multiplier = response == TargetResponse.Counter ? GameConfig.CounterDamageMultiplier : 1;
                        TargetHealth = Mathf.Max(0, TargetHealth - (Heavy ? GameConfig.HeavyDamage : GameConfig.LightDamage) * multiplier);
                        Emit(response == TargetResponse.Counter ? Impact.CounterHit : Heavy ? Impact.HeavyHit : Impact.TargetHit,
                            response == TargetResponse.Counter ? "COUNTER — an opening earned!" : Heavy ? "HEAVY HIT" : "HIT");
                    }
                }
                else Emit(Impact.Miss, "Out of reach or off target.");
            }
            if (swingAge >= swingDuration) swingDuration = 0;
        }

        public void ReceiveStrike(Stance zone, float distance, float facingAngle,
            float reach = GameConfig.PracticeReach, float damage = GameConfig.PracticeDamage)
        {
            if (Over || distance > reach || RecoilRemaining > 0) return;
            if (InvulnerableRemaining > 0) { Emit(Impact.Evade, "EVADED — find your opening."); return; }
            if (Guarding && Stance == zone && facingAngle <= GameConfig.GuardAngle)
            {
                if (ParryOpen)
                {
                    Parries++;
                    Stamina = Mathf.Min(StaminaLimit, Stamina + GameConfig.ParryRefund);
                    RecoilRemaining = GameConfig.PracticeRecoil;
                    Emit(Impact.Parry, "PARRY — bounce back!");
                }
                else if (Stamina >= GameConfig.GuardHitCost)
                {
                    Spend(GameConfig.GuardHitCost);
                    Emit(Impact.Block, "BLOCK — guard costs stamina.");
                }
                else BreakGuard(true);
                return;
            }
            Health = Mathf.Max(0, Health - damage);
            // A hit interrupts a charged or pending strike, never refunding spent stamina.
            CancelHeldInput();
            swingDuration = 0;
            Emit(Impact.PlayerHit, "HIT TAKEN — face your opponent and match the zone.");
        }

        private void BreakGuard(bool damage)
        {
            CancelHeldInput();
            BrokenRemaining = GameConfig.GuardBreakDuration;
            Stamina = 0;
            lastSpend = clock;
            if (damage) Health = Mathf.Max(0, Health - GameConfig.GuardBreakDamage);
            Emit(Impact.GuardBreak, "GUARD BROKEN — recover your footing.");
        }

        private void Spend(float amount) { Stamina = Mathf.Max(0, Stamina - amount); lastSpend = clock; }
        private void Say(string message) { Message = message; MessageRemaining = GameConfig.FeedbackDuration; }
        private void Emit(Impact impact, string message) { Say(message); Impacted?.Invoke(impact); }
    }
}
