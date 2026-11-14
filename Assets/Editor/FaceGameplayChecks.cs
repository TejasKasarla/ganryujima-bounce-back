using System;
using UnityEngine;
using Ganryujima.Core;
using Ganryujima.Combat;
using Ganryujima.Input.Face;

namespace Ganryujima.Editor
{
    public static class FaceGameplayChecks
    {
        private static int checks;
        [UnityEditor.MenuItem("Ganryujima/Check face gameplay")]
        public static void Verify()
        {
            checks=0;
            var combat=new CombatModel();
            using(var ai=new ArimaOpponent(combat))
            using(var face=new FaceGameplay(combat,ai))
            {
                FaceState Calm() => new FaceState { tracking=true, calibrated=true, composure=1, gaze=Gaze.center };
                void Reset() { combat.Reset(); ai.Reset(); face.Reset(); }
                void Send(FaceState sample, bool active=true, bool canAct=true)
                { face.Observe(sample,active); face.Advance(.02f,2,0); face.ApplyGestures(canAct,2,0); }
                void Step(float dt)
                { combat.Tick(dt,2,0); ai.Tick(dt,Vector2.zero,0); }
                Reset(); var s=Calm(); s.leanTrigger=-1; Send(s);
                Check(combat.DodgeDirection==1 && combat.InvulnerableRemaining>0,"Negative provider pulse starts reversed right dodge with i-frames");
                Equal(combat.Stamina,100-GameConfig.DodgeCost,"Dodge spends stamina");
                combat.ReceiveStrike(Stance.Upper,2,0);
                Equal(combat.Health,100,"I-frames prevent an in-range unguarded hit");
                Check(!combat.BeginGuard() && !combat.BeginCharge() && !combat.TryRetreat(),"Dodge locks overlapping actions");
                Step(GameConfig.DodgeInvulnerability+.01f); combat.ReceiveStrike(Stance.Upper,2,0);
                Equal(combat.Health,100-GameConfig.PracticeDamage,"Late dodge recovery is vulnerable");
                Step(GameConfig.DodgeDuration); Check(!combat.TryDodge(1),"Cooldown outlasts movement");
                Step(GameConfig.DodgeCooldown); Check(combat.TryDodge(1),"Right dodge available after cooldown");
                Reset(); Check(!combat.TryDodge(0) && !combat.TryDodge(2),"Invalid lean values cannot dodge");
                combat.BeginCharge(); Check(combat.TryDodge(1) && !combat.Charging,"Dodge cancels pending charge");
                Reset(); combat.BeginCharge(); combat.ReleaseStrike(); Check(!combat.TryDodge(1),"Cannot cancel committed strike into free immunity");
                Reset(); for(int i=0;i<6;i++) { combat.BeginCharge(); combat.ReleaseStrike(); combat.Tick(.47f,10,0); }
                Check(!combat.TryDodge(1) && !combat.TryKiai(),"Low stamina prevents face abilities");

                Reset(); s=Calm(); s.leanTrigger=1; s.leanX=1; Send(s);
                Check(combat.DodgeDirection==-1 && face.Lean<0,"Positive provider lean reverses both dodge and camera lean");
                Reset(); s=Calm(); s.leanX=-1; Send(s);
                Check(face.Lean>0,"Negative continuous lean reverses camera direction");
                Reset(); s=Calm(); s.kiai=true; Send(s);
                Check(ai.State==OpponentState.Staggered,"Kiai staggers opponent in range");
                Equal(combat.KiaiCooldown,GameConfig.KiaiCooldown,"Kiai cooldown begins at eight seconds");
                Equal(combat.Stamina,100-GameConfig.KiaiCost,"Kiai cost charged once");
                face.ApplyGestures(true,2,0); Equal(combat.Stamina,100-GameConfig.KiaiCost,"One pulse cannot spend twice");
                Check(!combat.TryKiai(),"Kiai cannot spam during cooldown");
                Step(GameConfig.ArimaStagger+.01f); Check(ai.State==OpponentState.Staggered,"Kiai has its configured longer stagger");
                Step(GameConfig.KiaiStagger); Check(ai.State==OpponentState.Guard,"Kiai stagger recovers");
                Reset(); Check(!ai.ReceiveKiai(GameConfig.KiaiRange+.01f,0),"Kiai range is bounded");
                Check(!ai.ReceiveKiai(2,GameConfig.KiaiAngle+1),"Kiai cannot stagger behind player");
                Check(ai.ReceiveKiai(2,0) && combat.TargetHealth==100 && combat.Parries==0,"Kiai neither damages nor awards a parry");
                Reset(); combat.BeginGuard(); combat.ReceiveStrike(combat.Stance,2,0);
                Check(ai.State==OpponentState.Recoil && ai.ReceiveKiai(2,0) && ai.State==OpponentState.Recoil,"Kiai preserves earned parry recoil");

                Reset(); s=Calm(); s.composure=0; Send(s); combat.BeginGuard(); combat.Tick(.15f,2,0);
                Check(!combat.ParryOpen,"Low composure closes parry before 150ms");
                s.composure=1; Send(s); Check(!combat.ParryOpen,"Composure change cannot reopen existing guard");
                combat.EndGuard(); combat.Tick(GameConfig.GuardRearm+.01f,2,0); combat.BeginGuard(); combat.Tick(.21f,2,0);
                Check(combat.ParryOpen,"High composure guard stays open past base window");
                combat.ReceiveStrike(combat.Stance,2,0);
                Check(combat.Parries==1 && face.LocalTimeScale<1,"Calm clash starts local slow motion");
                Equal(Time.timeScale,1,"Face gameplay does not change Unity global time scale");
                float scale=face.LocalTimeScale; face.Observe(Calm(),false); face.Advance(1,2,0);
                Equal(face.LocalTimeScale,scale,"Pause freezes clash duration");
                face.Observe(Calm(),true); face.Advance(GameConfig.CalmClashDuration+.01f,2,0);
                Equal(face.LocalTimeScale,1,"Clash slow motion expires");
                Reset(); s=Calm(); s.composure=0; Send(s); combat.BeginGuard(); combat.ReceiveStrike(combat.Stance,2,0);
                Equal(face.LocalTimeScale,1,"Agitated parry does not slow time");

                Reset(); s=Calm(); s.tracking=false; s.leanTrigger=1; s.kiai=true; Send(s);
                Check(combat.DodgeRemaining==0 && combat.KiaiCooldown==0 && !face.HasSignal,"Tracking loss rejects stale events");
                Equal(combat.ParryWindow,GameConfig.ParryWindow,"Tracking loss uses neutral base parry");
                s.tracking=true; s.calibrated=false; Send(s); Check(combat.DodgeRemaining==0,"Uncalibrated tracking cannot trigger dodge");
                s=Calm(); s.composure=float.NaN; s.leanX=float.PositiveInfinity; Send(s);
                Equal(combat.Composure,GameConfig.NeutralComposure,"Invalid composure sanitized"); Equal(face.Lean,0,"Invalid lean sanitized");
                Reset(); s=Calm(); s.leanTrigger=1; s.kiai=true; Send(s,false);
                Send(Calm()); Check(combat.DodgeRemaining==0 && combat.KiaiCooldown==0,"Paused pulses are discarded on resume");
                Reset(); s=Calm(); s.leanTrigger=1; Send(s,true,false);
                Check(face.BufferedGesture && combat.DodgeRemaining==0,"Hit-stop buffers one-frame gesture");
                Send(Calm()); Check(combat.DodgeRemaining>0 && !face.BufferedGesture,"Buffered gesture consumed after hit-stop");
                Reset(); Send(s,true,false); face.Advance(GameConfig.GestureBuffer+.01f,2,0); face.ApplyGestures(true,2,0);
                Check(combat.DodgeRemaining==0,"Expired gesture does not trigger late");

                Reset(); Send(Calm()); face.Advance(GameConfig.GazeFocusDuration,2,0);
                Check(face.GazeFocused,"Centered gaze acquires focus while aiming at opponent");
                face.Advance(.01f,2,GameConfig.GazeFocusAngle+1); Check(!face.GazeFocused,"Looking away removes focus");
                face.Advance(1,GameConfig.GazeFocusRange+1,0); Check(!face.GazeFocused,"Cannot read distant feints");
                s=Calm(); s.gaze=Gaze.unknown; Send(s); face.Advance(1,2,0); Check(!face.GazeFocused,"Unknown gaze degrades to ordinary tells");
                // Survive three honest attacks, then read the fourth attack before its physical withdrawal.
                Reset(); face.Observe(Calm(),true); face.Advance(1,2,0);
                int budget=2000;
                while(face.FeintHint?.StartsWith("FEINT")!=true && --budget>0) Step(.02f);
                Check(budget>0 && ai.Telling,"Focused gaze reveals feint during initial windup");
                s=Calm(); s.gaze=Gaze.left; Send(s); Check(face.FeintHint==null,"Off-center gaze hides hidden feint information");
                Reset(); Check(!face.BufferedGesture && !face.GazeFocused && face.LocalTimeScale==1 && combat.KiaiCooldown==0,"Retry clears all face gameplay state");
            }
            Debug.Log($"GANRYUJIMA FACE GAMEPLAY CHECKS PASS: {checks} assertions.");
        }
        private static void Check(bool condition,string message) { checks++; if(!condition) throw new Exception("Face gameplay check failed: "+message); }
        private static void Equal(float actual,float expected,string message) => Check(Mathf.Abs(actual-expected)<.001f,message);
    }
}
