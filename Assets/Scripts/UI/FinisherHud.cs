using UnityEngine;
using Ganryujima.Combat;
using Ganryujima.Core;

namespace Ganryujima.UI
{
    public static class FinisherHud
    {
        public static void Draw(FinishingBlow finisher)
        {
            GUI.matrix=Matrix4x4.identity;
            float w=Screen.width,h=Screen.height,bar=h*.115f;
            GUI.color=Color.black;
            GUI.DrawTexture(new Rect(0,0,w,bar),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0,h-bar,w,bar),Texture2D.whiteTexture);
            if (finisher.Flash>0)
            {
                GUI.color=new Color(1,.91f,.73f,finisher.Flash);
                GUI.DrawTexture(new Rect(0,bar,w,h-bar*2),Texture2D.whiteTexture);
            }
            GUI.color=new Color(.92f,.87f,.75f);
            var label=new GUIStyle(GUI.skin.label) { alignment=TextAnchor.MiddleCenter,fontSize=Mathf.Max(12,Mathf.RoundToInt(h*.02f)) };
            string caption=finisher.Paused ? "PAUSED  /  ESC TO RESUME" : finisher.Timeline.Elapsed>2.3f ? "A single opening. A decisive cut." : "THE FINAL EXCHANGE";
            GUI.Label(new Rect(0,h-bar,w,bar*.65f),caption,label);
            label.fontSize=Mathf.Max(10,Mathf.RoundToInt(h*.014f));
            GUI.Label(new Rect(0,h-bar*.4f,w,bar*.35f),"SPACE  SKIP     /     ESC  PAUSE",label);
        }
    }
}
