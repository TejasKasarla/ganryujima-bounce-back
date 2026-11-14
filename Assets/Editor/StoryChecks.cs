using System;
using UnityEngine;
using Ganryujima.Core;
using Ganryujima.Combat;
using Ganryujima.Story;
using Ganryujima.Input.Face;

namespace Ganryujima.Editor
{
    public static class StoryChecks
    {
        private static int assertions;
        [UnityEditor.MenuItem("Ganryujima/Check story rules")]
        public static void Verify()
        {
            assertions=0;
            var asset=Resources.Load<TextAsset>("Story/story");
            Check(asset!=null,"Bundled story JSON loads");
            var graph=JsonUtility.FromJson<StoryGraph>(asset.text);
            var story=new StoryModel(graph);
            void Continue() { story.Continue(); story.Continue(); }
            void Duel() { Continue(); story.Choose(0); Continue(); }
            Check(story.Current.id=="arrival","Graph begins at introduction");
            story.Tick(.2f,false); Check(story.RevealedCharacters>0 && !story.TextComplete,"Typewriter progressively reveals text");
            int shown=story.RevealedCharacters; story.Tick(2,true); Check(story.RevealedCharacters==shown,"Pause freezes typewriter");
            Check(!story.Continue() && story.TextComplete,"First skip reveals remaining line");
            Check(story.Continue() && story.Current.id=="approach","Second skip advances node");
            Check(!story.Choose(3) && story.Stats.Honor==0,"Invalid choice cannot apply effects");
            story.Choose(0); Check(story.Stats.Honor==2 && !story.Choose(0),"Choice effect applies once");
            Continue(); Check(story.Current.kind=="duel","Introduction hands off to duel");
            story.ResolveDuel(true,3); Check(!story.ResolveDuel(true,3) && story.Stats.Parries==3 && story.Stats.Wins==1,"Duel result records once");
            Continue(); Check(story.Current.id=="mercy","Win reaches binary aftermath choice");
            story.Choose(0); Check(story.Current.id=="mercy_end" && story.Stats.Honor==5 && story.Stats.Fame==1,"Mercy branch produces Honor and chapter ending");
            Check(story.Stats.BounceBack==3 && !story.Continue(),"Summary tally computed, ending cannot skip to missing node");
            story.Reset(); Duel(); story.ResolveDuel(false,2);
            Check(story.Stats.Wounds==1 && story.Stats.Losses==1 && story.Current.id=="fallen","Defeat continues with wound");
            Continue(); story.Choose(0); Check(story.Current.kind=="meditation","Return path supports meditation node");
            Check(!story.Continue(),"Rest cannot bypass healing through ordinary continue");
            story.LeaveMeditation(); Check(story.Current.kind=="duel" && story.Stats.Wounds==1,"Explicit departure preserves wound");
            var combat=new CombatModel(); combat.Reset(story.Stats.Wounds);
            Check(combat.StaminaLimit==90 && combat.Stamina==90 && combat.MovementScale<1,"Wound reduces combat stamina and speed");
            combat.BeginGuard(); combat.ReceiveStrike(combat.Stance,2,0);
            Check(combat.Stamina<=90,"Parry refund cannot exceed wounded maximum");
            combat.Tick(10,2,0); Check(combat.Stamina<=90,"Stamina stays capped while wounded");
            story.ResolveDuel(true,4); Check(story.Stats.Comebacks==1 && story.Stats.BounceBack==7,"Wounded victory records comeback plus parries");
            Continue(); story.Choose(1); Check(story.Current.id=="fame_end" && story.Stats.Fame==4,"Ruthless branch increases Fame");
            story.Reset(); Duel(); story.ResolveDuel(false,0); Continue(); story.Choose(1);
            Check(story.Current.id=="road_end" && story.Stats.Wounds==1,"Walking away ends chapter with wound retained");
            story.Reset(); Check(story.Stats.Wounds==0 && story.Stats.BounceBack==0 && story.Stats.Honor==0,"New journey resets session stats");
            Duel();
            for(int i=0;i<5;i++) { story.ResolveDuel(false,0); Continue(); story.Choose(0); story.LeaveMeditation(); }
            Check(story.Stats.Wounds==GameConfig.MaximumWounds,"Repeated losses clamp wounds");
            var input=new StoryChoiceInput(); story.Reset(); Continue(); input.Reset(3);
            var face=new FaceState { tracking=true,calibrated=true,gaze=Gaze.center,nod=true };
            Check(input.Read(story.Current,0,face,0,true)==-1,"Entry delay rejects stale gestures and accidental keys");
            face.nod=false; input.Read(story.Current,1,face,-1,false);
            face.leanTrigger=1; input.Read(story.Current,1,face,-1,false);
            face.leanTrigger=0; face.nod=true;
            Check(input.Read(story.Current,1,face,-1,false)==2,"Lean selects third option and survives held center gaze until nod");
            face.nod=false; face.gaze=Gaze.left; input.Read(story.Current,1,face,-1,false);
            Check(input.Selected==0 && input.Read(story.Current,1,face,-1,true)==0,"Gaze selects first option, Enter confirms");
            face.tracking=false; face.nod=true;
            Check(input.Read(story.Current,1,face,-1,false)==-1 && input.Read(story.Current,1,face,1,false)==1,"Lost tracking disables gestures but number fallback works");
            story.Choose(0); Continue(); story.ResolveDuel(true,0); Continue(); input.Reset(2);
            face.tracking=true; face.nod=true; face.shake=false;
            Check(input.Read(story.Current,1,face,-1,false)==0,"Binary nod selects first option");
            face.nod=false; face.shake=true; Check(input.Read(story.Current,1,face,-1,false)==1,"Binary shake selects second option");
            face.nod=true; Check(input.Read(story.Current,1,face,-1,false)==-1,"Ambiguous nod and shake cannot commit");
            bool rejected=false;
            try { new StoryModel(new StoryGraph { start="a",nodes=new[] {new StoryNode {id="a",kind="cutscene",next="missing"}} }); }
            catch(InvalidOperationException) { rejected=true; }
            Check(rejected,"Graph validation rejects broken links");
            Debug.Log($"GANRYUJIMA STORY CHECKS PASS: {assertions} assertions.");
        }
        private static void Check(bool condition,string message) { assertions++; if(!condition) throw new Exception("Story check failed: "+message); }
    }
}
