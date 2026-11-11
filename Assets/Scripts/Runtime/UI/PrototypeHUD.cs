using UnityEngine;
using VertigoShift.Gameplay.Gravity;
using VertigoShift.Gameplay.Panic;
using VertigoShift.Gameplay.Player;
using VertigoShift.Gameplay.Weapons;

namespace VertigoShift.UI
{
    /// <summary>
    /// Debug HUD (IMGUI - no prefab wiring needed).
    /// Shows ammo, reload progress, active gravity side, the dizziness meter and the controls.
    /// </summary>
    public class PrototypeHUD : MonoBehaviour
    {
        public Revolver revolver;
        public GravityShift gravity;
        public DizzinessSystem dizziness;
        public PlayerMotor motor;

        GUIStyle title;
        GUIStyle body;
        GUIStyle big;

        void EnsureStyles()
        {
            if (title != null) return;

            title = new GUIStyle(GUI.skin.label);
            title.fontSize = 17;
            title.fontStyle = FontStyle.Bold;
            title.normal.textColor = new Color(0.75f, 0.95f, 1f);

            body = new GUIStyle(title);
            body.fontSize = 13;
            body.fontStyle = FontStyle.Normal;
            body.normal.textColor = new Color(0.68f, 0.78f, 0.88f);

            big = new GUIStyle(title);
            big.fontSize = 32;
        }

        void OnGUI()
        {
            EnsureStyles();

            DrawCrosshair();
            DrawGravity();
            DrawDizziness();
            DrawAmmo();
            DrawHelp();
            DrawStats();
        }

        void DrawCrosshair()
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            GUI.color = new Color(0.8f, 0.95f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(center.x - 7f, center.y - 1f, 14f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(center.x - 1f, center.y - 7f, 2f, 14f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void DrawGravity()
        {
            if (gravity == null) return;
            string side = gravity.CurrentSide.ToString().ToUpperInvariant();
            int streak = gravity.ShiftStreak;

            GUI.Label(new Rect(18f, 16f, 520f, 26f), "GRAVITY  " + side + (streak > 1 ? "   SPAM x" + streak : ""), title);

            Color c = gravity.CurrentSide == GravitySide.Floor ? new Color(0.5f, 0.9f, 1f)
                    : gravity.CurrentSide == GravitySide.Ceiling ? new Color(1f, 0.85f, 0.4f)
                    : new Color(0.85f, 0.6f, 1f);
            GUI.color = c;
            GUI.Label(new Rect(18f, 40f, 520f, 20f), "1 Floor / 2 Left / 3 Ceiling / 4 Right  -  Q / E rotate", body);
            GUI.color = Color.white;
        }

        void DrawDizziness()
        {
            if (dizziness == null) return;
            float n = dizziness.Normalized;

            Rect bar = new Rect(18f, 64f, 300f, 14f);
            DrawBar(bar, n, n > 0.85f ? new Color(1f, 0.25f, 0.25f) : n > 0.5f ? new Color(1f, 0.75f, 0.2f) : new Color(0.35f, 0.85f, 1f));

            GUI.Label(new Rect(18f, 80f, 420f, 20f),
                "DIZZINESS  " + dizziness.value.ToString("0") + "%   " + dizziness.StateLabel, body);
        }

        void DrawAmmo()
        {
            if (revolver == null) return;
            string ammo = revolver.bullets + " / " + revolver.magazineSize;
            GUI.Label(new Rect(Screen.width - 190f, Screen.height - 90f, 170f, 44f), ammo, big);

            if (revolver.IsReloading)
            {
                Rect bar = new Rect(Screen.width - 190f, Screen.height - 40f, 160f, 12f);
                DrawBar(bar, revolver.ReloadProgress, new Color(1f, 0.8f, 0.3f));
                GUI.Label(new Rect(Screen.width - 190f, Screen.height - 62f, 170f, 20f), "RELOADING", body);
            }
            else if (revolver.bullets == 0)
            {
                GUI.Label(new Rect(Screen.width - 190f, Screen.height - 62f, 170f, 20f), "[R] RELOAD", body);
            }
        }

        void DrawHelp()
        {
            GUI.Label(new Rect(18f, Screen.height - 92f, 640f, 80f),
                "WASD move   SHIFT run   CTRL/C crouch + slide   SPACE jump   LMB fire   R reload\n" +
                "GRAVITY SHIFT: 1 floor / 2 left wall / 3 ceiling / 4 right wall  (Q / E rotate)   ESC cursor",
                body);
        }

        void DrawStats()
        {
            string speed = motor != null ? motor.CurrentSpeed.ToString("0.0") + " m/s" : "-";
            string grounded = motor != null ? (motor.IsGrounded ? "GROUND" : "AIR") : "-";
            string slide = motor != null && motor.IsSliding ? "  SLIDE" : "";
            GUI.Label(new Rect(Screen.width - 240f, 16f, 220f, 22f), speed + "  " + grounded + slide, body);
        }

        static void DrawBar(Rect rect, float fill, Color color)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fill), rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
