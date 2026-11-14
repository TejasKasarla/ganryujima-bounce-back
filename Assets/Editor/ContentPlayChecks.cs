using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Ganryujima.Core;
using Ganryujima.Combat;
using Ganryujima.World;

namespace Ganryujima.Editor
{
    public static class ContentPlayChecks
    {
        public static void Capture()
        {
            ContentSetup.Prepare();
            CombatChecks.Verify(); OpponentChecks.Verify(); FaceGameplayChecks.Verify(); FinisherChecks.Verify(); StoryChecks.Verify(); MeditationChecks.Verify(); ContentChecks.Verify();
            ProjectSetup.CreateScene(); SessionState.SetBool("GanryujimaContentCapture",true); EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if(!SessionState.GetBool("GanryujimaContentCapture",false)) return;
            double deadline=EditorApplication.timeSinceStartup+100;
            EditorApplication.update+=Check;
            async void Check()
            {
                var game=UnityEngine.Object.FindFirstObjectByType<GameLoop>();
                if(EditorApplication.isPlaying && game!=null && game.FramesTicked>=20)
                {
                    EditorApplication.update-=Check; SessionState.SetBool("GanryujimaContentCapture",false);
                    try { await VerifyScene(game); Debug.Log("CONTENT INTEGRATION PASS: three opponents, three Yoshioka venues, beach collider/normal map, Swallow Cut poses, final finisher, ending summary, full reset and live face ticks."); EditorApplication.Exit(0); }
                    catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
                }
                else if(EditorApplication.timeSinceStartup>deadline) { Debug.LogError("Content capture timeout"); EditorApplication.Exit(1); }
            }
        }
        private static async Task VerifyScene(GameLoop game)
        {
            string output=Environment.GetEnvironmentVariable("GANRYUJIMA_CAPTURE_DIR")??Path.GetFullPath("work/content-captures"); Directory.CreateDirectory(output);
            void Assert(bool value,string message) { if(!value) throw new Exception(message); }
            void Move() { ContentChecks.AdvanceScenes(game.Story); }
            void Venue(int index)
            {
                game.BeginJourney(); Move(); game.Story.Choose(0); Move(); game.Story.ResolveDuel(true,2); Move(); game.Story.Choose(0); Move(); game.Story.Choose(index); Move();
            }
            var camera=Camera.main;
            var art=(ArimaArt)typeof(GameLoop).GetField("opponentArt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(game);
            var arenas=(ArenaDirector)typeof(GameLoop).GetField("arenas",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(game);
            for(int venue=0;venue<3;venue++)
            {
                Venue(venue);
                Assert(game.Opponent.Id==OpponentId.Yoshioka && art.Root.name==game.Opponent.Name,"Yoshioka model/profile handoff failed");
                camera.transform.position=new Vector3(0,GameConfig.EyeHeight,-.6f); camera.transform.rotation=Quaternion.Euler(5,0,0);
                await Task.Delay(120); VisualCapture.Save(camera,Path.Combine(output,"yoshioka-"+arenas.Current+".png"));
            }
            game.Story.ResolveDuel(true,3); Move(); game.Story.Choose(0); Move(); game.Story.Choose(0); Move();
            Assert(game.Opponent.Id==OpponentId.Kojiro && game.Combat.TargetHealthLimit==GameConfig.KojiroHealth && arenas.Current=="beach","Final duel profile/arena handoff failed");
            var ground=GameObject.Find("Sand shore");
            Assert(ground!=null && ground.GetComponent<MeshCollider>()!=null && ground.GetComponent<Renderer>().sharedMaterial.GetTexture("_BumpMap")!=null,"Beach collision or downloaded normal texture missing");
            Assert(Physics.Raycast(new Vector3(0,5,0),Vector3.down,out var hit,10) && Mathf.Abs(hit.point.y)<.05f,"Beach court does not have flat walkable collision");
            foreach(var r in art.Root.GetComponentsInChildren<Renderer>()) Assert(r.sharedMaterial.shader.isSupported,"Character shader unsupported");
            camera.transform.position=new Vector3(0,GameConfig.EyeHeight,-.6f); camera.transform.rotation=Quaternion.Euler(5,0,0);
            int frames=game.FramesTicked;
            await Task.Delay(120); VisualCapture.Save(camera,Path.Combine(output,"kojiro-beach.png"));
            Assert(game.FramesTicked>frames,"Face loop stopped during content transition");
            // Keep the player out of range to inspect the entire signature without terminating combat.
            int budget=2000;
            while(!(game.Opponent.SwallowCut && game.Opponent.Telling) && --budget>0) game.Opponent.Tick(.02f,new Vector2(0,-2),0);
            Assert(budget>0,"Kojiro signature never started"); game.Opponent.Tick(.6f,new Vector2(0,-2),0); art.Tick(game.Opponent,1);
            await Task.Delay(120); VisualCapture.Save(camera,Path.Combine(output,"swallow-low.png"));
            budget=1000;
            while(game.Opponent.State!=OpponentState.SwallowTurn && --budget>0) game.Opponent.Tick(.02f,new Vector2(0,-2),0);
            Assert(budget>0,"Kojiro return phase never started");
            game.Opponent.Tick(.24f,new Vector2(0,-2),0); art.Tick(game.Opponent,.24f);
            await Task.Delay(120); VisualCapture.Save(camera,Path.Combine(output,"swallow-return.png"));
            // Reset into the final fight to verify the lethal cut uses the current opponent's rig and profile.
            game.Opponent.Reset(); game.Combat.Reset(0,game.Opponent.MaximumHealth); art.Tick(game.Opponent,1);
            for(int i=0;i<4;i++) { game.Combat.Tick(3,2,0); game.Combat.BeginCharge(); game.Combat.Tick(.56f,2,0); game.Combat.ReleaseStrike(); game.Combat.Tick(.9f,2,0); }
            Assert(game.Finisher.TryBegin(game.Combat),"Final finishing cinematic failed to trigger");
            game.Finisher.Tick(.7f,false); await Task.Delay(120); VisualCapture.Save(camera,Path.Combine(output,"kojiro-finisher.png"));
            if(game.Finisher.Paused) game.Finisher.TogglePause(); game.Finisher.Tick(0,true);
            game.Story.ResolveDuel(true,4); Move(); game.Story.Choose(0);
            Assert(game.Story.Current.id=="ending_way" && game.Story.Stats.Wins==3 && game.Story.Stats.FinalVictory,"Final victory did not produce expected summary");
            game.BeginJourney(); Assert(game.Opponent.Id==OpponentId.Arima && arenas.Current=="court" && game.Combat.TargetHealthLimit==100,"New journey failed to restore Arima/court");
        }
    }
}
