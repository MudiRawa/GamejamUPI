using UnityEngine;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    /// <summary>
    /// Efek "kelopak mata" menghitam di atas & bawah layar saat pusing.
    /// Menggantikan OnGUI di DizzinessSystem.
    /// Canvas ini memakai sortingOrder lebih rendah supaya berada DI BELAKANG HUD.
    /// </summary>
    public class PanicVignette : MonoBehaviour
    {
        public UITheme theme;
        public Canvas canvas;
        public VertigoShift.Gameplay.Panic.DizzinessSystem dizziness;

        Image kelopakAtas, kelopakBawah;

        static Sprite _gradasiAtas;
        static Sprite _gradasiBawah;

        static Sprite GradasiAtas
        {
            get
            {
                if (_gradasiAtas == null) _gradasiAtas = BuatGradasi(true);
                return _gradasiAtas;
            }
        }

        static Sprite GradasiBawah
        {
            get
            {
                if (_gradasiBawah == null) _gradasiBawah = BuatGradasi(false);
                return _gradasiBawah;
            }
        }

        void Start()
        {
            canvas = UIBind.AmbilCanvas(canvas, "Canvas_Kelopak");
            if (!CobaBind()) BuatUI();
        }

        /// <summary>Dipanggil oleh editor builder untuk membuat UI langsung di scene.</summary>
        public void BangunUI(Canvas c, UITheme t)
        {
            canvas = c;
            theme = t;
            BuatUI();
        }

        bool CobaBind()
        {
            if (canvas == null) return false;

            kelopakAtas = UIBind.Gambar(canvas.transform, "Kelopak_Atas");
            kelopakBawah = UIBind.Gambar(canvas.transform, "Kelopak_Bawah");
            if (kelopakAtas == null || kelopakBawah == null) return false;

            if (kelopakAtas.sprite == null) kelopakAtas.sprite = GradasiAtas;
            if (kelopakBawah.sprite == null) kelopakBawah.sprite = GradasiBawah;
            return true;
        }

        void BuatUI()
        {
            Color hitam = new Color(0f, 0f, 0f, 1f);

            kelopakAtas = UIFactory.BuatImage(canvas.transform, "Kelopak_Atas", GradasiAtas, hitam,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(-10f, 0f), new Vector2(10f, 0f));
            kelopakAtas.raycastTarget = false;

            kelopakBawah = UIFactory.BuatImage(canvas.transform, "Kelopak_Bawah", GradasiBawah, hitam,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(-10f, 0f), new Vector2(10f, 0f));
            kelopakBawah.raycastTarget = false;

            AturTinggi(0f);
        }

        void Update()
        {
            if (dizziness == null) return;

            // Pakai tinggi referensi 1080 supaya sama dengan CanvasScaler
            float tinggi = 1080f * Mathf.Max(0f, dizziness.Sipit) * 2f;
            AturTinggi(tinggi);
        }

        void AturTinggi(float tinggi)
        {
            if (kelopakAtas != null)
            {
                RectTransform rt = kelopakAtas.rectTransform;
                rt.offsetMin = new Vector2(-10f, -tinggi);
                rt.offsetMax = new Vector2(10f, 0f);
                kelopakAtas.enabled = tinggi > 0.01f;
            }

            if (kelopakBawah != null)
            {
                RectTransform rt = kelopakBawah.rectTransform;
                rt.offsetMin = new Vector2(-10f, 0f);
                rt.offsetMax = new Vector2(10f, tinggi);
                kelopakBawah.enabled = tinggi > 0.01f;
            }
        }

        static Sprite BuatGradasi(bool dariAtas)
        {
            const int N = 64;
            var tex = new Texture2D(1, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++)
            {
                float t = y / (float)(N - 1);
                float edar = dariAtas ? t : 1f - t;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.55f, edar));
                tex.SetPixel(0, y, new Color(1f, 1f, 1f, a));
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, N), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
