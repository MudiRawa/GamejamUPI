using UnityEngine;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    /// <summary>
    /// Meter pusing/panik di kiri atas.
    /// Memakai objek Panel_Pusing (beserta isinya) yang sudah ada di Canvas scene bila tersedia.
    /// </summary>
    public class DizzinessDisplay : MonoBehaviour
    {
        public UITheme theme;
        public Canvas canvas;
        public VertigoShift.Gameplay.Panic.DizzinessSystem dizziness;

        RectTransform rtPanel;
        Text lblPusing, txtAngka, txtPersen, txtStatus;
        Image barLatar, barIsi, tanda50, tanda85;

        Vector2 posisiDasar;
        float pusingTampil;
        float getar;

        void Start()
        {
            canvas = UIBind.AmbilCanvas(canvas, "Canvas_HUD");
            if (!CobaBind()) BuatUI();
            posisiDasar = rtPanel != null ? rtPanel.anchoredPosition : Vector2.zero;
        }

        /// <summary>Dipanggil oleh editor builder untuk membuat UI langsung di scene.</summary>
        public void BangunUI(Canvas c, UITheme t)
        {
            canvas = c;
            theme = t;
            BuatUI();
            posisiDasar = rtPanel != null ? rtPanel.anchoredPosition : Vector2.zero;
        }

        bool CobaBind()
        {
            if (canvas == null) return false;
            rtPanel = UIBind.Rect(canvas.transform, "Panel_Pusing");
            if (rtPanel == null) return false;

            lblPusing = UIBind.Teks(rtPanel, "Lbl_Pusing");
            txtAngka = UIBind.Teks(rtPanel, "Txt_Angka");
            txtPersen = UIBind.Teks(rtPanel, "Txt_Persen");
            txtStatus = UIBind.Teks(rtPanel, "Txt_Status");
            barLatar = UIBind.Gambar(rtPanel, "Bar_Latar");
            barIsi = UIBind.Gambar(rtPanel, "Bar_Isi");
            tanda50 = UIBind.Gambar(rtPanel, "Tanda_50");
            tanda85 = UIBind.Gambar(rtPanel, "Tanda_85");

            SiapkanBar();
            return true;
        }

        void SiapkanBar()
        {
            if (barIsi != null)
            {
                barIsi.type = Image.Type.Filled;
                barIsi.fillMethod = Image.FillMethod.Horizontal;
                barIsi.fillOrigin = (int)Image.OriginHorizontal.Left;
                barIsi.fillAmount = 0f;
            }
        }

        void BuatUI()
        {
            Color cyan = theme != null ? theme.cyan : new Color(0.16f, 0.94f, 1f);

            // Panel 290 x 102 di kiri atas (anchor top-left)
            var panel = UIFactory.BuatPanel(canvas.transform, "Panel_Pusing",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -120f), new Vector2(320f, -18f));
            rtPanel = panel;

            float panjang = 220f;

            // Baris label + garis
            lblPusing = UIFactory.BuatText(panel, "Lbl_Pusing", "PUSING",
                theme != null ? theme.fontTulisan : null, theme != null ? theme.ukuranKecil : 16,
                cyan, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 64f), new Vector2(130f, 84f));

            UIFactory.BuatGaris(panel, "Garis_Label", new Color(cyan.r, cyan.g, cyan.b, 0.6f), 2f,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 60f), new Vector2(panjang, 62f));
            UIFactory.BuatImage(panel, "Ujung_Garis", null, new Color(cyan.r, cyan.g, cyan.b, 0.6f),
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(panjang, 57f), new Vector2(panjang + 6f, 65f));

            // Angka besar + %
            txtAngka = UIFactory.BuatText(panel, "Txt_Angka", "0",
                theme != null ? theme.fontAngka : null, theme != null ? theme.ukuranAngkaSedang : 40,
                Color.white, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 30f), new Vector2(130f, 76f));

            txtPersen = UIFactory.BuatText(panel, "Txt_Persen", "%",
                theme != null ? theme.fontTulisan : null, theme != null ? theme.ukuranKecil : 14,
                new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(4f, 38f), new Vector2(44f, 62f));

            txtStatus = UIFactory.BuatText(panel, "Txt_Status", "STABIL",
                theme != null ? theme.fontTulisan : null, theme != null ? theme.ukuranKecil : 14,
                cyan, TextAnchor.MiddleRight,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(panjang - 120f, 40f), new Vector2(panjang, 64f));

            // Bar tipis + takik 50% & 85%
            barLatar = UIFactory.BuatImage(panel, "Bar_Latar", null, new Color(1f, 1f, 1f, 0.15f),
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 4f), new Vector2(panjang, 8f));

            barIsi = UIFactory.BuatImage(panel, "Bar_Isi", null, Color.white,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 4f), new Vector2(panjang, 8f));

            tanda50 = UIFactory.BuatImage(panel, "Tanda_50", null, new Color(1f, 1f, 1f, 0.6f),
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(panjang * 0.5f - 1f, 1f), new Vector2(panjang * 0.5f + 1f, 11f));
            tanda85 = UIFactory.BuatImage(panel, "Tanda_85", null, new Color(1f, 1f, 1f, 0.6f),
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(panjang * 0.85f - 1f, 1f), new Vector2(panjang * 0.85f + 1f, 11f));

            SiapkanBar();
        }

        void Update()
        {
            if (dizziness == null || rtPanel == null) return;

            Color cyan = theme != null ? theme.cyan : new Color(0.16f, 0.94f, 1f);
            float n = dizziness.Normalized;
            float dt = Time.unscaledDeltaTime;

            if (n > pusingTampil + 0.01f) getar = 1f;
            pusingTampil = Mathf.Lerp(pusingTampil, n, 10f * dt);
            getar = Mathf.MoveTowards(getar, 0f, 3f * dt);

            Color warna = cyan;
            string status = "STABIL";
            if (dizziness.IsHighTension) { warna = new Color(1f, 0.82f, 0.23f); status = "TEGANG"; }
            if (dizziness.IsCritical)
            {
                warna = new Color(1f, 0.25f, 0.25f);
                status = "PANIK";
                warna.a = 0.4f + Mathf.PingPong(Time.unscaledTime * 3f, 0.6f);
            }

            float x = Random.Range(-1f, 1f) * getar * 4f;
            float y = Random.Range(-1f, 1f) * getar * 4f;
            rtPanel.anchoredPosition = posisiDasar + new Vector2(x, y);

            if (lblPusing != null) lblPusing.color = cyan;
            if (txtAngka != null)
            {
                txtAngka.text = Mathf.RoundToInt(dizziness.value).ToString();
                txtAngka.color = Color.white;
            }
            if (txtStatus != null)
            {
                txtStatus.text = status;
                txtStatus.color = warna;
            }

            if (barIsi != null)
            {
                barIsi.fillAmount = Mathf.Clamp01(pusingTampil);
                barIsi.color = warna;
            }
            if (barLatar != null) barLatar.color = new Color(1f, 1f, 1f, 0.15f);
        }
    }
}
