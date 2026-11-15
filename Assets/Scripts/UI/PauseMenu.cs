using System;
using UnityEngine;
using Ganryujima.Core;

namespace Ganryujima.UI
{
    public sealed class PauseMenu
    {
        private int page;
        private Font serif;
        private readonly Color paper = new Color(.91f, .88f, .79f), gold = new Color(.74f, .63f, .40f);
        public void Open() { page = 0; }
        public void Draw(string chapter, bool canRestart, Action resume, Action restart, Action newJourney)
        {
            float scale = Mathf.Min(Screen.width / 1100f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale;
            Fill(new Rect(0, 0, w, h), new Color(.015f, .035f, .035f, .88f));
            float x = (w - 960) / 2, y = (h - 710) / 2;
            Fill(new Rect(x, y, 960, 710), new Color(.045f, .075f, .075f, .98f));
            Fill(new Rect(x + 40, y + 38, 34, 3), new Color(.62f, .19f, .13f));
            Label(new Rect(x + 40, y + 55, 830, 24), "G A N R Y U J I M A   /   " + chapter.ToUpperInvariant(), 12, gold);
            if (serif == null) serif = GameFonts.Serif();
            Label(new Rect(x + 37, y + 94, 850, 70), page == 0 ? "A breath between exchanges." : page == 1 ? "Find your balance." : "The way of the blade.", 37, paper, true);
            if (Button(new Rect(x + 40, y + 183, 255, 46), "RESUME   /   ESC")) resume();
            if (Button(new Rect(x + 40, y + 245, 255, 46), "SOUND & DISPLAY")) page = 1;
            if (Button(new Rect(x + 40, y + 307, 255, 46), "CONTROLS")) page = 2;
            if (canRestart && Button(new Rect(x + 40, y + 411, 255, 46), "RESTART THIS DUEL")) restart();
            if (Button(new Rect(x + 40, y + 473, 255, 46), "NEW JOURNEY")) newJourney();
            if (!Application.isEditor && Application.platform != RuntimePlatform.WebGLPlayer && Button(new Rect(x + 40, y + 535, 255, 46), "QUIT TO DESKTOP")) Application.Quit();
            float right = x + 345;
            if (page == 1)
            {
                bool changed = GUI.changed; GUI.changed = false;
                GameSettings.Volume = Slider(right, y + 181, "MASTER VOLUME", GameSettings.Volume, 0, 1, (GameSettings.Volume * 100).ToString("F0") + "%");
                GameSettings.Ambience = Slider(right, y + 245, "WIND & SEA", GameSettings.Ambience, 0, 1, (GameSettings.Ambience * 100).ToString("F0") + "%");
                GameSettings.Music = Slider(right, y + 309, "MUSIC & TAIKO", GameSettings.Music, 0, 1, (GameSettings.Music * 100).ToString("F0") + "%");
                GameSettings.Sensitivity = Slider(right, y + 373, "MOUSE SENSITIVITY", GameSettings.Sensitivity, .4f, 5, GameSettings.Sensitivity.ToString("F1"));
                Label(new Rect(right, y + 441, 175, 24), "QUALITY", 12, gold);
                GameSettings.Quality = GUI.SelectionGrid(new Rect(right, y + 472, 565, 35), GameSettings.Quality, new[] { "PERFORMANCE", "BALANCED", "HIGH" }, 3);
                GameSettings.Shake = Toggle(right, y + 530, "Camera shake", GameSettings.Shake);
                GameSettings.PostEffects = Toggle(right + 192, y + 530, "Ink / bloom", GameSettings.PostEffects);
                GameSettings.Particles = Toggle(right + 384, y + 530, "Petals / dust", GameSettings.Particles);
                if (GUI.changed) GameSettings.Save();
                GUI.changed |= changed;
                Label(new Rect(right, y + 588, 560, 64), "Settings save automatically. Performance reduces shadow distance and antialiasing. Your journey continues when you resume.", 14, paper);
            }
            else if (page == 2)
            {
                Label(new Rect(right, y + 181, 570, 435),
                    "WASD   /   Move and circle\nMouse   /   Look and select stance\nLMB   /   Strike; hold to charge a heavy cut\nRMB   /   Guard; press just before contact to parry\n1 / 2 / 3 / 4   /   Upper / middle / lower / side\nSpace   /   Retreat in combat; advance story; skip finisher\nQ / E   /   Keyboard dodge        F   /   Keyboard kiai\n1–3   /   Story choices; Y / N for binary choices\nEsc   /   Pause        R   /   Restart current duel\nF1   /   Camera preview        F2   /   Gameplay diagnostics", 17, paper);
                Label(new Rect(right, y + 590, 570, 74), "FACE INPUT: lean to dodge, open your mouth for kiai, stay relaxed for a wider parry window. During meditation, keep your head steady and mouth relaxed for five seconds. Hold C to test agitation in keyboard mode.", 14, gold);
            }
            else
            {
                Label(new Rect(right, y + 189, 550, 150), "Stillness is part of the fight.\n\nRead the wind-up. Match your guard to the incoming zone. Parry at contact, then strike while your opponent recoils.", 23, paper, true);
                Fill(new Rect(right, y + 382, 60, 2), gold);
                Label(new Rect(right, y + 420, 550, 150), "BOUNCE BACK\nEvery parry, wound healed through meditation, and victory while wounded contributes to your final tally. A defeat gives you another road forward.", 17, paper);
                Label(new Rect(right, y + 603, 550, 40), "The duel, story, cinematic and healing timers are paused.", 12, gold);
            }
            Label(new Rect(x + 40, y + 663, 870, 24), "B O U N C E   B A C K     /     E S C   T O   R E T U R N", 10, gold);
        }
        private float Slider(float x, float y, string name, float value, float min, float max, string text)
        {
            Label(new Rect(x, y, 440, 24), name, 12, gold);
            Label(new Rect(x + 500, y, 65, 24), text, 13, paper);
            return GUI.HorizontalSlider(new Rect(x, y + 31, 565, 20), value, min, max);
        }
        private bool Toggle(float x, float y, string text, bool value) => GUI.Toggle(new Rect(x, y, 183, 32), value, text);
        private bool Button(Rect r, string text)
        {
            Fill(r, r.Contains(Event.current.mousePosition) ? new Color(.48f, .20f, .14f) : new Color(.12f, .17f, .16f));
            var style = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(18, 10, 0, 0) };
            style.normal.textColor = paper;
            return GUI.Button(r, text, style);
        }
        private void Label(Rect r, string text, int size, Color color, bool literary = false)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, wordWrap = true, font = literary ? serif : GUI.skin.font };
            style.normal.textColor = color; GUI.Label(r, text, style);
        }
        private static void Fill(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
    }
}
