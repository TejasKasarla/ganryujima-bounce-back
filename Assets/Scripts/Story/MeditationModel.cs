using UnityEngine;
using Ganryujima.Core;
using Ganryujima.Input.Face;

namespace Ganryujima.Story
{
    public sealed class MeditationModel
    {
        public float CalmSeconds { get; private set; }
        public float Progress => Mathf.Clamp01(CalmSeconds/GameConfig.MeditationHoldSeconds);
        public float Composure { get; private set; }
        public float BrokenRemaining { get; private set; }
        public bool SignalReady { get; private set; }
        public bool Completed { get; private set; }
        public bool CompletedThisTick { get; private set; }
        public string Status => Completed ? "THE CIRCLE IS WHOLE" : !SignalReady ? "WAITING FOR A CALIBRATED FACE" :
            BrokenRemaining>0 ? "THE INK BREAKS. BEGIN AGAIN." : Composure>=GameConfig.MeditationCalmThreshold ? "LET THE CIRCLE GROW" : "SOFTEN YOUR EXPRESSION";
        public void Reset()
        {
            CalmSeconds=Composure=BrokenRemaining=0;
            SignalReady=Completed=CompletedThisTick=false;
        }
        public void Tick(float dt,FaceState face,bool paused)
        {
            CompletedThisTick=false;
            if(paused || dt<=0 || Completed) return;
            BrokenRemaining=Mathf.Max(0,BrokenRemaining-dt);
            SignalReady=face.tracking && face.calibrated && !float.IsNaN(face.composure) && !float.IsInfinity(face.composure);
            Composure=SignalReady?Mathf.Clamp01(face.composure):0;
            if(!SignalReady) { CalmSeconds=0; return; }
            if(Composure<=GameConfig.MeditationBreakThreshold)
            {
                if(CalmSeconds>0) BrokenRemaining=GameConfig.MeditationBreakFlash;
                CalmSeconds=0;
                return;
            }
            // A short recovery interval makes each broken stroke readable before a new one begins.
            if(BrokenRemaining>0 || Composure<GameConfig.MeditationCalmThreshold) return;
            CalmSeconds=Mathf.Min(GameConfig.MeditationHoldSeconds,CalmSeconds+dt);
            if(CalmSeconds>=GameConfig.MeditationHoldSeconds) Completed=CompletedThisTick=true;
        }
    }
}
