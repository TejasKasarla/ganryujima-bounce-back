using System;
using System.Collections.Generic;
using UnityEngine;
using Ganryujima.Core;
using Ganryujima.Combat;

namespace Ganryujima.Story
{
    [Serializable] public sealed class StoryEffect { public int honor, fame, wounds; }
    [Serializable] public sealed class StoryChoice { public string text, next; public StoryEffect effect; }
    [Serializable] public sealed class StoryNode
    {
        public string id, kind, title, speaker, text, shot, next, win, lose, opponent, arena, rule;
        public StoryChoice[] choices;
        public StoryEffect effect, winEffect, loseEffect;
    }
    [Serializable] public sealed class StoryGraph
    {
        public string start;
        public StoryNode[] nodes;
        public Dictionary<string,StoryNode> Validate()
        {
            var map=new Dictionary<string,StoryNode>();
            if(nodes==null || nodes.Length==0) throw new InvalidOperationException("story.json has no nodes.");
            foreach(var node in nodes)
            {
                if(node==null || string.IsNullOrWhiteSpace(node.id) || map.ContainsKey(node.id)) throw new InvalidOperationException("Story node IDs must be present and unique.");
                if(node.kind!="cutscene" && node.kind!="choice" && node.kind!="duel" && node.kind!="meditation" && node.kind!="ending") throw new InvalidOperationException("Unknown story kind: "+node.kind);
                map.Add(node.id,node);
            }
            void Require(string id) { if(string.IsNullOrEmpty(id) || !map.ContainsKey(id)) throw new InvalidOperationException("Story link missing: "+id); }
            Require(start);
            foreach(var node in nodes)
            {
                if(node.kind=="choice")
                {
                    if(node.choices==null || (node.choices.Length!=2 && node.choices.Length!=3)) throw new InvalidOperationException("Choices must have two or three options: "+node.id);
                    foreach(var choice in node.choices) { if(choice==null || string.IsNullOrWhiteSpace(choice.text)) throw new InvalidOperationException("Empty story choice."); Require(choice.next); }
                }
                else if(node.kind=="duel") { Require(node.win); Require(node.lose); }
                else if(node.kind!="ending") Require(node.next);
                if(node.rule=="final") { Require("ending_way"); Require("ending_undefeated"); Require("ending_ronin"); }
                if(!string.IsNullOrEmpty(node.opponent) && node.opponent!="arima" && node.opponent!="yoshioka" && node.opponent!="kojiro") throw new InvalidOperationException("Unknown opponent: "+node.opponent);
                if(!string.IsNullOrEmpty(node.arena) && node.arena!="court" && node.arena!="grove" && node.arena!="lantern" && node.arena!="beach" && node.arena!="beach_late") throw new InvalidOperationException("Unknown arena: "+node.arena);
            }
            return map;
        }
    }
    public sealed class JourneyStats
    {
        public int Honor { get; private set; }
        public int Fame { get; private set; }
        public int Wounds { get; private set; }
        public int Parries { get; internal set; }
        public int Comebacks { get; internal set; }
        public int Recoveries { get; internal set; }
        public int Wins { get; internal set; }
        public int Losses { get; internal set; }
        public bool FinalVictory { get; internal set; }
        public int BounceBack => Parries+Comebacks+Recoveries;
        internal void Apply(StoryEffect effect)
        {
            if(effect==null) return;
            Honor=Mathf.Clamp(Honor+effect.honor,0,100);
            Fame=Mathf.Clamp(Fame+effect.fame,0,100);
            int before=Wounds;
            Wounds=Mathf.Clamp(Wounds+effect.wounds,0,GameConfig.MaximumWounds);
            if(Wounds<before) Recoveries+=before-Wounds;
        }
    }
    public sealed class StoryModel
    {
        private readonly StoryGraph graph;
        private readonly Dictionary<string,StoryNode> nodes;
        public StoryNode Current { get; private set; }
        public JourneyStats Stats { get; private set; }
        public float Age { get; private set; }
        public int RevealedCharacters { get; private set; }
        public bool TextComplete => RevealedCharacters >= (Current.text??"").Length;
        public bool MeditationComplete { get; private set; }
        public OpponentId Encounter { get; private set; }
        public string Arena { get; private set; }
        public event Action<StoryNode> Changed;
        public StoryModel(StoryGraph graph) { this.graph=graph; nodes=graph.Validate(); Reset(); }
        public void Reset() { Stats=new JourneyStats(); Encounter=OpponentId.Arima; Arena="court"; Enter(graph.start); }
        private void Enter(string id)
        {
            if(nodes[id].rule=="final")
                id=Stats.FinalVictory && Stats.Honor>=GameConfig.EndingHonorThreshold && Stats.Honor>=Stats.Fame ? "ending_way" :
                    Stats.FinalVictory && Stats.Losses==0 ? "ending_undefeated" : "ending_ronin";
            Current=nodes[id]; Age=0; RevealedCharacters=0; MeditationComplete=false;
            if(!string.IsNullOrEmpty(Current.opponent)) Encounter=Current.opponent=="kojiro"?OpponentId.Kojiro:Current.opponent=="yoshioka"?OpponentId.Yoshioka:OpponentId.Arima;
            if(!string.IsNullOrEmpty(Current.arena)) Arena=Current.arena;
            Stats.Apply(Current.effect);
            Changed?.Invoke(Current);
        }
        public void Tick(float dt,bool paused)
        {
            if(paused || dt<=0) return;
            Age+=dt;
            RevealedCharacters=Mathf.Min((Current.text??"").Length,Mathf.FloorToInt(Age*GameConfig.StoryCharactersPerSecond));
        }
        public bool Continue()
        {
            if(Current.kind!="cutscene" && Current.kind!="meditation") return false;
            if(Current.kind=="meditation" && !MeditationComplete) return false;
            if(!TextComplete) { RevealedCharacters=(Current.text??"").Length; Age=Mathf.Max(Age,RevealedCharacters/GameConfig.StoryCharactersPerSecond); return false; }
            Enter(Current.next); return true;
        }
        public bool CompleteMeditation(MeditationModel activity)
        {
            if(Current.kind!="meditation" || MeditationComplete || activity==null || !activity.Completed) return false;
            MeditationComplete=true;
            Stats.Apply(new StoryEffect { wounds=-1 });
            RevealedCharacters=(Current.text??"").Length;
            Age=Mathf.Max(Age,RevealedCharacters/GameConfig.StoryCharactersPerSecond);
            return true;
        }
        public bool LeaveMeditation()
        {
            if(Current.kind!="meditation") return false;
            Enter(Current.next); return true; // Explicit departure grants no healing.
        }
        public bool Choose(int index)
        {
            if(Current.kind!="choice" || index<0 || index>=Current.choices.Length) return false;
            var choice=Current.choices[index]; Stats.Apply(choice.effect); Enter(choice.next); return true;
        }
        public bool ResolveDuel(bool won,int parries)
        {
            if(Current.kind!="duel") return false;
            Stats.Parries+=Mathf.Max(0,parries);
            if(won) { Stats.Wins++; if(Stats.Wounds>0) Stats.Comebacks++; }
            else Stats.Losses++;
            var node=Current;
            if(won && Encounter==OpponentId.Kojiro) Stats.FinalVictory=true;
            Stats.Apply(won?node.winEffect:node.loseEffect);
            Enter(won?node.win:node.lose);
            return true;
        }
    }
}
