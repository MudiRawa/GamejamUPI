using UnityEngine;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    /// <summary>
    /// Panel bantuan (F1) dan panel statistik di kanan atas.
    /// Memakai objek Panel_Help / Panel_Stats yang sudah ada di Canvas scene bila tersedia.
    /// </summary>
    public class HelpDisplay : MonoBehaviour
    {
        public UITheme theme;
        public Canvas canvas;
        public VertigoShift.Gameplay.Player.PlayerMotor motor;

        public bool helpAktif = false;

        Text teksHelp;
        Text teksStats;
        RectTransform panelHelp;

        void Start()
        {
            canvas = UIBind.AmbilCanvas(canvas, "Canvas_HUD");
            if (!CobaBind())
            {
                BuatUI();
                tampilkanHelp(false);
                tampilkanStats(false);
            }
        }

        /// <summary>Dipanggil oleh editor builder untuk membuat UI langsung di scene.</summary>
        public void BangunUI(Canvas c, UITheme t)
        {
            canvas = c;
            theme = t;
            BuatUI();
            tampilkanHelp(false);
            tampilkanStats(false);
        }

        bool CobaBind()
        {
            if (canvas == null) return false;

            panelHelp = UIBind.Rect(canvas.transform, "Panel_Help");
            if (panelHelp == null) return false;

            teksHelp = UIBind.Teks(panelHelp, "Txt_Help");
            if (teksHelp == null) return false;

            RectTransform panelStats = UIBind.Rect(canvas.transform, "Panel_Stats");
            if (panelStats != null) teksStats = UIBind.Teks(panelStats, "Txt_Stats");

            return true;
        }

        void BuatUI()
        {
            var t = theme;

            // Panel di pojok kiri atas (menempel ke anchor, jadi aman untuk semua rasio layar)
            panelHelp = UIFactory.BuatPanel(canvas.transform, "Panel_Help",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -168f), new Vector2(658f, -88f));

            teksHelp = UIFactory.BuatText(panelHelp, "Txt_Help",
                "WASD move   SHIFT run   CTRL/C crouch + slide   SPACE jump   LMB fire   R reload\n" +
                "GRAVITY SHIFT: 1 floor / 2 left wall / 3 ceiling / 4 right wall  (Q / E rotate)   ESC cursor",
                t != null ? t.fontTulisan : null, t != null ? t.ukuranKecil : 14,
                new Color(0.68f, 0.78f, 0.88f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -80f), new Vector2(640f, 0f));

            // Panel stats di pojok kanan atas
            var panelStats = UIFactory.BuatPanel(canvas.transform, "Panel_Stats",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-240f, -40f), new Vector2(-20f, -18f));

            teksStats = UIFactory.BuatText(panelStats, "Txt_Stats", "-",
                t != null ? t.fontTulisan : null, t != null ? t.ukuranKecil : 13,
                new Color(0.68f, 0.78f, 0.88f), TextAnchor.MiddleRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-220f, -16f), new Vector2(0f, 6f));
        }

        void Update()
        {
            if (teksStats != null && motor != null)
            {
                string speed = motor.CurrentSpeed.ToString("0.0") + " m/s";
                string grounded = motor.IsGrounded ? "GROUND" : "AIR";
                string slide = motor.IsSliding ? "  SLIDE" : "";
                teksStats.text = speed + "  " + grounded + slide;
            }
        }

        void tampilkanHelp(bool aktif)
        {
            if (panelHelp != null) panelHelp.gameObject.SetActive(aktif);
            if (teksHelp != null) teksHelp.gameObject.SetActive(aktif);
        }

        void tampilkanStats(bool aktif)
        {
            if (teksStats != null) teksStats.gameObject.SetActive(aktif);
        }

        // Dipanggil dari HUDController saat F1 ditekan
        public void SetHelpAktif(bool aktif)
        {
            helpAktif = aktif;
            tampilkanHelp(aktif);
            tampilkanStats(aktif);
        }
    }
}
