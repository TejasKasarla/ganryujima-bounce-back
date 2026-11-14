using System;
using UnityEngine;
using Ganryujima.Core;

namespace Ganryujima.Combat
{
    public enum OpponentState { Idle, Circle, Windup, Feint, Strike, Recover, Guard, Recoil, Counter, Staggered, Yield, Victory, SwallowTurn }
    public enum OpponentId { Arima, Yoshioka, Kojiro }

    /// <summary>Deterministic tutorial FSM. Movement and strike timing share combat's local clock.</summary>
    public sealed class ArimaOpponent : IDisposable
    {
        private readonly CombatModel combat;
        private float age;
        private float staggerDuration = GameConfig.ArimaStagger;
        private int cycle;
        private bool struck, feint;
        private bool swallow,returnCut;
        public OpponentId Id { get; private set; }
        public string Name => Id==OpponentId.Kojiro?"Sasaki Kojiro":Id==OpponentId.Yoshioka?"Yoshioka Denshichiro":"Arima Kihei";
        public float MaximumHealth => Id==OpponentId.Kojiro?GameConfig.KojiroHealth:GameConfig.MaximumHealth;
        public float Reach => Id==OpponentId.Kojiro?GameConfig.KojiroReach:GameConfig.ArimaReach;
        public float WindupDuration => Id==OpponentId.Arima?GameConfig.ArimaWindup:Id==OpponentId.Yoshioka?GameConfig.YoshiokaWindup:GameConfig.KojiroWindup;
        private float Speed => Id==OpponentId.Arima?GameConfig.ArimaSpeed:Id==OpponentId.Yoshioka?GameConfig.YoshiokaSpeed:GameConfig.KojiroSpeed;
        private float CounterDuration => Id==OpponentId.Kojiro?GameConfig.KojiroCounter:GameConfig.ArimaCounter;
        public bool SwallowCut => swallow && (Telling || State==OpponentState.Strike);
        public bool RisingCut => returnCut;
        public OpponentState State { get; private set; }
        public Vector2 Position { get; private set; }
        public Vector2 Forward { get; private set; }
        public Stance Zone { get; private set; }
        public bool Telling => State == OpponentState.Windup || State==OpponentState.SwallowTurn;
        public bool CounterOpen => State == OpponentState.Counter;
        public float TellProgress => Telling ? Mathf.Clamp01(age / (State==OpponentState.SwallowTurn?GameConfig.SwallowTurnTime:WindupDuration)) : 0;
        public float StateAge => age;
        public float CounterRemaining => CounterOpen ? Mathf.Max(0, CounterDuration - age) : 0;

        public string ReadTell(bool focused) => focused && Telling ? (feint ? "FEINT — wait for the second tell" : SwallowCut ? (returnCut?"SWALLOW CUT — rising return":"SWALLOW CUT — low sweep, then rising return") : "COMMITTED CUT") : null;
        public void Configure(OpponentId id) { Id=id; Reset(); }
        public bool ReceiveKiai(float distance, float angle)
        {
            if (combat.Over || distance > GameConfig.KiaiRange || angle > GameConfig.KiaiAngle) return false;
            // Do not shorten a counter opportunity the player has already earned.
            if (State == OpponentState.Recoil || CounterOpen) return true;
            Set(OpponentState.Staggered);
            staggerDuration = GameConfig.KiaiStagger;
            return true;
        }

        public ArimaOpponent(CombatModel combat)
        {
            this.combat = combat;
            combat.ResolveTargetStrike = Defend;
            combat.Impacted += OnImpact;
            Reset();
        }
        public void Reset()
        {
            Position = new Vector2(0, 2); Forward = Vector2.down;
            cycle = 0; Zone = Stance.Upper; feint = struck = false;
            swallow=returnCut=false;
            Set(OpponentState.Idle);
        }
        private void Set(OpponentState next) { State = next; age = 0; struck = false; staggerDuration = GameConfig.ArimaStagger; }
        private TargetResponse Defend(bool heavy, Stance zone)
        {
            if (CounterOpen) return TargetResponse.Counter;
            return State == OpponentState.Guard && !heavy && zone == Zone ? TargetResponse.Block : TargetResponse.Hit;
        }
        private void OnImpact(Impact impact)
        {
            if (combat.TargetHealth <= 0) { Set(OpponentState.Yield); return; }
            if (combat.Health <= 0) { Set(OpponentState.Victory); return; }
            if (impact == Impact.Parry) Set(OpponentState.Recoil);
            else if (impact == Impact.HeavyHit || impact == Impact.CounterHit) Set(OpponentState.Staggered);
            // Light hits cannot indefinitely stun-lock Arima. Recoveries remain punishable.
            else if (impact == Impact.TargetHit && State == OpponentState.Guard) Set(OpponentState.Staggered);
        }

