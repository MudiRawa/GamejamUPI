using UnityEngine;

namespace VertigoShift.UI
{
    [CreateAssetMenu(menuName = "Vertigo Shift/UI Theme", fileName = "UITheme")]
    public class UITheme : ScriptableObject
    {
        [Header("Warna Merek")]
        public Color magenta = new Color(1f, 0.15f, 0.7f);
        public Color cyan = new Color(0.16f, 0.94f, 1f);
        public Color merah = new Color(1f, 0.2f, 0.25f);
        public Color kuning = new Color(1f, 0.82f, 0.23f);
        public Color oranye = new Color(1f, 0.45f, 0.1f);

        [Header("Warna Dasar")]
        public Color teksPutih = Color.white;
        public Color teksAbu = new Color(0.68f, 0.78f, 0.88f);
        public Color panelLatar = new Color(0f, 0f, 0f, 0.55f);
        public Color garisCyan = new Color(0.16f, 0.94f, 1f, 0.6f);

        [Header("Font (Tektur disarankan)")]
        public Font fontJudul;
        public Font fontTulisan;
        public Font fontAngka;

        [Header("Ukuran Default (disesuaikan otomatis via CanvasScaler)")]
        public int ukuranJudul = 100;
        public int ukuranTombol = 46;
        public int ukuranTulisan = 22;
        public int ukuranKecil = 14;
        public int ukuranAngkaBesar = 56;
        public int ukuranAngkaSedang = 40;

        [Header("Animasi")]
        public float durasiFade = 0.3f;
        public float kecepatanGetar = 15f;
    }
}
