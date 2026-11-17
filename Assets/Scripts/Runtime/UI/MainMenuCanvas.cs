using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    /// <summary>
    /// Canvas main menu (uGUI).
    /// Tampilannya sengaja dibuat sama seperti versi IMGUI lama:
    /// kolom di kanan layar, judul miring dengan efek warna pecah (glitch),
    /// tombol teks rata kiri dengan animasi hover, latar grid gelap + garis neon.
    ///
    /// Kalau Canvas beserta panel-panelnya sudah ada di scene (dibuat lewat
    /// menu Tools > Vertigo Shift > UI), script ini hanya memakai objek itu.
    /// Kalau belum ada, UI dibuat otomatis saat runtime.
    /// </summary>
    public class MainMenuCanvas : MonoBehaviour
    {
        [Header("Scene")]
        public string sceneGame = "InGame";

        [Header("Tulisan")]
        public string judul = "VERTIGO SHIFT";
        public string slogan = "LESS IS MORE  //  MORE IS WORSE";
        public string[] kredit =
        {
            "LEAD PROGRAMMER|Nama",
            "PROGRAMMER ARTIST|Nama",
            "3D ARTIST|Nama",
            "VFX PROGRAMMER & SOUND ARTIST|Nama",
            "GAME DESIGNER|Nama",
        };

        [Header("Referensi UI (kosongkan untuk auto-buat)")]
        public Canvas canvasUtama;
        public PanelRef panelUtama;
        public PanelRef panelPengaturan;
        public PanelRef panelKredit;

        [Header("Font (diisi otomatis dari MainMenu wrapper)")]
        public Font fontJudul;
        public Font fontTulisan;

        [Header("Theme")]
        public UITheme theme;

        // ---- Ukuran mengikuti layout IMGUI lama (referensi 1920x1080) ----
        const int UkuranJudul = 100;
        const int UkuranTombol = 46;
        const int UkuranSlogan = 26;
        const int UkuranKecil = 22;

        const float AtasJudul = 170f;
        const float TinggiJudul = 160f;
        const float AtasSlogan = 330f;
        const float TinggiSlogan = 40f;
        const float AtasTombol = 470f;
        const float TinggiTombol = 70f;
        const float JarakTombol = 80f;
        const float LebarTombol = 507f;   // 0.6 x lebar kolom (845)
        const float AtasPanel = 450f;

        // Elemen yang dipakai saat runtime
        Text teksJudul, teksSlogan;
        RectTransform judulKiri, judulKanan;
        Button btnMainkan, btnPengaturan, btnKredit, btnKeluar, btnKembali, btnKembaliKredit;
        Slider sliderVolume, sliderSensitivitas;
        Text txtVolume, txtSensitivitas;
        Image fade;
        MainMenu wrapper;

        readonly List<AnimTombol> animTombol = new List<AnimTombol>();

        float volume = 0.8f;
        float sensitivitas = 1f;

        float glitch;
        float jedaGlitch = 2f;
        float alfaFade = 1f;

        enum Halaman { Utama, Pengaturan, Kredit }
        Halaman halaman = Halaman.Utama;

        void Awake()
        {
            if (theme == null) theme = Resources.Load<UITheme>("UITheme");
            if (theme == null) theme = ScriptableObject.CreateInstance<UITheme>();

            // Terapkan font dari wrapper MainMenu (jika ada) ke theme
            if (fontJudul != null) theme.fontJudul = fontJudul;
            if (fontTulisan != null) theme.fontTulisan = fontTulisan;

            wrapper = GetComponent<MainMenu>();
            if (wrapper == null) wrapper = FindAnyObjectByType<MainMenu>();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            volume = PlayerPrefs.GetFloat("volume", 0.8f);
            sensitivitas = PlayerPrefs.GetFloat("sensitivitas", 1f);
            AudioListener.volume = volume;

            UIBind.PastikanEventSystem();

            SiapkanCanvas();
            Tampilkan(halaman);

            if (fade != null) fade.color = new Color(0f, 0f, 0f, 1f);
            alfaFade = 1f;
        }

        void SiapkanCanvas()
        {
            if (canvasUtama == null)
            {
                GameObject go = GameObject.Find("Canvas_MainMenu");
                if (go != null) canvasUtama = go.GetComponent<Canvas>();
            }

            if (canvasUtama != null && CobaBind()) return;

            if (canvasUtama == null)
                canvasUtama = UIFactory.BuatCanvas("Canvas_MainMenu");

            BuatSemuaPanel();
            CobaBind();
        }

        /// <summary>
        /// Dipanggil oleh editor builder setelah canvas lama dibersihkan,
        /// supaya panel-panel dibuat langsung di dalam scene.
        /// </summary>
        public void BangunUIScene()
        {
            if (canvasUtama == null)
                canvasUtama = UIFactory.BuatCanvas("Canvas_MainMenu");

            BuatSemuaPanel();
        }

        // ---------------------------------------------------------------- Bind scene

        bool CobaBind()
        {
            animTombol.Clear();
            sliderVolume = null;
            sliderSensitivitas = null;

            if (canvasUtama == null) return false;

            RectTransform rootUtama = CariRect(canvasUtama.transform, "Panel_Utama");
            if (rootUtama == null) return false;

            panelUtama = new PanelRef { root = rootUtama };
            panelPengaturan = new PanelRef { root = CariRect(canvasUtama.transform, "Panel_Pengaturan") };
            panelKredit = new PanelRef { root = CariRect(canvasUtama.transform, "Panel_Kredit") };

            fade = CariGambar(canvasUtama.transform, "Fade");

            // --- Judul (3 lapis: kiri, kanan, utama) ---
            Transform rootJudul = Cari(rootUtama, "Judul_Root");
            if (rootJudul != null) rootJudul.localRotation = Quaternion.Euler(0f, 0f, 6f);

            teksJudul = TeksCari(rootUtama, "Judul");
            Text judulKiriTeks = TeksCari(rootUtama, "Judul_Kiri");
            Text judulKananTeks = TeksCari(rootUtama, "Judul_Kanan");
            judulKiri = judulKiriTeks != null ? judulKiriTeks.rectTransform : null;
            judulKanan = judulKananTeks != null ? judulKananTeks.rectTransform : null;

            if (teksJudul != null) teksJudul.text = judul;
            if (judulKiriTeks != null) judulKiriTeks.text = judul;
            if (judulKananTeks != null) judulKananTeks.text = judul;

            teksSlogan = TeksCari(rootUtama, "Slogan");
            if (teksSlogan != null) teksSlogan.text = slogan;

            // --- Panel utama ---
            btnMainkan = TombolCari(rootUtama, "Btn_Mainkan");
            if (btnMainkan != null)
            {
                btnMainkan.onClick.RemoveAllListeners();
                btnMainkan.onClick.AddListener(MasukGame);
            }

            btnPengaturan = TombolCari(rootUtama, "Btn_Pengaturan");
            if (btnPengaturan != null)
            {
                btnPengaturan.onClick.RemoveAllListeners();
                btnPengaturan.onClick.AddListener(() => Tampilkan(Halaman.Pengaturan));
            }

            btnKredit = TombolCari(rootUtama, "Btn_Kredit");
            if (btnKredit != null)
            {
                btnKredit.onClick.RemoveAllListeners();
                btnKredit.onClick.AddListener(() => Tampilkan(Halaman.Kredit));
            }

            btnKeluar = TombolCari(rootUtama, "Btn_Keluar");
            if (btnKeluar != null)
            {
                btnKeluar.onClick.RemoveAllListeners();
                btnKeluar.onClick.AddListener(Keluar);
            }

            PasangAnimasi(btnMainkan);
            PasangAnimasi(btnPengaturan);
            PasangAnimasi(btnKredit);
            PasangAnimasi(btnKeluar);

            // --- Panel pengaturan ---
            if (panelPengaturan.root != null)
            {
                sliderVolume = SliderCari(panelPengaturan.root, "Slider_Volume");
                txtVolume = TeksCari(panelPengaturan.root, "Txt_Volume");
                if (sliderVolume != null)
                {
                    sliderVolume.SetValueWithoutNotify(volume);
                    sliderVolume.onValueChanged.RemoveAllListeners();
                    sliderVolume.onValueChanged.AddListener(AturVolume);
                }
                if (txtVolume != null) txtVolume.text = PersenVolume();

                sliderSensitivitas = SliderCari(panelPengaturan.root, "Slider_Sens");
                txtSensitivitas = TeksCari(panelPengaturan.root, "Txt_Sens");
                if (sliderSensitivitas != null)
                {
                    sliderSensitivitas.SetValueWithoutNotify(sensitivitas);
                    sliderSensitivitas.onValueChanged.RemoveAllListeners();
                    sliderSensitivitas.onValueChanged.AddListener(AturSensitivitas);
                }
                if (txtSensitivitas != null) txtSensitivitas.text = TeksSensitivitas();

                btnKembali = TombolCari(panelPengaturan.root, "Btn_Kembali");
                if (btnKembali != null)
                {
                    btnKembali.onClick.RemoveAllListeners();
                    btnKembali.onClick.AddListener(KembaliKeUtama);
                }
                PasangAnimasi(btnKembali);
            }

            // --- Panel kredit ---
            if (panelKredit.root != null)
            {
                if (kredit != null)
                {
                    for (int i = 0; i < kredit.Length; i++)
                    {
                        string[] parts = kredit[i].Split('|');
                        Text peran = TeksCari(panelKredit.root, "Kredit_" + i + "_Peran");
                        Text nama = TeksCari(panelKredit.root, "Kredit_" + i + "_Nama");
                        if (peran != null) peran.text = parts[0];
                        if (nama != null) nama.text = parts.Length > 1 ? parts[1] : "";
                    }
                }

                btnKembaliKredit = TombolCari(panelKredit.root, "Btn_KembaliKredit");
                if (btnKembaliKredit != null)
                {
                    btnKembaliKredit.onClick.RemoveAllListeners();
                    btnKembaliKredit.onClick.AddListener(KembaliKeUtama);
                }
                PasangAnimasi(btnKembaliKredit);
            }

            return true;
        }

        // ---------------------------------------------------------------- Auto build

        void BuatSemuaPanel()
        {
            if (theme == null) theme = ScriptableObject.CreateInstance<UITheme>();
            if (canvasUtama == null) canvasUtama = UIFactory.BuatCanvas("Canvas_MainMenu");

            Font fJudul = fontJudul != null ? fontJudul : theme.fontJudul;
            Font fTulis = fontTulisan != null ? fontTulisan : theme.fontTulisan;

            // ---------- Latar: gelap + grid bergerak + garis neon ----------
            RectTransform latar = UIFactory.BuatPanel(canvasUtama.transform, "Latar",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.03f, 0.04f, 0.07f, 1f));

            RectTransform kotakGrid = UIFactory.BuatPanel(latar, "Latar_Grid",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // CanvasRenderer harus ada supaya Graphic kustom bisa digambar.
            if (kotakGrid.GetComponent<CanvasRenderer>() == null)
                kotakGrid.gameObject.AddComponent<CanvasRenderer>();
            var grid = kotakGrid.gameObject.AddComponent<MenuLatarGrid>();
            grid.color = Warna(theme.cyan, 0.06f);
            grid.raycastTarget = false;

            UIFactory.BuatImage(latar, "Garis_Magenta", null, Warna(theme.magenta, 0.5f),
                new Vector2(0f, 0f), new Vector2(0.4f, 0f),
                new Vector2(0f, 191f), new Vector2(0f, 194f));

            UIFactory.BuatImage(latar, "Garis_Cyan", null, Warna(theme.cyan, 0.5f),
                new Vector2(0f, 0f), new Vector2(0.28f, 0f),
                new Vector2(0f, 182f), new Vector2(0f, 184f));

            BuatTeksKotak(latar, "Txt_LatarSementara", "[ background sementara ]", fTulis, UkuranKecil,
                new Color(1f, 1f, 1f, 0.25f), 40f, 1020f, 600f, 30f);

            // ---------- Kolom kanan (x 0.5 - 0.94 layar) ----------
            RectTransform kolom = UIFactory.BuatPanel(canvasUtama.transform, "Kolom_Kanan",
                new Vector2(0.5f, 0f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero);

            // ---------- Panel Utama ----------
            panelUtama.root = UIFactory.BuatPanel(kolom, "Panel_Utama",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform rootJudul = BuatBaris(panelUtama.root, "Judul_Root", AtasJudul, TinggiJudul);
            rootJudul.localRotation = Quaternion.Euler(0f, 0f, 6f); // miring dikit

            BuatLapisJudul(rootJudul, "Judul_Kiri", Warna(theme.oranye, 0.85f), fJudul);
            BuatLapisJudul(rootJudul, "Judul_Kanan", Warna(theme.cyan, 0.85f), fJudul);
            teksJudul = BuatLapisJudul(rootJudul, "Judul", theme.magenta, fJudul);

            teksSlogan = BuatTeksBaris(panelUtama.root, "Slogan", slogan, fTulis, UkuranSlogan,
                theme.cyan, AtasSlogan, TinggiSlogan);

            float y = AtasTombol;
            btnMainkan = BuatTombolMenu(panelUtama.root, "Btn_Mainkan", "MAINKAN", fJudul, y);
            y += JarakTombol;
            btnPengaturan = BuatTombolMenu(panelUtama.root, "Btn_Pengaturan", "PENGATURAN", fJudul, y);
            y += JarakTombol;
            btnKredit = BuatTombolMenu(panelUtama.root, "Btn_Kredit", "KREDIT", fJudul, y);
            y += JarakTombol;
            btnKeluar = BuatTombolMenu(panelUtama.root, "Btn_Keluar", "KELUAR", fJudul, y);

            // ---------- Panel Pengaturan ----------
            panelPengaturan.root = UIFactory.BuatPanel(kolom, "Panel_Pengaturan",
                new Vector2(0f, 1f), new Vector2(0.75f, 1f),
                new Vector2(0f, -AtasPanel - 340f), new Vector2(0f, -AtasPanel),
                theme.panelLatar);

            BuatTeksKotak(panelPengaturan.root, "Lbl_Volume", "VOLUME", fTulis, UkuranKecil,
                theme.teksAbu, 30f, 20f, 300f, 40f);

            sliderVolume = BuatSliderMenu(panelPengaturan.root, "Slider_Volume",
                290f, 20f, 280f, 40f, volume, 0f, 1f, theme.magenta, Color.white);

            txtVolume = BuatTeksKotak(panelPengaturan.root, "Txt_Volume", PersenVolume(), fTulis,
                UkuranKecil, Color.white, 595f, 20f, 120f, 40f);

            BuatTeksKotak(panelPengaturan.root, "Lbl_Sens", "SENSITIVITAS", fTulis, UkuranKecil,
                theme.teksAbu, 30f, 90f, 300f, 40f);

            sliderSensitivitas = BuatSliderMenu(panelPengaturan.root, "Slider_Sens",
                290f, 90f, 280f, 40f, sensitivitas, 0.3f, 2f, theme.magenta, Color.white);

            txtSensitivitas = BuatTeksKotak(panelPengaturan.root, "Txt_Sens", TeksSensitivitas(), fTulis,
                UkuranKecil, Color.white, 595f, 90f, 120f, 40f);

            btnKembali = BuatTombolKotak(panelPengaturan.root, "Btn_Kembali", "KEMBALI", fJudul,
                0f, 210f, LebarTombol, TinggiTombol);

            // ---------- Panel Kredit ----------
            int jumlahKredit = kredit != null ? kredit.Length : 0;
            float tinggiKredit = 120f + jumlahKredit * 62f;

            panelKredit.root = UIFactory.BuatPanel(kolom, "Panel_Kredit",
                new Vector2(0f, 1f), new Vector2(0.75f, 1f),
                new Vector2(0f, -AtasPanel - tinggiKredit), new Vector2(0f, -AtasPanel),
                theme.panelLatar);

            for (int i = 0; i < jumlahKredit; i++)
            {
                string[] parts = kredit[i].Split('|');
                float baris = 20f + i * 62f;

                BuatTeksKotak(panelKredit.root, "Kredit_" + i + "_Peran", parts[0], fTulis, UkuranKecil,
                    theme.cyan, 30f, baris, 800f, 26f);

                BuatTeksKotak(panelKredit.root, "Kredit_" + i + "_Nama",
                    parts.Length > 1 ? parts[1] : "", fTulis, UkuranSlogan,
                    Color.white, 30f, baris + 24f, 800f, 34f);
            }

            btnKembaliKredit = BuatTombolKotak(panelKredit.root, "Btn_KembaliKredit", "KEMBALI", fJudul,
                0f, 40f + jumlahKredit * 62f, LebarTombol, TinggiTombol);

            // ---------- Fade (layar hitam saat pindah scene) ----------
            fade = UIFactory.BuatImage(canvasUtama.transform, "Fade", null, new Color(0f, 0f, 0f, 1f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fade.raycastTarget = false;

            panelUtama.root.gameObject.SetActive(true);
            if (panelPengaturan.root != null) panelPengaturan.root.gameObject.SetActive(false);
            if (panelKredit.root != null) panelKredit.root.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- Builder helpers

        static Color Warna(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        static RectTransform BuatBaris(Transform induk, string nama, float yAtas, float tinggi)
        {
            return UIFactory.BuatPanel(induk, nama,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -yAtas - tinggi), new Vector2(0f, -yAtas));
        }

        static RectTransform BuatKotak(Transform induk, string nama, float x, float yAtas, float lebar, float tinggi)
        {
            return UIFactory.BuatPanel(induk, nama,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x, -yAtas - tinggi), new Vector2(x + lebar, -yAtas));
        }

        Text BuatTeksKotak(Transform induk, string nama, string isi, Font font, int ukuran, Color warna,
            float x, float yAtas, float lebar, float tinggi, TextAnchor rata = TextAnchor.MiddleLeft)
        {
            Text t = UIFactory.BuatText(induk, nama, isi, font, ukuran, warna, rata,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x, -yAtas - tinggi), new Vector2(x + lebar, -yAtas));
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        Text BuatTeksBaris(Transform induk, string nama, string isi, Font font, int ukuran, Color warna,
            float yAtas, float tinggi, float kiri = 0f, float kanan = 0f)
        {
            Text t = UIFactory.BuatText(induk, nama, isi, font, ukuran, warna, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(kiri, -yAtas - tinggi), new Vector2(-kanan, -yAtas));
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        Text BuatLapisJudul(Transform induk, string nama, Color warna, Font font)
        {
            Text t = UIFactory.BuatText(induk, nama, judul, font, UkuranJudul, warna,
                TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        Button BuatTombolMenu(Transform induk, string nama, string isi, Font font, float yAtas)
        {
            RectTransform rt = BuatBaris(induk, nama, yAtas, TinggiTombol);
            return RangkaiTombol(rt, isi, font, TinggiTombol);
        }

        Button BuatTombolKotak(Transform induk, string nama, string isi, Font font,
            float x, float yAtas, float lebar, float tinggi)
        {
            RectTransform rt = BuatKotak(induk, nama, x, yAtas, lebar, tinggi);
            return RangkaiTombol(rt, isi, font, tinggi);
        }

        Button RangkaiTombol(RectTransform rt, string isi, Font font, float tinggi)
        {
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);
            bg.raycastTarget = true;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;

            // 3 lapis teks: bayangan kiri (merah), bayangan kanan (cyan), teks utama (putih)
            BuatTeksBaris(rt, "Teks_Kiri", isi, font, UkuranTombol, new Color(1f, 0.2f, 0.25f, 0f),
                0f, tinggi, 30f);
            BuatTeksBaris(rt, "Teks_Kanan", isi, font, UkuranTombol, new Color(0.16f, 0.94f, 1f, 0f),
                0f, tinggi, 30f);
            Text utama = BuatTeksBaris(rt, "Teks", isi, font, UkuranTombol,
                new Color(1f, 1f, 1f, 0.85f), 0f, tinggi, 30f);
            utama.rectTransform.SetAsLastSibling();

            return btn;
        }

        Slider BuatSliderMenu(Transform induk, string nama, float x, float yAtas, float lebar, float tinggi,
            float nilai, float min, float maks, Color isi, Color handle)
        {
            RectTransform rt = BuatKotak(induk, nama, x, yAtas, lebar, tinggi);

            // Area klik transparan seluas kotak slider (biar gampang digeser)
            var sentuh = rt.gameObject.AddComponent<Image>();
            sentuh.color = new Color(0f, 0f, 0f, 0f);
            sentuh.raycastTarget = true;

            var slider = rt.gameObject.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = maks;
            slider.value = nilai;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.None;
            slider.targetGraphic = sentuh;

            // Track tipis di tengah
            UIFactory.BuatImage(rt, "Background", null, new Color(1f, 1f, 1f, 0.15f),
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, -4f), new Vector2(0f, 4f));

            // Area isi bar (ditimpa anchor-nya oleh Slider, jadi perlu induk sendiri)
            RectTransform areaIsi = UIFactory.BuatPanel(rt, "Fill Area",
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, -4f), new Vector2(0f, 4f));

            Image fill = UIFactory.BuatImage(areaIsi, "Fill", null, isi,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            slider.fillRect = fill.rectTransform;

            // Handle putih
            Image gagang = UIFactory.BuatImage(rt, "Handle", null, handle,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(-8f, -12f), new Vector2(8f, 12f));
            slider.handleRect = gagang.rectTransform;

            return slider;
        }

        // ---------------------------------------------------------------- Animasi

        class AnimTombol
        {
            public Text utama, kiri, kanan;
            public Vector2 awal;
            public float hover, target;
        }

        void PasangAnimasi(Button btn)
        {
            if (btn == null) return;

            Text utama = TeksCari(btn.transform, "Teks");
            if (utama == null) return;

            var anim = new AnimTombol
            {
                utama = utama,
                kiri = TeksCari(btn.transform, "Teks_Kiri"),
                kanan = TeksCari(btn.transform, "Teks_Kanan"),
                awal = utama.rectTransform.anchoredPosition,
            };
            animTombol.Add(anim);

            var et = btn.GetComponent<EventTrigger>();
            if (et == null) et = btn.gameObject.AddComponent<EventTrigger>();
            et.triggers.Clear();

            var masuk = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            masuk.callback.AddListener(_ => anim.target = 1f);
            et.triggers.Add(masuk);

            var keluar = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            keluar.callback.AddListener(_ => anim.target = 0f);
            et.triggers.Add(keluar);
        }

        void Animasi()
        {
            float dt = Time.unscaledDeltaTime;

            // Judul: warna pecah + glitch sesekali
            jedaGlitch -= dt;
            if (jedaGlitch <= 0f)
            {
                glitch = 0.15f;
                jedaGlitch = Random.Range(1.5f, 4f);
            }
            glitch = Mathf.MoveTowards(glitch, 0f, dt);

            float geser = glitch > 0f ? Random.Range(-14f, 14f) : 6f;
            if (judulKiri != null) judulKiri.anchoredPosition = new Vector2(-geser, -geser * 0.4f);
            if (judulKanan != null) judulKanan.anchoredPosition = new Vector2(geser, geser * 0.4f);

            // Tombol: pas disorot teksnya geser & warna pecah
            for (int i = animTombol.Count - 1; i >= 0; i--)
            {
                AnimTombol a = animTombol[i];
                if (a.utama == null)
                {
                    animTombol.RemoveAt(i);
                    continue;
                }

                a.hover = Mathf.Lerp(a.hover, a.target, 12f * dt);
                if (a.hover < 0.001f) a.hover = 0f;

                float h = a.hover;
                float pecah = 4f * h;

                a.utama.rectTransform.anchoredPosition = a.awal + new Vector2(30f * h, 0f);
                SetAlfa(a.utama, Mathf.Lerp(0.85f, 1f, h));

                if (a.kiri != null)
                {
                    a.kiri.rectTransform.anchoredPosition =
                        a.awal + new Vector2(30f * h - pecah, -pecah * 0.4f);
                    SetAlfa(a.kiri, 0.85f * h);
                }

                if (a.kanan != null)
                {
                    a.kanan.rectTransform.anchoredPosition =
                        a.awal + new Vector2(30f * h + pecah, pecah * 0.4f);
                    SetAlfa(a.kanan, 0.85f * h);
                }
            }
        }

        static void SetAlfa(Text t, float alfa)
        {
            if (t == null) return;
            Color c = t.color;
            if (Mathf.Abs(c.a - alfa) < 0.001f) return;
            c.a = alfa;
            t.color = c;
        }

        // ---------------------------------------------------------------- Aksi

        void Tampilkan(Halaman h)
        {
            halaman = h;

            if (panelUtama != null && panelUtama.root != null)
                panelUtama.root.gameObject.SetActive(h == Halaman.Utama);
            if (panelPengaturan != null && panelPengaturan.root != null)
                panelPengaturan.root.gameObject.SetActive(h == Halaman.Pengaturan);
            if (panelKredit != null && panelKredit.root != null)
                panelKredit.root.gameObject.SetActive(h == Halaman.Kredit);
        }

        void AturVolume(float v)
        {
            volume = v;
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat("volume", volume);
            if (txtVolume != null) txtVolume.text = PersenVolume();
        }

        void AturSensitivitas(float v)
        {
            sensitivitas = v;
            PlayerPrefs.SetFloat("sensitivitas", sensitivitas);
            if (txtSensitivitas != null) txtSensitivitas.text = TeksSensitivitas();
        }

        string PersenVolume()
        {
            return Mathf.RoundToInt(volume * 100f) + "%";
        }

        string TeksSensitivitas()
        {
            return "x" + sensitivitas.ToString("0.0");
        }

        void KembaliKeUtama()
        {
            PlayerPrefs.Save();
            Tampilkan(Halaman.Utama);
        }

        void MasukGame()
        {
            // Pakai fade dari wrapper MainMenu kalau ada
            if (wrapper != null)
            {
                wrapper.MulaiGame();
                return;
            }

            PlayerPrefs.Save();
            SceneManager.LoadScene(sceneGame);
        }

        void Keluar()
        {
            MainMenu.Keluar();
        }

        void Update()
        {
            Animasi();

            // Fade hitam mengikuti wrapper MainMenu (atau memudar sendiri)
            if (fade != null)
            {
                alfaFade = wrapper != null
                    ? wrapper.Gelap
                    : Mathf.MoveTowards(alfaFade, 0f, Time.unscaledDeltaTime * 1.2f);
                fade.color = new Color(0f, 0f, 0f, alfaFade);
            }

            // ESC untuk kembali ke menu utama
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                if (halaman != Halaman.Utama) Tampilkan(Halaman.Utama);
        }

        // ---------------------------------------------------------------- Helper pencarian

        static Transform Cari(Transform induk, string nama)
        {
            if (induk == null) return null;

            Transform t = induk.Find(nama);
            if (t != null) return t;

            for (int i = 0; i < induk.childCount; i++)
            {
                t = Cari(induk.GetChild(i), nama);
                if (t != null) return t;
            }

            return null;
        }

        static Text TeksCari(Transform induk, string nama)
        {
            Transform t = Cari(induk, nama);
            return t != null ? t.GetComponent<Text>() : null;
        }

        static RectTransform CariRect(Transform induk, string nama)
        {
            return Cari(induk, nama) as RectTransform;
        }

        static Button TombolCari(Transform induk, string nama)
        {
            Transform t = Cari(induk, nama);
            return t != null ? t.GetComponent<Button>() : null;
        }

        static Slider SliderCari(Transform induk, string nama)
        {
            Transform t = Cari(induk, nama);
            return t != null ? t.GetComponent<Slider>() : null;
        }

        static Image CariGambar(Transform induk, string nama)
        {
            Transform t = Cari(induk, nama);
            return t != null ? t.GetComponent<Image>() : null;
        }

        // ---------------------------------------------------------------- Helper struct

        [System.Serializable]
        public class PanelRef
        {
            public RectTransform root;
        }
    }
}
