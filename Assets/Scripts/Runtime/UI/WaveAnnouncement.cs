using UnityEngine;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    /// <summary>
    /// Penghitung wave di atas layar + pengumuman wave di tengah layar.
    /// Memakai objek Panel_Wave / Panel_WaveTengah yang sudah ada di Canvas scene bila tersedia.
    /// </summary>
    public class WaveAnnouncement : MonoBehaviour
    {
        public UITheme theme;
        public Canvas canvas;
        public VertigoShift.Gameplay.Waves.EnemyWaveManager waveManager;

        public float lamaTulisan = 2f;

        RectTransform panelWave, panelTengah;
        Text lblWave, txtWave, txtSisa;
        Image garisKiri, garisKanan, ujungKiri, ujungKanan;
        Text txtTengah, txtSub;
        Image garisTengah;

        int waveTadi, sisaTadi;
        float tulisan;
        bool beres;
        float denyut;

        void Start()
        {
            canvas = UIBind.AmbilCanvas(canvas, "Canvas_HUD");
            if (!CobaBind())
            {
                BuatUI();
                if (panelTengah != null) panelTengah.gameObject.SetActive(false);
            }
        }

        /// <summary>Dipanggil oleh editor builder untuk membuat UI langsung di scene.</summary>
        public void BangunUI(Canvas c, UITheme t)
        {
            canvas = c;
            theme = t;
            BuatUI();
            if (panelTengah != null) panelTengah.gameObject.SetActive(false);
        }

        bool CobaBind()
        {
            if (canvas == null) return false;

            panelWave = UIBind.Rect(canvas.transform, "Panel_Wave");
            if (panelWave == null) return false;

            lblWave = UIBind.Teks(panelWave, "Lbl_Wave");
            txtWave = UIBind.Teks(panelWave, "Txt_Wave");
            txtSisa = UIBind.Teks(panelWave, "Txt_Sisa");
            garisKiri = UIBind.Gambar(panelWave, "Garis_Kiri");
            garisKanan = UIBind.Gambar(panelWave, "Garis_Kanan");
            ujungKiri = UIBind.Gambar(panelWave, "Ujung_Kiri");
            ujungKanan = UIBind.Gambar(panelWave, "Ujung_Kanan");

            panelTengah = UIBind.Rect(canvas.transform, "Panel_WaveTengah");
            if (panelTengah != null)
            {
                txtTengah = UIBind.Teks(panelTengah, "Txt_Tengah");
                txtSub = UIBind.Teks(panelTengah, "Txt_Sub");
                garisTengah = UIBind.Gambar(panelTengah, "Garis_Tengah");
            }

            return true;
        }

        void BuatUI()
        {
            var t = theme;
            Color cyan = t != null ? t.cyan : new Color(0.16f, 0.94f, 1f);

            // Panel di atas tengah
            panelWave = UIFactory.BuatPanel(canvas.transform, "Panel_Wave",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-150f, -116f), new Vector2(150f, -22f));

            // Label "WAVE"
            lblWave = UIFactory.BuatText(panelWave, "Lbl_Wave", "WAVE",
                t != null ? t.fontTulisan : null, t != null ? t.ukuranKecil : 14,
                cyan, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-100f, 58f), new Vector2(100f, 76f));

            // Angka wave
            txtWave = UIFactory.BuatText(panelWave, "Txt_Wave", "00",
                t != null ? t.fontAngka : null, t != null ? t.ukuranAngkaSedang : 40,
                Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-100f, 16f), new Vector2(100f, 62f));

            // Garis kiri & kanan di bawah angka
            garisKiri = UIFactory.BuatImage(panelWave, "Garis_Kiri", null, new Color(cyan.r, cyan.g, cyan.b, 0.6f),
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 11f), new Vector2(100f, 13f));
            garisKanan = UIFactory.BuatImage(panelWave, "Garis_Kanan", null, new Color(cyan.r, cyan.g, cyan.b, 0.6f),
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(200f, 11f), new Vector2(300f, 13f));
            ujungKiri = UIFactory.BuatImage(panelWave, "Ujung_Kiri", null, new Color(cyan.r, cyan.g, cyan.b, 0.6f),
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(-6f, 8f), new Vector2(0f, 16f));
            ujungKanan = UIFactory.BuatImage(panelWave, "Ujung_Kanan", null, new Color(cyan.r, cyan.g, cyan.b, 0.6f),
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(300f, 8f), new Vector2(306f, 16f));

            // Sisa musuh di bawah
            txtSisa = UIFactory.BuatText(panelWave, "Txt_Sisa", "MUSUH  0",
                t != null ? t.fontTulisan : null, t != null ? t.ukuranKecil : 16,
                new Color(1f, 0.35f, 0.45f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-100f, -20f), new Vector2(100f, 2f));

            // --- Pengumuman di tengah layar ---
            panelTengah = UIFactory.BuatPanel(canvas.transform, "Panel_WaveTengah",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-960f, -40f), new Vector2(960f, 40f));

            txtTengah = UIFactory.BuatText(panelTengah, "Txt_Tengah", "WAVE 1",
                t != null ? t.fontAngka : null, t != null ? t.ukuranJudul : 56,
                Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-500f, -40f), new Vector2(500f, 40f));

            garisTengah = UIFactory.BuatImage(panelTengah, "Garis_Tengah", null, new Color(cyan.r, cyan.g, cyan.b, 0.7f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-180f, 40f), new Vector2(180f, 42f));

            txtSub = UIFactory.BuatText(panelTengah, "Txt_Sub", "MUSUH DATANG",
                t != null ? t.fontTulisan : null, t != null ? t.ukuranTulisan : 16,
                cyan, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-500f, 46f), new Vector2(500f, 72f));
        }

        void Update()
        {
            if (waveManager == null) return;

            int wave = waveManager.CurrentWave;
            int sisa = waveManager.EnemiesRemaining;
            float dt = Time.unscaledDeltaTime;

            if (wave != waveTadi && wave > 0) { beres = false; tulisan = 1f; }
            if (sisaTadi > 0 && sisa == 0) { beres = true; tulisan = 1f; }
            if (sisa < sisaTadi) denyut = 1f;
            waveTadi = wave;
            sisaTadi = sisa;

            tulisan = Mathf.MoveTowards(tulisan, 0f, dt / lamaTulisan);
            denyut = Mathf.MoveTowards(denyut, 0f, dt * 4f);

            // Update wave counter
            if (txtWave != null) txtWave.text = wave.ToString("00");

            // Update sisa musuh
            if (txtSisa != null)
            {
                float ukuranAsli = theme != null ? theme.ukuranKecil : 16;
                txtSisa.fontSize = Mathf.RoundToInt(ukuranAsli * (1f + denyut * 0.3f));

                if (sisa > 0)
                {
                    txtSisa.text = "MUSUH  " + sisa;
                    txtSisa.color = new Color(1f, 0.35f, 0.45f);
                }
                else if (wave > 0)
                {
                    txtSisa.text = "AMAN";
                    txtSisa.color = theme != null ? theme.cyan : Color.cyan;
                }
                else
                {
                    txtSisa.text = "";
                }
            }

            // Pengumuman tengah layar
            if (panelTengah != null)
            {
                if (tulisan > 0f && wave > 0)
                {
                    panelTengah.gameObject.SetActive(true);

                    float t = 1f - tulisan;
                    float a = Mathf.Clamp01(tulisan * 3f);
                    float masuk = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 5f));

                    if (txtTengah != null)
                    {
                        txtTengah.text = beres ? "WAVE " + wave + " BERES" : "WAVE " + wave;
                        txtTengah.color = new Color(1f, 1f, 1f, a);
                    }

                    if (garisTengah != null)
                    {
                        float lebar = 360f * masuk;
                        garisTengah.rectTransform.sizeDelta = new Vector2(lebar, 2f);
                        garisTengah.color = new Color(
                            theme != null ? theme.cyan.r : 0.16f,
                            theme != null ? theme.cyan.g : 0.94f,
                            theme != null ? theme.cyan.b : 1f,
                            0.7f * a);
                    }

                    if (txtSub != null)
                    {
                        txtSub.text = beres ? "SIAP-SIAP" : "MUSUH DATANG";
                        txtSub.color = new Color(
                            theme != null ? theme.cyan.r : 0.16f,
                            theme != null ? theme.cyan.g : 0.94f,
                            theme != null ? theme.cyan.b : 1f,
                            a);
                    }
                }
                else
                {
                    panelTengah.gameObject.SetActive(false);
                }
            }
        }
    }
}
