using UnityEngine;

namespace VertigoShift.UI
{
    /// <summary>
    /// Entry point wave HUD lama. Sekarang hanya meneruskan referensi
    /// ke WaveAnnouncement berbasis Canvas.
    /// </summary>
    public class WaveHUD : MonoBehaviour
    {
        public VertigoShift.Gameplay.Waves.EnemyWaveManager waveManager;

        [Header("Font (diisi lewat Inspector)")]
        public UnityEngine.Font fontAngka;
        public UnityEngine.Font fontTulisan;

        public float lamaTulisan = 2f;

        void Awake()
        {
            var waveAnnouncement = FindAnyObjectByType<WaveAnnouncement>();
            if (waveAnnouncement == null)
            {
                var go = new GameObject("WaveAnnouncement");
                waveAnnouncement = go.AddComponent<WaveAnnouncement>();
            }

            // Jangan timpa referensi yang sudah dipasang lewat scene/builder dengan null.
            if (waveManager != null) waveAnnouncement.waveManager = waveManager;
            if (lamaTulisan > 0f) waveAnnouncement.lamaTulisan = lamaTulisan;

            var hud = FindAnyObjectByType<HUDController>();
            if (hud != null)
            {
                if (fontAngka != null) hud.fontAngka = fontAngka;
                if (fontTulisan != null) hud.fontTulisan = fontTulisan;
            }
        }
    }
}
