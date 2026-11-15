using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VertigoShift.Gameplay.Waves;
using VertigoShift.UI;

namespace VertigoShift.Gameplay.Player
{
    // darah pemain. kena tembak -> darah berkurang + layar merah (PlayerKena).
    // darah habis -> kalah: kontrol mati, layar pelan-pelan gelap, muncul wave terakhir,
    // klik di mana aja -> balik ke main menu.
    // dipasang di objek Player (otomatis lewat Tools > VFX > Pasang VFX ke Scene)
    public class PlayerHealth : MonoBehaviour
    {
        public float darahMaks = 100f;
        public float darah = 100f;
        public string sceneMenu = "MainMenu";

        [Header("Font (boleh kosong)")]
        public Font fontAngka;
        public Font fontTulisan;

        public bool Mati { get; private set; }

        float gelap;          // 0 = layar normal, 1 = item
        float waktuMati;
        int waveTerakhir;
        float darahTampil;
        GUIStyle gayaJudul, gayaWave, gayaKecil, gayaAngka;

        void Start()
        {
            darah = darahMaks;
            darahTampil = darah;
        }

        public void TakeDamage(float jumlah, Vector3 dariMana)
        {
            if (Mati) return;
            darah = Mathf.Max(0f, darah - jumlah);
            PlayerKena.Kena(dariMana);
            if (darah <= 0f) Kalah();
        }

        void Kalah()
        {
            Mati = true;
            waktuMati = Time.unscaledTime;

            // matiin semua kontrol pemain (gerak, liat, nembak, gravity)
            foreach (MonoBehaviour skrip in GetComponentsInChildren<MonoBehaviour>())
                if (skrip != this) skrip.enabled = false;

            // wave berhenti
            EnemyWaveManager wave = FindAnyObjectByType<EnemyWaveManager>();
            if (wave != null)
            {
                waveTerakhir = wave.CurrentWave;
                wave.StopAllCoroutines();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0.3f; // pelan dramatis pas layar gelap
        }

        void Update()
        {
            darahTampil = Mathf.Lerp(darahTampil, darah, 8f * Time.unscaledDeltaTime);
            if (!Mati) return;

            gelap = Mathf.MoveTowards(gelap, 1f, Time.unscaledDeltaTime / 1.5f);
            if (gelap >= 1f) Time.timeScale = 0f;

            // klik di mana aja (atau tombol apa aja) buat balik ke menu, tapi tunggu layarnya gelap dulu
            bool klik = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                     || (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame);
            if (gelap >= 1f && Time.unscaledTime - waktuMati > 2f && klik)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(sceneMenu);
            }
        }

        void OnGUI()
        {
            if (gayaJudul == null)
            {
                gayaJudul = Gaya(fontAngka, 72, TextAnchor.MiddleCenter);
                gayaWave = Gaya(fontTulisan, 26, TextAnchor.MiddleCenter);
                gayaKecil = Gaya(fontTulisan, 16, TextAnchor.MiddleCenter);
                gayaAngka = Gaya(fontAngka, 22, TextAnchor.MiddleLeft);
            }

            if (!Mati) GambarDarah();
            else GambarKalah();
        }

        // bar darah di pojok kiri bawah
        void GambarDarah()
        {
            float lebar = 260f, tinggi = 10f;
            float x = 30f;
            float y = Screen.height - 40f;
            float isi = darahTampil / darahMaks;
            bool kritis = isi < 0.3f;

            GUI.color = new Color(0.06f, 0.06f, 0.07f, 0.85f);
            GUI.DrawTexture(new Rect(x - 2f, y - 2f, lebar + 4f, tinggi + 4f), Texture2D.whiteTexture);
            Color warna = kritis ? new Color(0.92f, 0.2f, 0.2f) : new Color(0.94f, 0.93f, 0.89f);
            if (kritis) warna.a = 0.5f + Mathf.PingPong(Time.unscaledTime * 3f, 0.5f);
            GUI.color = warna;
            GUI.DrawTexture(new Rect(x, y, lebar * isi, tinggi), Texture2D.whiteTexture);

            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(x + 2f, y - 30f, 200f, 28f), Mathf.CeilToInt(darah).ToString(), gayaAngka);
            GUI.color = warna;
            GUI.Label(new Rect(x, y - 32f, 200f, 28f), Mathf.CeilToInt(darah).ToString(), gayaAngka);
            GUI.color = Color.white;
        }

        // layar gelap + tulisan kalah
        void GambarKalah()
        {
            GUI.depth = -100; // paling depan, nutup HUD lain
            Rect layar = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.color = new Color(0f, 0f, 0f, gelap * 0.92f);
            GUI.DrawTexture(layar, Texture2D.whiteTexture);

            // tulisan muncul setelah layar setengah gelap
            float a = Mathf.Clamp01((gelap - 0.5f) * 2f);
            if (a <= 0f) { GUI.color = Color.white; return; }
            float cy = Screen.height * 0.42f;

            Tulis(new Rect(0f, cy - 60f, Screen.width, 90f), "KAMU TUMBANG", gayaJudul, new Color(0.92f, 0.2f, 0.2f, a));
            Tulis(new Rect(0f, cy + 30f, Screen.width, 40f), "BERTAHAN SAMPAI WAVE " + waveTerakhir, gayaWave, new Color(0.94f, 0.93f, 0.89f, a));

            if (Time.unscaledTime - waktuMati > 2f)
            {
                float kedip = 0.35f + Mathf.PingPong(Time.unscaledTime, 0.65f);
                Tulis(new Rect(0f, cy + 110f, Screen.width, 30f), "klik di mana aja buat balik ke menu", gayaKecil, new Color(1f, 1f, 1f, a * kedip));
            }
            GUI.color = Color.white;
        }

        static void Tulis(Rect r, string teks, GUIStyle gaya, Color warna)
        {
            GUI.color = new Color(0f, 0f, 0f, warna.a);
            GUI.Label(new Rect(r.x + 4f, r.y + 4f, r.width, r.height), teks, gaya);
            GUI.color = warna;
            GUI.Label(r, teks, gaya);
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
