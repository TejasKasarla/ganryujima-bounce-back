using System;
using UnityEngine;
using Ganryujima.Combat;
using Ganryujima.Core;
using Ganryujima.Input.Face;

namespace Ganryujima.UI
{
    /// <summary>Quiet paper-and-ink interface. Developer readouts are opt-in with F2.</summary>
    public static class CombatHud
    {
        private static readonly Color Paper = new Color(0.93f, 0.89f, 0.78f);
        private static readonly Color Ink = new Color(0.10f, 0.14f, 0.14f);
        private static readonly Color Vermilion = new Color(0.61f, 0.23f, 0.15f);
        private static readonly Color Gold = new Color(0.76f, 0.66f, 0.43f);
        private static Font serif;

        public static void Draw(CombatModel combat, ArimaOpponent opponent, FaceState face, bool ready,
            bool captured, float distance, Action enter, Action restart, string inputError, bool debug = false)
        {
            float scale = Mathf.Max(0.6f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            if (serif == null) serif = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman" }, 24);
            GUIStyle tiny = Style(11, Paper), body = Style(14, Paper), title = Style(32, Paper, true);
            GUIStyle centered = Style(15, Paper); centered.alignment = TextAnchor.MiddleCenter;
            if (combat == null) return;
            if (!captured)
            {
                // Leave the right two thirds of the court visible through the entry composition.
                Fill(new Rect(0, 0, 650, height), new Color(0.075f, 0.105f, 0.10f, 0.94f));
                Fill(new Rect(65, 74, 34, 34), Vermilion);
                GUI.Label(new Rect(74, 75, 28, 30), "一", Style(24, Paper, true));
                GUI.Label(new Rect(115, 81, 460, 25), "G A N R Y U J I M A", body);
                GUI.Label(new Rect(65, 146, 520, 28), "B O U N C E   B A C K", Style(11, Gold));
                string heading = combat.Over ? (combat.Health <= 0 ? "Rise.\nBegin again." : "An opening.\nA way forward.") : "Steel. Silence.\nA second chance.";
                GUI.Label(new Rect(59, 215, 550, 190), heading, Style(58, Paper, true));
                Fill(new Rect(65, 419, 62, 2), Vermilion);
                string copy = combat.Over ? $"This exchange is over.\n{combat.Parries} parries. Another chance to find your footing." :
                    "On this shore, every exchange begins with stillness.\nFace Arima Kihei. Read his blade. Earn your opening.";
                GUI.Label(new Rect(65, 448, 510, 82), copy, Style(17, Paper, true));
                if (Button(new Rect(65, 566, 500, 52), combat.Over ? "DUEL AGAIN   →" : ready ? "ENTER THE COURT   →" : "PREPARING THE COURT…") && ready)
                { if (combat.Over) restart(); else enter(); }
                GUI.Label(new Rect(65, 654, 500, 105), "WASD  move     ·     Mouse  look / stance\nLMB  strike     ·     Hold LMB  heavy     ·     RMB  guard\n1–4  stance     ·     Space  retreat     ·     Esc  pause\nR  restart     ·     F2  field notes     ·     F1  camera diagnostics", tiny);
                GUI.Label(new Rect(65, height - 65, 500, 30), "T H E   F I R S T   E X C H A N G E   /   A R I M A", Style(10, Gold));
                if (!string.IsNullOrEmpty(inputError)) GUI.Label(new Rect(65, 780, 500, 60), inputError, body);
            }
            else
            {
                // Small identity and a thin target meter; the environment owns the screen.
                GUI.Label(new Rect(38, 28, 370, 40), "Ganryujima", Style(26, Ink, true));
                GUI.Label(new Rect(40, 68, 370, 22), "T H E   F I R S T   E X C H A N G E", Style(9, Ink));
                GUI.Label(new Rect(width / 2 - 200, 28, 400, 30), "A R I M A   K I H E I", new GUIStyle(Style(11, Ink)) { alignment = TextAnchor.MiddleCenter });
                Bar(new Rect(width / 2 - 160, 63, 320, 3), combat.TargetHealth / GameConfig.MaximumHealth, Vermilion);
                GUI.Label(new Rect(width - 230, 35, 185, 28), $"PARRIES   {combat.Parries:00}", new GUIStyle(Style(11, Ink)) { alignment = TextAnchor.MiddleRight });
                Fill(new Rect(25, height - 143, 320, 111), new Color(0.06f, 0.10f, 0.10f, 0.79f));
                GUI.Label(new Rect(42, height - 132, 270, 25), "VITALITY", tiny);
                Bar(new Rect(42, height - 104, 275, 4), combat.Health / GameConfig.MaximumHealth, Vermilion);
                GUI.Label(new Rect(42, height - 86, 270, 24), "BREATH", tiny);
                Bar(new Rect(42, height - 58, 275, 3), combat.Stamina / GameConfig.MaximumStamina, Gold);
                string action = combat.BrokenRemaining > 0 ? "BROKEN GUARD" : combat.Charging ? (combat.ChargeProgress >= 1 ? "HEAVY READY" : "DRAWING STRENGTH") : combat.ParryOpen ? "PARRY" : combat.Guarding ? "GUARD" : combat.Swinging ? "CUT" : "READY";
                GUI.Label(new Rect(width / 2 - 230, height - 99, 460, 35), combat.Stance.ToString().ToUpperInvariant() + "  /  " + action, centered);
                string spacing = distance > GameConfig.LightReach ? "BEYOND REACH" : distance < GameConfig.TooCloseDistance ? "CLOSE QUARTERS" : "WITHIN REACH";
                GUI.Label(new Rect(width / 2 - 230, height - 64, 460, 30), $"{distance:F1} m   ·   {spacing}", new GUIStyle(tiny) { alignment = TextAnchor.MiddleCenter });
                if (combat.Charging) Bar(new Rect(width / 2 - 110, height - 114, 220, 3), combat.ChargeProgress, Gold);
                GUI.Label(new Rect(width - 310, height - 66, 270, 30), "F2  FIELD NOTES    /    ESC  PAUSE", new GUIStyle(tiny) { alignment = TextAnchor.MiddleRight });
                Fill(new Rect(width / 2 - 1, height / 2 - 1, 3, 3), combat.ParryOpen ? Gold : Paper);
                if (opponent.Telling)
                {
                    Fill(new Rect(width / 2 - 190, height / 2 - 119, 380, 57), new Color(0.05f, 0.09f, 0.09f, 0.8f));
                    GUI.Label(new Rect(width / 2 - 180, height / 2 - 114, 360, 35), "INCOMING  /  " + opponent.Zone.ToString().ToUpperInvariant(), centered);
                    Bar(new Rect(width / 2 - 160, height / 2 - 70, 320, 2), opponent.TellProgress, Gold);
                }
                string tell = opponent.CounterOpen ? $"COUNTER NOW  /  {opponent.CounterRemaining:F1}s" :
                    opponent.State == OpponentState.Feint ? "WITHDRAWAL — watch for a new zone" :
                    opponent.State == OpponentState.Guard ? "ARIMA GUARDS  /  " + opponent.Zone.ToString().ToUpperInvariant() : "";
                if (tell.Length > 0) GUI.Label(new Rect(width / 2 - 300, height / 2 - 112, 600, 35), tell, centered);
                if (combat.MessageRemaining > 0)
                    GUI.Label(new Rect(width / 2 - 350, height / 2 + 110, 700, 50), combat.Message, centered);
            }
            if (debug)
            {
                Fill(new Rect(width - 480, 110, 445, 235), new Color(0.06f, 0.10f, 0.10f, 0.95f));
                GUI.Label(new Rect(width - 460, 125, 410, 205),
                    $"FIELD NOTES / ARIMA: {opponent.State}\nTracking {face.tracking} · Calibrated {face.calibrated}\nComposure {face.composure:F2} · Lean {face.leanX:F1} · Gaze {face.gaze}\nLean event {face.leanTrigger} · Kiai {face.kiai} · Nod {face.nod}\nShake {face.shake} · Wink {face.wink}\n\nStance: ↑ upper / ↓ lower / ← middle / → side\nFallback: 1 upper / 2 middle / 3 lower / 4 side\nQ/E lean · F kiai · Y/N nod/shake · C composure\nZ/X/V gaze · G wink (readout only)", tiny);
            }
            GUI.color = Color.white;
        }

        private static GUIStyle Style(int size, Color color, bool literary = false)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, wordWrap = true, font = literary ? serif : GUI.skin.font };
            style.normal.textColor = color;
            return style;
        }
        private static bool Button(Rect rect, string text)
        {
            Fill(rect, rect.Contains(Event.current.mousePosition) ? new Color(0.72f, 0.29f, 0.19f) : Vermilion);
            var style = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(22, 16, 0, 0) };
            style.normal.textColor = Paper;
            return GUI.Button(rect, text, style);
        }
        private static void Bar(Rect rect, float fraction, Color color)
        {
            Fill(rect, new Color(0.5f, 0.5f, 0.45f, 0.4f));
            Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), color);
        }
        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }
    }
}
