using Ganryujima.Core;
using Ganryujima.Input.Face;

namespace Ganryujima.Story
{
    /// <summary>Only consumes shared face snapshots. Entry delay discards gestures from the preceding scene.</summary>
    public sealed class StoryChoiceInput
    {
        public int Selected { get; private set; }
        private Gaze lastGaze=Gaze.unknown;
        public void Reset(int count) { Selected=count==3?1:0; lastGaze=Gaze.unknown; }
        public int Read(StoryNode node,float age,FaceState face,int number,bool confirm)
        {
            if(node.kind!="choice" || age<GameConfig.StoryChoiceDelay) return -1;
            int count=node.choices.Length;
            if(number>=0 && number<count) return number;
            if(face.tracking && face.calibrated)
            {
                if(count==2) { if(face.nod && !face.shake) return 0; if(face.shake && !face.nod) return 1; }
                else
                {
                    if(face.gaze!=lastGaze)
                    {
                        if(face.gaze==Gaze.left) Selected=0;
                        else if(face.gaze==Gaze.right) Selected=2;
                        else if(face.gaze==Gaze.center) Selected=1;
                        lastGaze=face.gaze;
                    }
                    if(face.leanTrigger<0) Selected=0;
                    else if(face.leanTrigger>0) Selected=2;
                    if(face.nod && !face.shake) return Selected;
                }
            }
            return confirm?Selected:-1;
        }
    }
}
