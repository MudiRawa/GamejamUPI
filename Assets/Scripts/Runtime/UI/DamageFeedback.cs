using UnityEngine;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    /// <summary>
    /// Efek merah di pinggir layar + panah arah penembak saat player terkena tembakan.
    /// Memakai objek Panel_Merah / Img_Panah yang sudah ada di Canvas scene bila tersedia.
    /// </summary>
    public class DamageFeedback : MonoBehaviour
    {
        public float lamaMerah = 0.6f;
        public float lamaPanah = 1.2f;
        public float kuatGeter = 0.45f;
        public Color warna = new Color(1f, 0.1f, 0.15f);

        [Header("Canvas (kosongkan untuk auto-buat)")]
        public Canvas canvas;

        [Header("Theme")]
        public UITheme theme;

        static DamageFeedback instance;

        Image panelMerah;
        Image imgPanah;
        RectTransform rtPanah;

        float merah, panah;
        Vector3 dariMana;

        void Awake()
        {
            instance = this;

            if (theme == null) theme = Resources.Load<UITheme>("UITheme");
            if (canvas == null)
            {
                GameObject go = GameObject.Find("Canvas_Damage");
                if (go != null) canvas = go.GetComponent<Canvas>();
            }
            if (canvas == null) canvas = UIFactory.BuatCanvas("Canvas_Damage");

            if (!CobaBind()) BuatUI();
            Sembunyikan();
        }

        /// <summary>Dipanggil oleh editor builder untuk membuat UI langsung di scene.</summary>
        public void BangunUI(Canvas c, UITheme t)
        {
            canvas = c;
            theme = t;
            BuatUI();
            Sembunyikan();
        }

        public static void Kena(Vector3 posisiPenembak)
        {
            if (instance == null)
                instance = new GameObject("DamageFeedback").AddComponent<DamageFeedback>();
            instance.Mulai(posisiPenembak);
        }

        bool CobaBind()
        {
            if (canvas == null) return false;

            panelMerah = UIBind.Gambar(canvas.transform, "Panel_Merah");
            imgPanah = UIBind.Gambar(canvas.transform, "Img_Panah");
            if (panelMerah == null || imgPanah == null) return false;

            rtPanah = imgPanah.rectTransform;
            return true;
        }

        void Mulai(Vector3 asal)
        {
            dariMana = asal;
            merah = 1f;
            panah = 1f;

            var look = FindAnyObjectByType<VertigoShift.Gameplay.Player.PlayerLook>();
            if (look != null) look.AddShake(kuatGeter);

            Tampilkan();
        }

        void BuatUI()
        {
            var t = theme;
            Color w = t != null ? t.merah : new Color(1f, 0.1f, 0.15f);

            // Panel merah menutupi seluruh layar (pinggir merah)
            panelMerah = UIFactory.BuatImage(canvas.transform, "Panel_Merah", null, new Color(w.r, w.g, w.b, 0f),
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, 0f));
            panelMerah.raycastTarget = false;

            // Panah di tengah layar
            imgPanah = UIFactory.BuatImage(canvas.transform, "Img_Panah", null, new Color(w.r, w.g, w.b, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-60f, -20f), new Vector2(60f, 20f));
            rtPanah = imgPanah.rectTransform;
            imgPanah.raycastTarget = false;
        }

        void Tampilkan()
        {
            if (panelMerah != null) panelMerah.gameObject.SetActive(true);
            if (imgPanah != null) imgPanah.gameObject.SetActive(true);
        }

        void Sembunyikan()
        {
            if (panelMerah != null) panelMerah.gameObject.SetActive(false);
            if (imgPanah != null) imgPanah.gameObject.SetActive(false);
        }

        void Update()
        {
            if (merah <= 0f && panah <= 0f)
            {
                Sembunyikan();
                return;
            }

            merah = Mathf.MoveTowards(merah, 0f, Time.unscaledDeltaTime / lamaMerah);
            panah = Mathf.MoveTowards(panah, 0f, Time.unscaledDeltaTime / lamaPanah);

            // Pinggir layar merah
            if (panelMerah != null && merah > 0f)
            {
                panelMerah.color = new Color(warna.r, warna.g, warna.b, merah * 0.85f);
            }

            // Panah arah penembak
            if (imgPanah != null && panah > 0f)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 lokal = cam.transform.InverseTransformPoint(dariMana);
                    float sudut = Mathf.Atan2(lokal.x, lokal.z) * Mathf.Rad2Deg;
                    rtPanah.localEulerAngles = new Vector3(0f, 0f, sudut);
                }
                imgPanah.color = new Color(warna.r, warna.g, warna.b, panah);
            }
        }
    }
}
