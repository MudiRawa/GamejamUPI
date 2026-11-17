using UnityEngine;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    /// <summary>
    /// Indikator gravity di kiri atas (teks kecil).
    /// Memakai objek Txt_Gravity yang sudah ada di Canvas scene bila tersedia.
    /// </summary>
    public class GravityDisplay : MonoBehaviour
    {
        public UITheme theme;
        public Canvas canvas;
        public VertigoShift.Gameplay.Gravity.GravityShift gravity;

        Text teksGravity;

        void Start()
        {
            canvas = UIBind.AmbilCanvas(canvas, "Canvas_HUD");
            if (!CobaBind()) BuatUI();
        }

        public void BangunUI(Canvas c, UITheme t)
        {
            canvas = c;
            theme = t;
            BuatUI();
        }

        bool CobaBind()
        {
            if (canvas == null) return false;
            teksGravity = UIBind.Teks(canvas.transform, "Txt_Gravity");
            return teksGravity != null;
        }

        void BuatUI()
        {
            teksGravity = UIFactory.BuatText(canvas.transform, "Txt_Gravity", "GRAVITY  FLOOR",
                theme != null ? theme.fontTulisan : null, theme != null ? theme.ukuranKecil : 14,
                new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -112f), new Vector2(330f, -92f));
        }

        void Update()
        {
            if (gravity == null || teksGravity == null) return;

            string side = gravity.CurrentSide.ToString().ToUpperInvariant();
            int streak = gravity.ShiftStreak;
            teksGravity.text = "GRAVITY  " + side + (streak > 1 ? "  x" + streak : "");
        }
    }
}
