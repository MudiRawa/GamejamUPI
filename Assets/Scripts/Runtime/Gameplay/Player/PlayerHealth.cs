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

        /// <summary>0 = layar normal, 1 = layar gelap penuh (dipakai HealthDisplay).</summary>
        public float Kegelapan { get { return gelap; } }
        public int WaveTerakhir { get { return waveTerakhir; } }
        public float WaktuMati { get { return waktuMati; } }

        float gelap;          // 0 = layar normal, 1 = item
        float waktuMati;
        int waveTerakhir;
        float darahTampil;

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

        void OnGUI_Dihapus()
        {
            // UI darah & layar kalah sekarang ada di HealthDisplay (Canvas/uGUI).
        }
    }
}
