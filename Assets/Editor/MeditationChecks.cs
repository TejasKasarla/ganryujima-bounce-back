using System;
using UnityEngine;
using Ganryujima.Core;
using Ganryujima.Combat;
using Ganryujima.Input.Face;
using Ganryujima.Story;

namespace Ganryujima.Editor
{
    public static class MeditationChecks
    {
        private static int checks;
        [UnityEditor.MenuItem("Ganryujima/Check meditation rules")]
        public static void Verify()
        {
            checks=0;
            var activity=new MeditationModel();
            var calm=new FaceState { tracking=true,calibrated=true,composure=.95f };
            activity.Tick(2,calm,false); Check(activity.Progress>0 && !activity.Completed,"Calm draws partial circle");
            float progress=activity.Progress; activity.Tick(5,calm,true);
            Check(activity.Progress==progress,"Pause cannot complete a circle");
            var agitated=calm; agitated.composure=.15f; activity.Tick(.02f,agitated,false);
            Check(activity.Progress==0 && activity.BrokenRemaining>0,"Agitation breaks and resets stroke");
            activity.Tick(.2f,calm,false); Check(activity.Progress==0,"Broken stroke remains visible before restarting");
            activity.Tick(GameConfig.MeditationBreakFlash,calm,false); activity.Tick(GameConfig.MeditationHoldSeconds,calm,false);
            Check(activity.Completed && activity.CompletedThisTick && activity.Progress==1,"Fresh calm completes circle");
            activity.Tick(.1f,calm,false); Check(!activity.CompletedThisTick,"Completion cue only fires once");
            activity.Tick(5,agitated,false); Check(activity.Completed,"Completed circle stays complete");
            activity.Reset(); Check(!activity.Completed && activity.Progress==0 && activity.BrokenRemaining==0,"New scene clears meditation state");
            activity.Tick(1,calm,false); var uncertain=calm; uncertain.composure=.6f; progress=activity.Progress;
            activity.Tick(1,uncertain,false); Check(activity.Progress==progress,"Intermediate composure stalls without breaking");
            uncertain.tracking=false; activity.Tick(10,uncertain,false);
            Check(activity.Progress==0 && !activity.SignalReady && !activity.Completed,"Missing tracking cannot heal and clears held time");
            uncertain=calm; uncertain.calibrated=false; activity.Tick(10,uncertain,false); Check(activity.Progress==0,"Uncalibrated face cannot heal");
            uncertain=calm; uncertain.composure=float.NaN; activity.Tick(10,uncertain,false); Check(!activity.SignalReady,"Invalid composure rejects progress");
            activity.Tick(0,calm,false); activity.Tick(-1,calm,false); Check(activity.Progress==0,"Nonpositive time cannot advance");
            var story=new StoryModel(JsonUtility.FromJson<StoryGraph>(Resources.Load<TextAsset>("Story/story").text));
            void Continue() { story.Continue(); story.Continue(); }
            Continue(); story.Choose(0); Continue(); story.ResolveDuel(false,2); Continue(); story.Choose(0);
            Check(story.Current.kind=="meditation" && story.Stats.Wounds==1,"Defeat reaches wounded meditation");
            Check(!story.CompleteMeditation(activity) && !story.Continue(),"Neither skip nor incomplete activity grants healing");
            activity.Tick(GameConfig.MeditationHoldSeconds,calm,false);
            Check(story.CompleteMeditation(activity) && story.Stats.Wounds==0 && story.Stats.Recoveries==1,"Completed activity heals one wound and adds recovery");
            Check(story.Stats.BounceBack==3 && !story.CompleteMeditation(activity),"Recovery counts once alongside recorded parries");
            Check(story.Continue() && story.Current.kind=="duel","Completed circle returns to duel");
            var combat=new CombatModel(); combat.Reset(story.Stats.Wounds);
            Check(combat.StaminaLimit==100 && combat.MovementScale==1,"Healing removes stamina and walking penalties");
            Check(!story.CompleteMeditation(activity),"Cannot farm healing outside meditation");
            // Skipping remains a deliberate wounded path and does not manufacture recoveries.
            story.ResolveDuel(false,0); Continue(); story.Choose(0); story.LeaveMeditation();
            Check(story.Stats.Wounds==1 && story.Stats.Recoveries==1,"Early departure preserves wound and tally");
            for(int i=0;i<2;i++) { story.ResolveDuel(false,0); Continue(); story.Choose(0); story.LeaveMeditation(); }
            story.ResolveDuel(false,0); Continue(); story.Choose(0);
            Check(story.CompleteMeditation(activity) && story.Stats.Wounds==2 && story.Stats.Recoveries==2,"One circle heals one of multiple wounds");
            Debug.Log($"GANRYUJIMA MEDITATION CHECKS PASS: {checks} assertions.");
        }
        private static void Check(bool value,string message) { checks++; if(!value) throw new Exception("Meditation check failed: "+message); }
    }
}
