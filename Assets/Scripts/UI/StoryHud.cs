using System;
using UnityEngine;
using Ganryujima.Story;
using Ganryujima.Core;

namespace Ganryujima.UI
{
    public static class StoryHud
    {
        private static readonly Color Paper=new Color(.94f,.9f,.79f),Gold=new Color(.78f,.65f,.41f);
        public static void Draw(StoryModel story,StoryChoiceInput input,bool paused,Action advance,Action<int> choose,Action newJourney)
        {
            float scale=Mathf.Max(.4f,Mathf.Min(Screen.width/1280f,Screen.height/720f));
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float w=Screen.width/scale,h=Screen.height/scale;
            var node=story.Current; var stats=story.Stats;
            Fill(new Rect(0,0,w,86),new Color(.025f,.04f,.04f,.96f));
            Fill(new Rect(0,h-250,w,250),new Color(.025f,.04f,.04f,.95f));
            Label(new Rect(42,18,w-440,40),node.title,26,Paper);
            Label(new Rect(42,58,w-120,22),$"HONOR {stats.Honor}    FAME {stats.Fame}    WOUNDS {stats.Wounds}    BOUNCE BACK {stats.BounceBack}",12,Gold);
            if(paused)
            {
                Fill(new Rect(0,86,w,h-336),new Color(0,0,0,.5f));
                Label(new Rect(42,h-210,w-84,50),"PAUSED — press Esc to continue",24,Paper); return;
            }
            float x=42,y=h-231,width=w-84;
            if(node.kind=="choice")
            {
                Label(new Rect(x,y,width,50),node.text,18,Paper);
                int count=node.choices.Length;
                for(int i=0;i<count;i++)
                {
                    float cardWidth=(width-(count-1)*14)/count;
                    var rect=new Rect(x+i*(cardWidth+14),y+65,cardWidth,75);
                    Fill(rect,input.Selected==i?new Color(.43f,.18f,.12f):new Color(.12f,.16f,.15f));
                    if(GUI.Button(rect,$"{i+1}  {node.choices[i].text}",ButtonStyle()) && story.Age>=GameConfig.StoryChoiceDelay) { choose(i); return; }
                }
                Label(new Rect(x,y+158,width,45),count==2?"Nod / Y = first    ·    Shake / N = second    ·    1–2 or click to choose":"Lean / gaze left, center, right to select    ·    Nod / Enter confirms    ·    1–3 or click to choose",13,Gold);
            }
            else if(node.kind=="ending")
            {
                Label(new Rect(x,y,width,70),node.text,19,Paper);
                Label(new Rect(x,y+78,width,42),$"{stats.Wins} wins  ·  {stats.Losses} losses  ·  {stats.Parries} parries  ·  {stats.Comebacks} comebacks  ·  {stats.Recoveries} recoveries",15,Gold);
                Fill(new Rect(x,y+135,300,48),new Color(.48f,.19f,.12f));
                if(GUI.Button(new Rect(x,y+135,300,48),"BEGIN A NEW JOURNEY",ButtonStyle())) newJourney();
                Label(new Rect(x+325,y+139,width-325,50),"A legend-inspired story, not a historical reconstruction.",12,Gold);
            }
            else
            {
                Label(new Rect(x,y,width,24),node.speaker??"THE ROAD",12,Gold);
                string visible=(node.text??"").Substring(0,story.RevealedCharacters);
                Label(new Rect(x,y+34,width,110),visible,22,Paper);
                if(GUI.Button(new Rect(x,y+158,width,36),story.TextComplete?"SPACE / CLICK TO CONTINUE":"SPACE / CLICK TO REVEAL",ButtonStyle())) advance();
            }
            Label(new Rect(w-350,22,310,30),"ESC  PAUSE    /    F1  CAMERA",12,Gold);
        }
        private static GUIStyle ButtonStyle()
        {
            var style=new GUIStyle(GUI.skin.label) { alignment=TextAnchor.MiddleCenter,wordWrap=true,fontSize=17,padding=new RectOffset(12,12,8,8) };
            style.normal.textColor=Paper; return style;
        }
        private static void Label(Rect rect,string text,int size,Color color)
        {
            var style=new GUIStyle(GUI.skin.label) { fontSize=size,wordWrap=true }; style.normal.textColor=color; GUI.Label(rect,text,style);
        }
        private static void Fill(Rect rect,Color color) { GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=Color.white; }
    }
}
