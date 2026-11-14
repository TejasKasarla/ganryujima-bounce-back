using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Ganryujima.Core;
using Ganryujima.Combat;

namespace Ganryujima.Editor
{
    public static class FinisherChecks
    {
        private static int checks;
        [MenuItem("Ganryujima/Check finisher rules")]
        public static void Verify()
        {
            checks=0;
            var c=new CombatModel(); var timeline=new FinisherTimeline();
            Check(!timeline.Begin(c),"Ordinary hit cannot start a finisher");
            for(int i=0;i<6;i++) c.ReceiveStrike(Stance.Middle,2,0);
            Check(!timeline.Begin(c),"Player defeat never triggers victory finisher");
            c.Reset(); Win(c);
            Check(timeline.Begin(c) && !timeline.Begin(c),"Lethal victory starts once");
            timeline.Tick(.7f,false,false); float elapsed=timeline.Elapsed;
            timeline.Tick(10,true,true); Check(timeline.Elapsed==elapsed && timeline.Active,"Pause freezes timeline and ignores skip");
            timeline.Tick(.6f,false,false); Check(timeline.ImpactThisTick,"Crossing impact plays cue once");
            timeline.Tick(.1f,false,false); Check(!timeline.ImpactThisTick,"Impact cannot repeat");
            timeline.Tick(10,false,false); Check(timeline.Complete && timeline.Elapsed==GameConfig.FinisherDuration,"Natural completion reaches result");
            timeline.Reset(); Check(timeline.Begin(c),"Retry can trigger another finisher");
            timeline.Tick(0,false,true); Check(timeline.Complete && !timeline.ImpactThisTick,"Skip completes without delayed impact");
            Check(c.TargetHealth==0 && c.Health==100,"Finisher never reapplies combat damage");
            Debug.Log($"GANRYUJIMA FINISHER CHECKS PASS: {checks} assertions.");
        }
        private static void Win(CombatModel c)
        {
            for(int i=0;i<3;i++) { c.BeginCharge(); c.Tick(.56f,2,0); c.ReleaseStrike(); c.Tick(.9f,2,0); }
        }
        public static void Capture()
        {
            CombatChecks.Verify(); OpponentChecks.Verify(); FaceGameplayChecks.Verify(); Verify();
            ProjectSetup.CreateScene(); SessionState.SetBool("GanryujimaFinisherCapture",true);
            EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if(!SessionState.GetBool("GanryujimaFinisherCapture",false)) return;
            double deadline=EditorApplication.timeSinceStartup+100;
            EditorApplication.update+=CheckReady;
            async void CheckReady()
            {
                var game=UnityEngine.Object.FindFirstObjectByType<GameLoop>();
                if(EditorApplication.isPlaying && game!=null && game.FramesTicked>=20)
                {
                    EditorApplication.update-=CheckReady; SessionState.SetBool("GanryujimaFinisherCapture",false);
                    try { await CaptureFrames(game); Debug.Log("FINISHER INTEGRATION PASS: lethal hit, poses, impact, audio asset, restore, skip, retry, face ticks."); EditorApplication.Exit(0); }
                    catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
                }
                else if(EditorApplication.timeSinceStartup>deadline) { Debug.LogError("Finisher capture timeout"); EditorApplication.Exit(1); }
            }
        }
        private static async Task CaptureFrames(GameLoop game)
        {
            string output=Environment.GetEnvironmentVariable("GANRYUJIMA_CAPTURE_DIR")??Path.GetFullPath("work/finisher-captures");
            Directory.CreateDirectory(output);
            Camera camera=Camera.main;
            camera.transform.position=new Vector3(0,GameConfig.EyeHeight,-.5f);
            camera.transform.rotation=Quaternion.identity;
            Vector3 original=camera.transform.position; float fov=camera.fieldOfView;
            game.Combat.Reset(); game.Opponent.Reset(); Win(game.Combat);
            var finisher=game.Finisher;
            Check(finisher.TryBegin(game.Combat),"Real scene begins on lethal cut");
            int frames=game.FramesTicked;
            var clip=Resources.Load<AudioClip>("Audio/Finisher/SwordSlash");
            Check(clip!=null && clip.length>.1f,"Downloaded sword sound imports as playable audio");
            float[] samples={.7f,1.19f,2.4f,3.65f};
            foreach(float sample in samples)
            {
                if(finisher.Paused) finisher.TogglePause();
                while(finisher.Timeline.Elapsed<sample-.0001f)
                    finisher.Tick(Mathf.Min(.02f,sample-finisher.Timeline.Elapsed),false);
                await Task.Delay(120);
                VisualCapture.Save(camera,Path.Combine(output,$"finisher-{sample:F2}.png"));
            }
            if(finisher.Paused) finisher.TogglePause(); finisher.Tick(.3f,false);
            Check(!finisher.Active && Vector3.Distance(camera.transform.position,original)<.001f && camera.fieldOfView==fov,"Completion restores gameplay camera");
            Check(game.FramesTicked>frames,"Face update keeps ticking during cinematic");
            finisher.Reset(); game.Combat.Reset(); game.Opponent.Reset(); Win(game.Combat);
            Check(finisher.TryBegin(game.Combat),"Retry starts a fresh cinematic");
            finisher.Tick(0,true);
            Check(!finisher.Active && Vector3.Distance(camera.transform.position,original)<.001f,"Skip restores camera and reaches result");
        }
        private static void Check(bool condition,string message) { checks++; if(!condition) throw new Exception("Finisher check failed: "+message); }
    }
}
