using System;
using UnityEngine;
using Ganryujima.Core;
using Ganryujima.Combat;

namespace Ganryujima.Editor
{
    public static class OpponentChecks
    {
        private static int checks;
        [UnityEditor.MenuItem("Ganryujima/Check Arima rules")]
        public static void Verify()
        {
            checks = 0;
            var c = new CombatModel();
            using (var ai = new ArimaOpponent(c))
            {
                Vector2 player = Vector2.zero;
                void Step(float dt = .02f)
                {
                    c.Tick(dt, Vector2.Distance(ai.Position,player), 0);
                    ai.Tick(dt,player,0);
                }
                void Until(OpponentState state)
                {
                    int limit=1500;
                    while(ai.State != state && --limit>0) Step();
                    Check(limit>0,"Reaches " + state);
                }
                void Reset() { c.Reset(); ai.Reset(); }
                Until(OpponentState.Windup);
                Check(ai.Zone==Stance.Upper,"First attack announces upper zone");
                float age=ai.StateAge; Vector2 position=ai.Position;
                Step(0); Check(ai.StateAge==age && ai.Position==position,"Pause freezes AI clock and footwork");
                Until(OpponentState.Strike);
                c.Stance=ai.Zone; c.BeginGuard(); Step(.11f);
                Check(c.Parries==1 && ai.State==OpponentState.Recoil,"Real FSM strike triggers parry and recoil");
                float health=c.Health;
                Until(OpponentState.Counter);
                Check(c.Health==health && ai.CounterRemaining>1,"Recoil opens safe timed counter window");
                c.EndGuard(); c.BeginCharge(); c.ReleaseStrike(); Step(.13f);
                Check(Mathf.Abs(c.TargetHealth-(100-GameConfig.LightDamage*GameConfig.CounterDamageMultiplier))<.01f,"Counter bonus applies at contact");
                Check(ai.State==OpponentState.Staggered,"Counter consumes opening and staggers");
                Until(OpponentState.Guard);
                c.Stance=ai.Zone; c.BeginCharge(); c.ReleaseStrike(); Step(.13f);
                float afterCounter=c.TargetHealth;
                Check(Mathf.Abs(afterCounter-(100-GameConfig.LightDamage*GameConfig.CounterDamageMultiplier))<.01f,"Matching light strike blocked by enemy guard");
                Reset(); Until(OpponentState.Guard);
                c.Stance=(Stance)(((int)ai.Zone+1)%4); c.BeginCharge(); c.ReleaseStrike(); Step(.13f);
                Check(c.TargetHealth==100-GameConfig.LightDamage && ai.State==OpponentState.Staggered,"Changing zone defeats enemy guard");
                Reset(); c.BeginCharge(); Step(GameConfig.HeavyChargeTime); Until(OpponentState.Windup);
                c.ReleaseStrike(); Step(GameConfig.HeavyWindup+.01f);
                Check(ai.State==OpponentState.Staggered,"Precharged heavy strike interrupts the faster windup");
                Reset(); Until(OpponentState.Strike); player=new Vector2(0,-8); Step(.11f);
                Check(c.Health==100,"Out of reach at contact avoids damage");
                player=Vector2.zero; Reset(); Until(OpponentState.Strike);
                Vector2 originalForward=ai.Forward;
                player=ai.Position-originalForward*1.5f; Step(.11f);
                Check(c.Health==100,"Moving behind committed cut avoids damage");
                player=Vector2.zero; Reset(); Until(OpponentState.Feint);
                health=c.Health; Stance falseZone=ai.Zone;
                Until(OpponentState.Windup);
                Check(c.Health==health && ai.Zone!=falseZone && ai.TellProgress<.03f,"Feint causes no damage and gives fresh changed-zone tell");
                Reset(); Until(OpponentState.Strike); c.Stance=ai.Zone; c.BeginGuard(); Step(.11f);
                Until(OpponentState.Counter); Until(OpponentState.Guard);
                Check(!ai.CounterOpen,"Unused counter window expires");
                player=new Vector2(0,-12); Reset();
                float start=Vector2.Distance(ai.Position,player);
                for(int i=0;i<100;i++) Step();
                Check(Vector2.Distance(ai.Position,player)<start && ai.State==OpponentState.Circle,"Arima closes long distance before attacking");
                Check(ai.Position.magnitude<=GameConfig.ArenaRadius-GameConfig.ArimaRadius,"AI stays within court");
                player=Vector2.zero; Reset(); Until(OpponentState.Victory);
                Check(c.Over && c.Health==0,"Undefended duel reaches defeat");
                Reset();
                for(int i=0;i<3;i++)
                {
                    c.BeginCharge(); c.Tick(.56f,2,0); c.ReleaseStrike(); c.Tick(.9f,2,0);
                }
                Check(c.TargetHealth==0 && ai.State==OpponentState.Yield,"Lethal hit yields immediately");
                Reset(); Check(ai.State==OpponentState.Idle && ai.Position==new Vector2(0,2) && !c.Over && !ai.CounterOpen,"Retry resets FSM, pose position, health and counter");
            }
            Check(c.ResolveTargetStrike==null,"Disposal clears opponent combat hook");
            Debug.Log($"GANRYUJIMA ARIMA CHECKS PASS: {checks} assertions.");
        }
        private static void Check(bool value,string message) { checks++; if(!value) throw new Exception("Arima check failed: "+message); }
    }
}
