using UnityEngine;
using VertigoShift.Gameplay.Player;

namespace VertigoShift.UI
{
    // efek pas pemain kena tembak musuh:
    // pinggir layar merah, panah merah di sekitar crosshair nunjuk arah penembak, kamera geter.
    // nggak perlu dipasang manual, otomatis kebikin waktu PlayerKena.Kena() dipanggil
    public class PlayerKena : MonoBehaviour
    {
        public float lamaMerah = 0.6f;  // berapa detik pinggir layar merah
        public float lamaPanah = 1.2f;  // berapa detik panah arah keliatan
        public float kuatGeter = 0.45f;
        public Color warna = new Color(1f, 0.1f, 0.15f);

        static PlayerKena ini;

        float merah, panah;
        Vector3 dariMana;
        Texture2D gambarPinggir, gambarPanah;
        PlayerLook look;

        public static void Kena(Vector3 dariMana)
        {
            if (ini == null) ini = new GameObject("PlayerKena").AddComponent<PlayerKena>();
            ini.Mulai(dariMana);
        }

        void Mulai(Vector3 asal)
        {
            dariMana = asal;
            merah = 1f;
            panah = 1f;
            if (look == null) look = FindAnyObjectByType<PlayerLook>();
            if (look != null) look.AddShake(kuatGeter);
        }

        void Update()
        {
            merah = Mathf.MoveTowards(merah, 0f, Time.unscaledDeltaTime / lamaMerah);
            panah = Mathf.MoveTowards(panah, 0f, Time.unscaledDeltaTime / lamaPanah);
        }

        void OnGUI()
        {
            if (merah <= 0f && panah <= 0f) return;
            if (gambarPinggir == null) BuatGambar();

            // pinggir layar merah
            GUI.color = new Color(warna.r, warna.g, warna.b, merah * 0.85f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), gambarPinggir);

            // panah di sekitar crosshair, diputer ke arah penembak
            Camera cam = Camera.main;
            if (panah > 0f && cam != null)
            {
                Vector3 lokal = cam.transform.InverseTransformPoint(dariMana);
                float sudut = Mathf.Atan2(lokal.x, lokal.z) * Mathf.Rad2Deg;
                Vector2 tengah = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                float s = Screen.height / 1080f;
                float jarak = 140f * s, lebar = 120f * s, tinggi = 40f * s;

                Matrix4x4 simpan = GUI.matrix;
                GUIUtility.RotateAroundPivot(sudut, tengah);
                GUI.color = new Color(warna.r, warna.g, warna.b, panah);
                GUI.DrawTexture(new Rect(tengah.x - lebar * 0.5f, tengah.y - jarak - tinggi, lebar, tinggi), gambarPanah);
                GUI.matrix = simpan;
            }
            GUI.color = Color.white;
        }

        // gambarnya dibikin dari kode, jadi nggak perlu file PNG
        void BuatGambar()
        {
            // pinggir: tengahnya bening, makin ke pinggir makin merah
            gambarPinggir = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float dx = x / 127f * 2f - 1f, dy = y / 127f * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                    gambarPinggir.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, (r - 0.45f) / 0.55f)));
                }
            gambarPinggir.wrapMode = TextureWrapMode.Clamp;
            gambarPinggir.Apply();

            // panah: lengkungan tipis yang tengahnya paling tebal
            gambarPanah = new Texture2D(96, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 96; x++)
                {
                    float u = x / 95f * 2f - 1f;                 // -1 kiri, 1 kanan
                    float lengkung = 0.35f + 0.45f * u * u;       // pinggirnya agak turun
                    float v = 1f - y / 31f;                       // 0 atas, 1 bawah
                    float tebal = Mathf.Lerp(0.35f, 0.08f, Mathf.Abs(u));
                    float a = Mathf.Clamp01(1f - Mathf.Abs(v - lengkung) / tebal) * Mathf.Clamp01((1f - Mathf.Abs(u)) * 4f);
                    gambarPanah.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            gambarPanah.wrapMode = TextureWrapMode.Clamp;
            gambarPanah.Apply();
        }
    }
}
