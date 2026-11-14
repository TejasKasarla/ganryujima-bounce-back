using System;
using UnityEngine;
using Ganryujima.Combat;
using Ganryujima.Core;
using Ganryujima.Input.Face;

namespace Ganryujima.UI
{
    public static class CombatHud
    {
        private static readonly Color Paper = new Color(0.96f, 0.93f, 0.83f);
        private static readonly Color Red = new Color(0.75f, 0.22f, 0.15f);
        private static readonly Color Gold = new Color(0.95f, 0.75f, 0.3f);

        public static void Draw(CombatModel combat, PracticeExchange practice, FaceState face, bool ready,
            bool captured, float distance, Action enter, Action restart, string inputError)
        {
            float scale = Mathf.Max(0.6f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            var heading = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            var text = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            var small = new GUIStyle(text) { fontSize = 12 };
            var centered = new GUIStyle(heading) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(30, 20, 650, 35), "GANRYUJIMA  /  BOUNCE BACK", heading);
            GUI.Label(new Rect(30, 55, 650, 25), "02 — THE FIRST EXCHANGE · COMBAT PRACTICE", small);
            if (combat == null) return;
            Bar(new Rect(30, 95, 280, 22), combat.Health / GameConfig.MaximumHealth, Red, $"HEALTH  {combat.Health:F0}", small);
            Bar(new Rect(30, 125, 280, 22), combat.Stamina / GameConfig.MaximumStamina, Gold, $"STAMINA  {combat.Stamina:F0}", small);
            Bar(new Rect(width - 310, 95, 280, 22), combat.TargetHealth / GameConfig.MaximumHealth, Red, $"PRACTICE POST  {combat.TargetHealth:F0}", small);
            GUI.Label(new Rect(width - 310, 125, 280, 30), $"PARRIES  {combat.Parries}     R  reset practice", small);
            string maai = distance > GameConfig.LightReach ? "OUT OF REACH" : distance < GameConfig.TooCloseDistance ? "CLOSE QUARTERS" : "STRIKING DISTANCE";
            GUI.Label(new Rect(width / 2 - 230, 90, 460, 40), $"{distance:F1} m  /  {maai}", new GUIStyle(text) { alignment = TextAnchor.MiddleCenter });

            if (captured)
            {
                GUI.Label(new Rect(width / 2 - 6, height / 2 - 16, 20, 30), combat.ParryOpen ? "◇" : "+", text);
                if (practice.Telling)
                {
                    GUI.color = Gold;
                    GUI.Label(new Rect(width / 2 - 260, height / 2 - 135, 520, 40), $"INCOMING / {practice.Zone.ToString().ToUpperInvariant()}", centered);
                    GUI.color = Color.white;
                    Bar(new Rect(width / 2 - 160, height / 2 - 90, 320, 10), practice.TellProgress, Gold, "", small);
                }
                else if (combat.RecoilRemaining > 0)
                    GUI.Label(new Rect(width / 2 - 260, height / 2 - 125, 520, 40), "PARRY / RECOIL", centered);
                if (combat.MessageRemaining > 0)
                    GUI.Label(new Rect(width / 2 - 330, height / 2 + 115, 660, 55), combat.Message, new GUIStyle(text) { alignment = TextAnchor.MiddleCenter });
            }

            string action = combat.BrokenRemaining > 0 ? "GUARD BROKEN" : combat.Charging ? "CHARGING" : combat.Swinging ? (combat.Heavy ? "HEAVY STRIKE" : "STRIKE") : combat.ParryOpen ? "PARRY WINDOW" : combat.Guarding ? "GUARD" : "READY";
            GUI.Label(new Rect(width - 360, height - 185, 330, 35), $"{combat.Stance.ToString().ToUpperInvariant()}  /  {action}", heading);
            GUI.Label(new Rect(width - 360, height - 148, 330, 95), "Mouse flick: ↑ upper · ↓ lower\n← middle · → side  /  keys 1–4\nLMB release: strike · hold: heavy\nRMB: guard / parry · Space: retreat", small);
            if (combat.Charging) Bar(new Rect(width - 360, height - 210, 320, 15), combat.ChargeProgress, Gold, combat.ChargeProgress >= 1 ? "HEAVY READY" : "CHARGE", small);
            GUI.Box(new Rect(25, height - 140, 510, 90), GUIContent.none);
            GUI.Label(new Rect(40, height - 133, 490, 80),
                $"FACE MOCK / {(face.tracking ? "TRACKING" : "OFFLINE")} / {(face.calibrated ? "CALIBRATED" : "UNCALIBRATED")}\n" +
                $"Composure {face.composure:F2} · Lean {face.leanX:F1} · Gaze {face.gaze}\n" +
                $"Lean pulse {face.leanTrigger} · Kiai {face.kiai} · Nod {face.nod} · Shake {face.shake} · Wink {face.wink}", small);
            GUI.Label(new Rect(30, height - 35, width - 60, 30), "WASD footwork · Esc pause/release · R restart     |     Fixed practice rhythm; Arima AI comes next.", small);
            if (captured) return;
            float left = (width - 590) / 2;
            Fill(new Rect(left - 30, 185, 650, 450), new Color(0.1f, 0.13f, 0.12f, 0.97f));
            GUI.color = Paper;
            string title = combat.Over ? (combat.Health <= 0 ? "Find your footing again." : "The first exchange is yours.") : "Find the opening.";
            GUI.Label(new Rect(left, 210, 590, 70), title, new GUIStyle(heading) { fontSize = 31, wordWrap = true });
            GUI.Label(new Rect(left, 292, 580, 90), combat.Over ? $"Practice complete · {combat.Parries} parries\nReset to restore both health bars and stamina." :
                "Approach the post. It strikes on a fixed rhythm.\nMatch the incoming zone; press RMB just before impact to parry. Hold RMB for a stamina-costing block.", text);
            if (GUI.Button(new Rect(left, 398, 580, 48), combat.Over ? "Reset and practice again" : ready ? "Enter the practice ground" : "Starting input…") && ready)
            { if (combat.Over) restart(); else enter(); }
            GUI.Label(new Rect(left, 467, 580, 115), "LMB: tap/release for light · hold 0.55s/release for heavy\nRMB: guard/parry · 1/2/3/4: upper/middle/lower/side\nWASD: footwork · Space: retreat (no i-frames) · R: reset\nFace controls remain readout-only until milestone 4.", small);
            if (!string.IsNullOrEmpty(inputError)) GUI.Label(new Rect(left, 585, 580, 40), inputError, small);
            GUI.color = Color.white;
        }

        private static void Bar(Rect rect, float fraction, Color color, string label, GUIStyle text)
        {
            Fill(rect, new Color(0.1f, 0.13f, 0.12f, 0.9f));
            Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), color);
            GUI.Label(new Rect(rect.x + 7, rect.y, rect.width - 14, 25), label, text);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
