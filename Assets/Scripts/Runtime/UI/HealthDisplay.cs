using UnityEngine;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    /// <summary>
    /// Bar darah di kiri bawah + layar kalah ("KAMU TUMBANG").
    /// Menggantikan OnGUI di PlayerHealth.
    /// </summary>
    public class HealthDisplay : MonoBehaviour
    {
        public UITheme theme;

        [Header("Canvas")]
        public Canvas canvas;        // Canvas_HUD untuk bar darah
        public Canvas canvasKalah;   // Canvas_Kalah (di atas semua HUD)

        [Header("Gameplay")]
        public VertigoShift.Gameplay.Player.PlayerHealth health;

        RectTransform panelDarah;
        Image barIsi;
        Text txtDarah;

        Image panelGelap;
        Text txtKalah, txtWave, txtHint;

        float darahTampil = 100f;
        bool tadiMati;

        void Start()
        {
            canvas = UIBind.AmbilCanvas(canvas, "Canvas_HUD");

            if (canvasKalah == null)
            {
                GameObject go = GameObject.Find("Canvas_Kalah");
                if (go != null) canvasKalah = go.GetComponent<Canvas>();
            }
            if (canvasKalah == null)
            {
                canvasKalah = UIFactory.BuatCanvas("Canvas_Kalah");
                canvasKalah.sortingOrder = 200;
            }

            if (!CobaBind()) BuatUI();

            if (health != null) darahTampil = health.darah;
            tadiMati = health != null && health.Mati;
            if (canvasKalah != null) canvasKalah.gameObject.SetActive(tadiMati);
            if (panelDarah != null) panelDarah.gameObject.SetActive(!tadiMati);
        }

        /// <summary>Dipanggil oleh editor builder untuk membuat UI langsung di scene.</summary>
        public void BangunUI(Canvas hud, Canvas kalah, UITheme t)
        {
            canvas = hud;
            canvasKalah = kalah;
            theme = t;
            BuatUI();
            if (canvasKalah != null) canvasKalah.gameObject.SetActive(false);
        }

        bool CobaBind()
        {
            if (canvas == null || canvasKalah == null) return false;

            panelDarah = UIBind.Rect(canvas.transform, "Panel_Darah");
            if (panelDarah == null) return false;

            barIsi = UIBind.Gambar(panelDarah, "Bar_Isi");
            txtDarah = UIBind.Teks(panelDarah, "Txt_Darah");

            panelGelap = UIBind.Gambar(canvasKalah.transform, "Panel_Gelap");
            txtKalah = UIBind.Teks(canvasKalah.transform, "Txt_Kalah");
            txtWave = UIBind.Teks(canvasKalah.transform, "Txt_Wave");
            txtHint = UIBind.Teks(canvasKalah.transform, "Txt_Hint");

            SiapkanBar();
            return panelGelap != null && txtKalah != null;
        }

        void SiapkanBar()
        {
            if (barIsi != null)
            {
                barIsi.type = Image.Type.Filled;
                barIsi.fillMethod = Image.FillMethod.Horizontal;
                barIsi.fillOrigin = (int)Image.OriginHorizontal.Left;
                barIsi.fillAmount = 1f;
            }
        }

        void BuatUI()
        {
            Color putih = theme != null ? theme.teksPutih : new Color(0.94f, 0.93f, 0.89f);
            Color merah = theme != null ? theme.merah : new Color(0.92f, 0.2f, 0.2f);

            // --- Bar darah (kiri bawah) ---
            panelDarah = UIFactory.BuatPanel(canvas.transform, "Panel_Darah",
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(28f, 28f), new Vector2(292f, 42f),
                new Color(0.06f, 0.06f, 0.07f, 0.85f));

            barIsi = UIFactory.BuatImage(panelDarah, "Bar_Isi", null, putih,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(2f, 2f), new Vector2(-2f, -2f));

            txtDarah = UIFactory.BuatText(panelDarah, "Txt_Darah", "100",
                theme != null ? theme.fontAngka : null, theme != null ? theme.ukuranTulisan : 22,
                putih, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 16f), new Vector2(200f, 44f));

            SiapkanBar();

            // --- Layar kalah ---
            panelGelap = UIFactory.BuatImage(canvasKalah.transform, "Panel_Gelap", null, new Color(0f, 0f, 0f, 0f),
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, 0f));
            panelGelap.raycastTarget = false;

            txtKalah = UIFactory.BuatText(canvasKalah.transform, "Txt_Kalah", "KAMU TUMBANG",
                theme != null ? theme.fontJudul : null, 72,
                merah, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-600f, 57f), new Vector2(600f, 147f));

            txtWave = UIFactory.BuatText(canvasKalah.transform, "Txt_Wave", "BERTAHAN SAMPAI WAVE 1",
                theme != null ? theme.fontTulisan : null, 26,
                putih, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-600f, 17f), new Vector2(600f, 57f));

            txtHint = UIFactory.BuatText(canvasKalah.transform, "Txt_Hint", "klik di mana aja buat balik ke menu",
                theme != null ? theme.fontTulisan : null, 16,
                Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-600f, -53f), new Vector2(600f, -23f));
        }

        void Update()
        {
            if (health == null)
            {
                // PlayerHealth baru dipasang saat player pertama kali kena tembak,
                // jadi cari ulang kalau belum ada.
                health = FindAnyObjectByType<VertigoShift.Gameplay.Player.PlayerHealth>();
                if (health == null) return;
                darahTampil = health.darah;
            }

            if (health.Mati != tadiMati)
            {
                tadiMati = health.Mati;
                if (canvasKalah != null) canvasKalah.gameObject.SetActive(tadiMati);
                if (panelDarah != null) panelDarah.gameObject.SetActive(!tadiMati);
            }

            if (!health.Mati)
            {
                darahTampil = Mathf.Lerp(darahTampil, health.darah, 8f * Time.unscaledDeltaTime);
                float isi = health.darahMaks > 0f ? Mathf.Clamp01(darahTampil / health.darahMaks) : 0f;
                bool kritis = isi < 0.3f;

                Color warna = kritis
                    ? (theme != null ? theme.merah : new Color(0.92f, 0.2f, 0.2f))
                    : (theme != null ? theme.teksPutih : new Color(0.94f, 0.93f, 0.89f));
                if (kritis) warna.a = 0.5f + Mathf.PingPong(Time.unscaledTime * 3f, 0.5f);

                if (barIsi != null)
                {
                    barIsi.fillAmount = isi;
                    barIsi.color = warna;
                }
                if (txtDarah != null)
                {
                    txtDarah.text = Mathf.CeilToInt(health.darah).ToString();
                    txtDarah.color = warna;
                }
                return;
            }

            // --- Layar kalah ---
            float gelap = Mathf.Clamp01(health.Kegelapan);
            if (panelGelap != null) panelGelap.color = new Color(0f, 0f, 0f, gelap * 0.92f);

            float a = Mathf.Clamp01((gelap - 0.5f) * 2f);

            Color merah = theme != null ? theme.merah : new Color(0.92f, 0.2f, 0.2f);
            if (txtKalah != null) txtKalah.color = new Color(merah.r, merah.g, merah.b, a);

            if (txtWave != null)
            {
                Color putih = theme != null ? theme.teksPutih : new Color(0.94f, 0.93f, 0.89f);
                txtWave.color = new Color(putih.r, putih.g, putih.b, a);
                txtWave.text = "BERTAHAN SAMPAI WAVE " + health.WaveTerakhir;
            }

            if (txtHint != null)
            {
                bool tampil = Time.unscaledTime - health.WaktuMati > 2f;
                float kedip = 0.35f + Mathf.PingPong(Time.unscaledTime, 0.65f);
                txtHint.color = new Color(1f, 1f, 1f, tampil ? a * kedip : 0f);
            }
        }
    }
}
