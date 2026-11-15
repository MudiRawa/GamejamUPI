using UnityEngine;
using VertigoShift.Gameplay.Waves;

namespace VertigoShift.UI
{
    /// <summary>
    /// HUD wave & sisa musuh di atas tengah. Gayanya sama kayak HUD revolver:
    /// label kecil cyan, garis tipis, angka gede putih. Pas wave mulai / beres ada tulisan di tengah layar.
    /// </summary>
    public class WaveHUD : MonoBehaviour
    {
        public EnemyWaveManager waveManager;

        [Header("Font (diisi lewat Tools > VFX > Pasang VFX ke Scene)")]
        public Font fontAngka;
        public Font fontTulisan;

        public float lamaTulisan = 2f; // berapa detik tulisan "WAVE 3" di tengah layar

        static readonly Color Cyan = new Color(0.16f, 0.94f, 1f);

        GUIStyle gayaLabel, gayaAngka, gayaMusuh, gayaTengah, gayaSub;
        int waveTadi, sisaTadi;
        float tulisan;   // 1 = baru muncul, 0 = udah ilang
        bool beres;
        float denyut;    // angka musuh membesar dikit pas ada yang mati

        void Update()
        {
            if (waveManager == null)
            {
                waveManager = FindAnyObjectByType<EnemyWaveManager>();
                if (waveManager == null) return;
            }

            int wave = waveManager.CurrentWave;
            int sisa = waveManager.EnemiesRemaining;
            if (wave != waveTadi && wave > 0) { beres = false; tulisan = 1f; }
            if (sisaTadi > 0 && sisa == 0) { beres = true; tulisan = 1f; }
            if (sisa < sisaTadi) denyut = 1f;
            waveTadi = wave;
            sisaTadi = sisa;

            float dt = Time.unscaledDeltaTime;
            tulisan = Mathf.MoveTowards(tulisan, 0f, dt / lamaTulisan);
            denyut = Mathf.MoveTowards(denyut, 0f, dt * 4f);
        }

        void OnGUI()
        {
            if (waveManager == null) return;
            if (gayaLabel == null)
            {
                gayaLabel = Gaya(fontTulisan, 14, TextAnchor.MiddleCenter);
                gayaAngka = Gaya(fontAngka, 40, TextAnchor.MiddleCenter);
                gayaMusuh = Gaya(fontTulisan, 16, TextAnchor.MiddleCenter);
                gayaTengah = Gaya(fontAngka, 56, TextAnchor.MiddleCenter);
                gayaSub = Gaya(fontTulisan, 16, TextAnchor.MiddleCenter);
            }

            float cx = Screen.width * 0.5f;
            int wave = waveManager.CurrentWave;
            int sisa = waveManager.EnemiesRemaining;

            // ---- atas tengah: WAVE + angka + garis kiri kanan
            Glow(new Rect(cx - 100f, 12f, 200f, 18f), "WAVE", gayaLabel, Cyan);
            Glow(new Rect(cx - 100f, 26f, 200f, 46f), wave.ToString("00"), gayaAngka, Color.white);
            GUI.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.6f);
            GUI.DrawTexture(new Rect(cx - 150f, 49f, 100f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + 50f, 49f, 100f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 156f, 46f, 6f, 8f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + 150f, 46f, 6f, 8f), Texture2D.whiteTexture);

            // sisa musuh di bawahnya
            int ukuranAsli = gayaMusuh.fontSize;
            gayaMusuh.fontSize = Mathf.RoundToInt(ukuranAsli * (1f + denyut * 0.3f));
            if (sisa > 0) Glow(new Rect(cx - 100f, 72f, 200f, 22f), "MUSUH  " + sisa, gayaMusuh, new Color(1f, 0.35f, 0.45f));
            else if (wave > 0) Glow(new Rect(cx - 100f, 72f, 200f, 22f), "AMAN", gayaMusuh, Cyan);
            gayaMusuh.fontSize = ukuranAsli;

            // ---- tulisan di tengah layar pas wave mulai / beres
            if (tulisan > 0f && wave > 0)
            {
                float t = 1f - tulisan;
                float a = Mathf.Clamp01(tulisan * 3f);                       // ilang pelan di akhir
                float masuk = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 5f)); // garis melebar di awal
                float ty = Screen.height * 0.3f;

                Glow(new Rect(0f, ty - 40f, Screen.width, 80f), beres ? "WAVE " + wave + " BERES" : "WAVE " + wave,
                     gayaTengah, new Color(1f, 1f, 1f, a));
                GUI.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.7f * a);
                float lebar = 360f * masuk;
                GUI.DrawTexture(new Rect(cx - lebar * 0.5f, ty + 38f, lebar, 2f), Texture2D.whiteTexture);
                Glow(new Rect(0f, ty + 44f, Screen.width, 26f), beres ? "SIAP-SIAP" : "MUSUH DATANG",
                     gayaSub, new Color(Cyan.r, Cyan.g, Cyan.b, a));
            }
            GUI.color = Color.white;
        }

        // tulisan dengan cahaya cyan tipis (sama kayak HUD revolver)
        static void Glow(Rect r, string teks, GUIStyle gaya, Color warna)
        {
            GUI.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.25f * warna.a);
            GUI.Label(new Rect(r.x - 2f, r.y, r.width, r.height), teks, gaya);
            GUI.Label(new Rect(r.x + 2f, r.y, r.width, r.height), teks, gaya);
            GUI.Label(new Rect(r.x, r.y - 2f, r.width, r.height), teks, gaya);
            GUI.Label(new Rect(r.x, r.y + 2f, r.width, r.height), teks, gaya);
            GUI.color = warna;
            GUI.Label(r, teks, gaya);
            GUI.color = Color.white;
        }

        static GUIStyle Gaya(Font font, int ukuran, TextAnchor rata)
        {
            GUIStyle gaya = new GUIStyle(GUI.skin.label);
            gaya.font = font;
            gaya.fontSize = ukuran;
            gaya.alignment = rata;
            gaya.normal.textColor = Color.white;
            gaya.clipping = TextClipping.Overflow;
            if (font == null) gaya.fontStyle = FontStyle.Bold;
            return gaya;
        }
    }
}