        public void Tick(float dt, Vector2 player, float playerFacingAngle)
        {
            if (dt <= 0) return;
            if (combat.Over) { Set(combat.TargetHealth <= 0 ? OpponentState.Yield : OpponentState.Victory); return; }
            age += dt;
            Vector2 delta = player - Position;
            float distance = delta.magnitude;
            // Commit to the cut at the end of its tell; circling can evade its facing cone.
            if (State != OpponentState.Strike && delta.sqrMagnitude > 0.001f) Forward = delta.normalized;
            switch (State)
            {
                case OpponentState.Idle:
                    if (age >= GameConfig.ArimaIdle) Set(OpponentState.Circle);
                    break;
                case OpponentState.Circle:
                    float spacing=Id==OpponentId.Kojiro?GameConfig.KojiroSpacing:GameConfig.ArimaSpacing;
                    float radial = distance > spacing + 0.12f ? 1 : distance < spacing - 0.25f ? -1 : 0;
                    Vector2 tangent = new Vector2(-Forward.y, Forward.x) * (cycle % 2 == 0 ? 1 : -1);
                    Move((Forward * radial + tangent * 0.32f) * (Speed * dt), player);
                    if (age >= GameConfig.ArimaCircle && distance <= Reach)
                    {
                        Zone = (Stance)(cycle % 4);
                        swallow=Id==OpponentId.Kojiro && cycle%3==2; returnCut=false;
                        if(swallow) Zone=Stance.Lower;
                        feint = Id==OpponentId.Yoshioka ? cycle%2==0 : cycle % 4 == 3;
                        if(swallow) feint=false;
                        cycle++;
                        Set(OpponentState.Windup);
                    }
                    break;
                case OpponentState.Windup:
                    if (feint && age >= WindupDuration * 0.55f) Set(OpponentState.Feint);
                    else if (age >= WindupDuration) Set(OpponentState.Strike);
                    break;
                case OpponentState.SwallowTurn:
                    if(age>=GameConfig.SwallowTurnTime) Set(OpponentState.Strike);
                    break;
                case OpponentState.Feint:
                    if (age >= GameConfig.ArimaFeint)
                    {
                        Zone = (Stance)(((int)Zone + 1) % 4); feint = false;
                        Set(OpponentState.Windup); // Fresh, full tell: tutorial feints never deal hidden instant damage.
                    }
                    break;
                case OpponentState.Strike:
                    Move(Forward * (GameConfig.ArimaLungeSpeed * dt), player);
                    if (!struck && age >= GameConfig.ArimaImpactAt)
                    {
                        struck = true;
                        delta = player - Position;
                        if (Vector2.Angle(Forward, delta) <= GameConfig.HitAngle)
                            combat.ReceiveStrike(Zone, delta.magnitude, playerFacingAngle, Reach, Id==OpponentId.Kojiro?GameConfig.KojiroDamage:GameConfig.ArimaDamage);
                    }
                    // Impact callbacks can replace this state with Recoil or Victory immediately.
                    if (State == OpponentState.Strike && age >= GameConfig.ArimaStrike)
                    {
                        if(swallow && !returnCut) { returnCut=true; Zone=Stance.Upper; Set(OpponentState.SwallowTurn); }
                        else Set(OpponentState.Recover);
                    }
                    break;
                case OpponentState.Recover:
                    if (age >= GameConfig.ArimaRecover) Set(OpponentState.Guard);
                    break;
                case OpponentState.Guard:
                    if (age >= GameConfig.ArimaGuard) Set(OpponentState.Circle);
                    break;
                case OpponentState.Recoil:
                    Move(-Forward * (GameConfig.ArimaRecoilSpeed * dt), player);
                    if (age >= GameConfig.ArimaRecoil) Set(OpponentState.Counter);
                    break;
                case OpponentState.Counter:
                    if (age >= CounterDuration) Set(OpponentState.Guard);
                    break;
                case OpponentState.Staggered:
                    if (age >= staggerDuration) Set(OpponentState.Guard);
                    break;
            }
        }
        private void Move(Vector2 offset, Vector2 player)
        {
            Vector2 next = Vector2.ClampMagnitude(Position + offset, GameConfig.ArenaRadius - GameConfig.ArimaRadius);
            float separation = GameConfig.PlayerRadius + GameConfig.ArimaRadius;
            if ((next - player).magnitude >= separation) Position = next;
        }
        public void Dispose() { combat.Impacted -= OnImpact; combat.ResolveTargetStrike = null; }
    }
}
