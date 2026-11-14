using System;
using UnityEngine;
using Ganryujima.Combat;
using Ganryujima.Core;
using Ganryujima.Story;

namespace Ganryujima.Editor
{
    public static class ContentChecks
    {
        private static int checks;
        [UnityEditor.MenuItem("Ganryujima/Check full game content")]
        public static void Verify()
        {
            checks=0;
            var c=new CombatModel();
            using(var ai=new ArimaOpponent(c))
            {
                ai.Configure(OpponentId.Yoshioka);
                for(int i=0;i<200 && ai.State!=OpponentState.Feint;i++) ai.Tick(.02f,Vector2.zero,0);
                Check(ai.State==OpponentState.Feint,"Yoshioka introduces feints immediately");
                ai.Configure(OpponentId.Kojiro); c.Reset(0,ai.MaximumHealth);
                Check(ai.Reach>GameConfig.HeavyReach && c.TargetHealthLimit==GameConfig.KojiroHealth,"Kojiro has long reach and higher vitality");
                void Reset() { c.Reset(0,GameConfig.KojiroHealth); ai.Configure(OpponentId.Kojiro); }
                void Tick() { c.Tick(.02f,10,0); ai.Tick(.02f,Vector2.zero,0); }
                void Until(Func<bool> condition)
                {
                    int budget=2500;
                    while(!condition() && --budget>0) Tick();
                    Check(budget>0,"AI reaches required phase");
                }
                Until(()=>ai.SwallowCut && ai.State==OpponentState.Windup);
                Check(ai.Zone==Stance.Lower && !ai.RisingCut,"Swallow announces low sweep");
                Check(ai.ReadTell(true).Contains("SWALLOW") && ai.ReadTell(false)==null,"Focused gaze reveals signature detail");
                float age=ai.StateAge; Vector2 position=ai.Position;
                ai.Tick(0,Vector2.zero,0); Check(ai.StateAge==age && ai.Position==position,"Pause freezes signature windup");
                Until(()=>ai.State==OpponentState.SwallowTurn);
                Check(ai.Zone==Stance.Upper && ai.RisingCut && ai.Telling,"Low sweep has a separately telegraphed rising return");
                float health=c.Health;
                ai.Tick(GameConfig.SwallowTurnTime*.5f,Vector2.zero,0);
                Check(ai.State==OpponentState.SwallowTurn && c.Health==health,"Rising tell cannot hit early");
                Until(()=>ai.State==OpponentState.Strike);
                c.Stance=Stance.Upper; c.BeginGuard(); Tick(); Tick(); Tick(); Tick(); Tick(); Tick();
                Check(ai.State==OpponentState.Recoil && c.Parries==1,"Rising cut parry creates real recoil");
                Until(()=>ai.CounterOpen); Check(ai.CounterRemaining<=GameConfig.KojiroCounter,"Kojiro counter window uses profile timing");
                Reset(); Until(()=>ai.SwallowCut && ai.State==OpponentState.Strike);
                c.Stance=Stance.Lower; c.BeginGuard();
                for(int i=0;i<6;i++) Tick();
                Check(ai.State==OpponentState.Recoil && c.Parries==1,"Parrying low sweep cancels the second cut");
                Reset(); Until(()=>ai.State==OpponentState.SwallowTurn);
                Check(ai.ReceiveKiai(2,0),"Kiai can interrupt signature return");
                ai.Tick(.2f,Vector2.zero,0); Check(ai.State==OpponentState.Staggered,"Interrupted return does not emit stale strike");
                Reset(); Until(()=>ai.State==OpponentState.SwallowTurn);
                ai.Tick(GameConfig.SwallowTurnTime+.01f,new Vector2(0,-10),0);
                ai.Tick(.12f,new Vector2(0,-10),0);
                Check(c.Health>0,"Retreat outside reach survives the return");
                ai.Configure(OpponentId.Arima); c.Reset(); Check(!ai.SwallowCut && ai.Name=="Arima Kihei" && ai.Reach==GameConfig.ArimaReach,"New encounter clears signature state");
            }
            var graph=JsonUtility.FromJson<StoryGraph>(Resources.Load<TextAsset>("Story/story").text);
            StoryModel Campaign(bool honorable,int venue,bool loseFinal=false)
            {
                var s=new StoryModel(graph);
                void Move() { AdvanceScenes(s); }
                Move(); s.Choose(honorable?0:2); Move(); s.ResolveDuel(true,2); Move(); s.Choose(honorable?0:1); Move();
                Check(s.Current.id=="venue" && s.Encounter==OpponentId.Yoshioka,"Arima aftermath reaches Yoshioka");
                s.Choose(venue); Move();
                Check(s.Arena==(venue==0?"court":venue==1?"grove":"lantern") && s.Current.id=="yoshioka","Venue choice changes Yoshioka arena");
                s.ResolveDuel(true,3); Move(); s.Choose(honorable?0:1); Move();
                Check(s.Encounter==OpponentId.Kojiro && s.Arena=="beach" && s.Current.id=="arrival_time","Yoshioka aftermath reaches island arrival choice");
                s.Choose(honorable?0:1); Move();
                Check(s.Current.id=="kojiro" && s.Arena==(honorable?"beach":"beach_late"),"Arrival choice carries to final duel");
                if(loseFinal)
                {
                    s.ResolveDuel(false,1); Move(); Check(s.Current.id=="k_return" && s.Stats.Wounds==1,"Final loss continues with wound");
                    s.Choose(1);
                }
                else { s.ResolveDuel(true,4); Move(); s.Choose(honorable?0:1); }
                return s;
            }
            var way=Campaign(true,1);
            Check(way.Current.id=="ending_way" && way.Stats.FinalVictory && way.Stats.Wins==3,"Honor route reaches The Way");
            var undefeated=Campaign(false,2);
            Check(undefeated.Current.id=="ending_undefeated" && undefeated.Stats.Parries==9,"Perfect fame route reaches The Undefeated with cumulative parries");
            var ronin=Campaign(false,0,true);
            Check(ronin.Current.id=="ending_ronin" && !ronin.Stats.FinalVictory && ronin.Stats.Losses==1,"Final departure reaches The Ronin");
            var retry=new StoryModel(graph); AdvanceScenes(retry); retry.Choose(0); AdvanceScenes(retry); retry.ResolveDuel(true,0); AdvanceScenes(retry); retry.Choose(0); AdvanceScenes(retry); retry.Choose(1); AdvanceScenes(retry);
            retry.ResolveDuel(false,0); AdvanceScenes(retry); retry.Choose(0);
            Check(retry.Current.kind=="meditation" && retry.Encounter==OpponentId.Yoshioka && retry.Arena=="grove","Yoshioka meditation preserves chosen venue and encounter");
            retry.LeaveMeditation(); Check(retry.Current.id=="yoshioka" && retry.Stats.Wounds==1,"Wounded retry returns to correct opponent");
            way.Reset(); Check(way.Encounter==OpponentId.Arima && way.Arena=="court" && !way.Stats.FinalVictory,"New journey restores Arima and clears ending flags");
            Debug.Log($"GANRYUJIMA CONTENT CHECKS PASS: {checks} assertions.");
        }
        public static void AdvanceScenes(StoryModel story)
        {
            int budget=100;
            while(story.Current.kind=="cutscene" && --budget>0) { story.Continue(); story.Continue(); }
            if(budget==0) throw new Exception("Story failed to reach input/duel/ending");
        }
        private static void Check(bool value,string message) { checks++; if(!value) throw new Exception("Content check failed: "+message); }
    }
}
