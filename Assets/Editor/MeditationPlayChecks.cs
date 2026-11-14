using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Ganryujima.Core;
using Ganryujima.Input.Face;
using Ganryujima.Story;
using Ganryujima.World;

namespace Ganryujima.Editor
{
    public static class MeditationPlayChecks
    {
        public static void Capture()
        {
            CombatChecks.Verify(); OpponentChecks.Verify(); FaceGameplayChecks.Verify(); FinisherChecks.Verify(); StoryChecks.Verify(); MeditationChecks.Verify();
            ProjectSetup.CreateScene(); SessionState.SetBool("GanryujimaMeditationCapture",true); EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if(!SessionState.GetBool("GanryujimaMeditationCapture",false)) return;
            double deadline=EditorApplication.timeSinceStartup+100;
            EditorApplication.update+=Check;
            async void Check()
            {
                var game=UnityEngine.Object.FindFirstObjectByType<GameLoop>();
                if(EditorApplication.isPlaying && game!=null && game.FramesTicked>=20)
                {
                    EditorApplication.update-=Check; SessionState.SetBool("GanryujimaMeditationCapture",false);
                    try { await VerifyScene(game); Debug.Log("MEDITATION INTEGRATION PASS: enso shader, break, completion, bell asset, wound healing, recovery tally, restored duel and face ticks."); EditorApplication.Exit(0); }
                    catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
                }
                else if(EditorApplication.timeSinceStartup>deadline) { Debug.LogError("Meditation capture timeout"); EditorApplication.Exit(1); }
            }
        }
        private static async Task VerifyScene(GameLoop game)
        {
            string output=Environment.GetEnvironmentVariable("GANRYUJIMA_CAPTURE_DIR")??Path.GetFullPath("work/meditation-captures");
            Directory.CreateDirectory(output);
            void Assert(bool value,string message) { if(!value) throw new Exception(message); }
            void Continue() { game.Story.Continue(); game.Story.Continue(); }
            T Field<T>(string name) => (T)typeof(GameLoop).GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(game);
            game.BeginJourney(); Continue(); game.Story.Choose(0); Continue(); game.Story.ResolveDuel(false,2); Continue(); game.Story.Choose(0);
            Assert(game.Story.Current.kind=="meditation" && game.Story.Stats.Wounds==1,"Meditation story handoff failed");
            var visual=Field<MeditationScene>("meditationScene"); var camera=Camera.main; var activity=game.Meditation;
            Assert(visual.Visible && GameObject.Find("Meditation enso").GetComponent<Renderer>().sharedMaterial.shader.isSupported,"Enso shader or canvas missing");
            Assert(Resources.Load<AudioClip>("Audio/Meditation/SingingBowl")?.length>1,"Completion bell failed to import");
            game.Story.Tick(2,false); Field<StoryCamera>("storyCamera").Tick(2);
            var calm=new FaceState {tracking=true,calibrated=true,composure=.95f};
            activity.Tick(2.5f,calm,false); visual.Tick(activity,false);
            int frames=game.FramesTicked;
            await Task.Delay(120); VisualCapture.Save(camera,Path.Combine(output,"enso-half.png"));
            Assert(game.FramesTicked>frames,"Face input stopped in meditation");
            var agitated=calm; agitated.composure=.15f; activity.Tick(.02f,agitated,false); visual.Tick(activity,false);
            await Task.Delay(120); VisualCapture.Save(camera,Path.Combine(output,"enso-broken.png"));
            activity.Tick(GameConfig.MeditationBreakFlash,calm,false); activity.Tick(GameConfig.MeditationHoldSeconds,calm,false);
            Assert(game.Story.CompleteMeditation(activity),"Completed circle did not heal");
            visual.Tick(activity,false); visual.Complete();
            await Task.Delay(120); VisualCapture.Save(camera,Path.Combine(output,"enso-complete.png"));
            Assert(game.Story.Stats.Wounds==0 && game.Story.Stats.Recoveries==1 && game.Story.Stats.BounceBack==3,"Healing stats are incorrect");
            game.Story.Continue();
            Assert(!visual.Visible && game.Story.Current.kind=="duel" && game.Combat.StaminaLimit==100 && game.Combat.MovementScale==1,"Healed duel did not restore stats or hide enso");
            Assert(camera.transform.localPosition==Vector3.up*GameConfig.EyeHeight,"Meditation did not restore first-person camera");
        }
    }
}
