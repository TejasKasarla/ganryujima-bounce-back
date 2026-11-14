using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Ganryujima.Core;
using Ganryujima.Combat;

namespace Ganryujima.Editor
{
    public static class StoryPlayChecks
    {
        public static void Capture()
        {
            CombatChecks.Verify(); OpponentChecks.Verify(); FaceGameplayChecks.Verify(); FinisherChecks.Verify(); StoryChecks.Verify();
            ProjectSetup.CreateScene(); SessionState.SetBool("GanryujimaStoryCapture",true); EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if(!SessionState.GetBool("GanryujimaStoryCapture",false)) return;
            double deadline=EditorApplication.timeSinceStartup+100;
            EditorApplication.update+=Check;
            async void Check()
            {
                var game=UnityEngine.Object.FindFirstObjectByType<GameLoop>();
                if(EditorApplication.isPlaying && game!=null && game.FramesTicked>=20)
                {
                    EditorApplication.update-=Check; SessionState.SetBool("GanryujimaStoryCapture",false);
                    try { await VerifyScene(game); Debug.Log("STORY INTEGRATION PASS: intro camera, typewriter, choice-to-duel, loss continuation, wound retry, finisher aftermath, summary and new journey."); EditorApplication.Exit(0); }
                    catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
                }
                else if(EditorApplication.timeSinceStartup>deadline) { Debug.LogError("Story capture timeout"); EditorApplication.Exit(1); }
            }
        }
        private static async Task VerifyScene(GameLoop game)
        {
            string output=Environment.GetEnvironmentVariable("GANRYUJIMA_CAPTURE_DIR")??Path.GetFullPath("work/story-captures");
            Directory.CreateDirectory(output);
            void Assert(bool condition,string message) { if(!condition) throw new Exception(message); }
            void Continue() { game.Story.Continue(); game.Story.Continue(); }
            var camera=Camera.main;
            var cine=(Ganryujima.Story.StoryCamera)typeof(GameLoop).GetField("storyCamera",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(game);
            game.BeginJourney(); Assert(game.JourneyStarted && game.Story.Current.id=="arrival","Journey did not begin");
            game.Story.Tick(1.5f,false); cine.Tick(1.5f);
            int frames=game.FramesTicked; float health=game.Combat.Health;
            await Task.Delay(150);
            Assert(game.FramesTicked>frames && game.Combat.Health==health,"Narrative must keep face ticks and freeze combat");
            VisualCapture.Save(camera,Path.Combine(output,"intro.png"));
            Continue(); game.Story.Choose(2); Continue();
            Assert(game.Story.Current.kind=="duel" && game.Combat.Health==100 && game.Combat.Wounds==0,"Duel handoff did not reset combat");
            Assert(camera.transform.localPosition==Vector3.up*GameConfig.EyeHeight && camera.fieldOfView==GameConfig.FieldOfView,"Narrative did not restore first-person camera");
            game.Story.ResolveDuel(false,2); Continue(); game.Story.Choose(0); Continue();
            Assert(game.Story.Current.kind=="duel" && game.Combat.Wounds==1 && game.Combat.StaminaLimit==90,"Wounded story retry did not configure actual combat model");
            for(int i=0;i<3;i++) { game.Combat.BeginCharge(); game.Combat.Tick(.56f,2,0); game.Combat.ReleaseStrike(); game.Combat.Tick(.9f,2,0); }
            // The wounded maximum can require breathing before the final heavy.
            if(!game.Combat.Over) { game.Combat.Tick(3,2,0); game.Combat.BeginCharge(); game.Combat.Tick(.56f,2,0); game.Combat.ReleaseStrike(); game.Combat.Tick(.9f,2,0); }
            Assert(game.Finisher.TryBegin(game.Combat),"Wounded win did not allow finisher");
            if(game.Finisher.Paused) game.Finisher.TogglePause(); game.Finisher.Tick(0,true);
            // Drive the result explicitly: batch mode lacks the focused cursor capture used by live gameplay.
            if(game.Story.Current.kind=="duel") game.Story.ResolveDuel(true,game.Combat.Parries);
            Continue(); game.Story.Choose(0);
            Assert(game.Story.Current.kind=="ending" && game.Story.Stats.Comebacks==1,"Wounded victory did not reach chapter summary");
            cine.Tick(2); await Task.Delay(120);
            VisualCapture.Save(camera,Path.Combine(output,"chapter-end.png"));
            game.BeginJourney(); Assert(game.Story.Stats.Wounds==0 && game.Story.Stats.Fame==0 && game.Story.Current.id=="arrival","New journey did not clear prior stats");
        }
    }
}
