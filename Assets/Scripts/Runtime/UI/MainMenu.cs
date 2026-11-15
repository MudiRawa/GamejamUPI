using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace VertigoShift.UI
{
    // Main menu. Tulisan sama tombolnya digambar lewat kode di sini.
    // Selama art belum jadi, background & judulnya pakai versi sementara.
    // Kalau art udah ada, tinggal isi gambarLatar / gambarJudul di Inspector.
    // Scene-nya dibikin lewat Tools > Menu > Buat Main Menu
    public class MainMenu : MonoBehaviour
    {
        [Header("Scene game")]
        public string sceneGame = "InGame";

        [Header("Tulisan")]
        public string judul = "VERTIGO SHIFT";
        public string slogan = "LESS IS MORE  //  MORE IS WORSE";
        // format: "PERAN|Nama"
        public string[] kredit =
        {
            "LEAD PROGRAMMER|Nama",
            "PROGRAMMER ARTIST|Nama",
            "3D ARTIST|Nama",
            "VFX PROGRAMMER & SOUND ARTIST|Nama",
            "GAME DESIGNER|Nama",
        };

        [Header("Gambar (isi nanti kalau art udah jadi)")]
        public Texture2D gambarLatar;  // kosong = background sementara (grid gelap)
        public Texture2D gambarJudul;  // kosong = judul pakai tulisan

        [Header("Font (diisi otomatis)")]
        public Font fontJudul;
        public Font fontTulisan;

        [Header("Background")]
        public float jarakFokus = 1.4f;   // jarak revolver dari kamera, sisanya buram
        public float goyangKamera = 0.6f; // kamera goyang pelan biar hidup

        enum Halaman { Utama, Pengaturan, Kredit }
        Halaman halaman = Halaman.Utama;

        Transform kamera;
        Quaternion rotasiAwal;

        float gelap = 1f;       // layar hitam buat fade
        bool masukGame;
        float glitch;
        float jedaGlitch = 2f;
        readonly float[] hover = new float[6];

        float volume;
        float sensitivitas;

        GUIStyle gayaJudul, gayaSlogan, gayaTombol, gayaKecil;
        readonly Color magenta = new Color(1f, 0.15f, 0.7f);
        readonly Color cyan = new Color(0.16f, 0.94f, 1f);
        readonly Color merah = new Color(1f, 0.2f, 0.25f);

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            volume = PlayerPrefs.GetFloat("volume", 0.8f);
            sensitivitas = PlayerPrefs.GetFloat("sensitivitas", 1f);
            AudioListener.volume = volume;

            Camera cam = Camera.main;
            if (cam != null)
            {
                kamera = cam.transform;
                rotasiAwal = kamera.rotation;
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }

            // background buram, yang tajam cuma revolver di depan
            Volume vol = new GameObject("Volume_Menu").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 100;
            VolumeProfile profil = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.sharedProfile = profil;
            DepthOfField blur = profil.Add<DepthOfField>(true);
            blur.mode.Override(DepthOfFieldMode.Gaussian);
            blur.gaussianStart.Override(jarakFokus + 1f);
            blur.gaussianEnd.Override(jarakFokus + 8f);
            blur.gaussianMaxRadius.Override(1.5f);
            Bloom bloom = profil.Add<Bloom>(true);
            bloom.intensity.Override(1.5f);
            bloom.threshold.Override(0.8f);
            Vignette vignette = profil.Add<Vignette>(true);
            vignette.intensity.Override(0.45f);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float t = Time.unscaledTime;

            // kamera goyang pelan kayak lagi napas
            if (kamera != null)
                kamera.rotation = rotasiAwal * Quaternion.Euler(Mathf.Sin(t * 0.5f) * goyangKamera, Mathf.Sin(t * 0.33f) * goyangKamera, 0f);

            // judul sesekali glitch
            jedaGlitch -= dt;
            if (jedaGlitch <= 0f)
            {
                glitch = 0.15f;
                jedaGlitch = Random.Range(1.5f, 4f);
            }
            glitch = Mathf.MoveTowards(glitch, 0f, dt);

            // fade masuk / keluar
            if (masukGame)
            {
                gelap = Mathf.MoveTowards(gelap, 1f, dt * 2.5f);
                if (gelap >= 1f) SceneManager.LoadScene(sceneGame);
            }
            else
            {
                gelap = Mathf.MoveTowards(gelap, 0f, dt * 1.2f);
            }

            // ESC buat balik ke menu utama
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) halaman = Halaman.Utama;
        }

        // ---------------------------------------------------------------- TAMPILAN
        void OnGUI()
        {
            float s = Screen.height / 1080f; // biar ukurannya ikut resolusi
            SiapkanGaya(s);
            GambarLatar(s);

            // kolom kanan, kayak di referensi
            float x = Screen.width * 0.5f;
            float lebar = Screen.width * 0.44f;

            // judul miring dikit + warna pecah
            Matrix4x4 awal = GUI.matrix;
            GUIUtility.RotateAroundPivot(-6f, new Vector2(x + lebar * 0.5f, 250f * s));
            Rect rJudul = new Rect(x, 170f * s, lebar, 160f * s);
            float geser = glitch > 0f ? Random.Range(-14f, 14f) * s : 6f * s;
            if (gambarJudul != null)
            {
                Rect rGeser = new Rect(rJudul.x + (glitch > 0f ? geser : 0f), rJudul.y, rJudul.width, rJudul.height);
                GUI.DrawTexture(rGeser, gambarJudul, ScaleMode.ScaleToFit);
            }
            else TulisPecah(rJudul, judul, gayaJudul, magenta, cyan, geser);
            GUI.matrix = awal;

            GUI.color = cyan;
            GUI.Label(new Rect(x, 330f * s, lebar, 40f * s), slogan, gayaSlogan);

            float y = 470f * s;
            if (halaman == Halaman.Utama) MenuUtama(x, y, lebar, s);
            else if (halaman == Halaman.Pengaturan) MenuPengaturan(x, y, lebar, s);
            else MenuKredit(x, y, lebar, s);

            // layar hitam buat fade
            if (gelap > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, gelap);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }

        void MenuUtama(float x, float y, float lebar, float s)
        {
            if (Tombol(0, x, y, lebar, s, "MAINKAN")) masukGame = true;
            if (Tombol(1, x, y + 80f * s, lebar, s, "PENGATURAN")) halaman = Halaman.Pengaturan;
            if (Tombol(2, x, y + 160f * s, lebar, s, "KREDIT")) halaman = Halaman.Kredit;
            if (Tombol(3, x, y + 240f * s, lebar, s, "KELUAR")) Keluar();
        }

        void MenuPengaturan(float x, float y, float lebar, float s)
        {
            Panel(x, y - 20f * s, lebar, 340f * s);

            float volumeBaru = Penggeser(x + 30f * s, y, s, "VOLUME", volume, 0f, 1f, Mathf.RoundToInt(volume * 100f) + "%");
            if (volumeBaru != volume)
            {
                volume = volumeBaru;
                AudioListener.volume = volume;
                PlayerPrefs.SetFloat("volume", volume);
            }

            float sensBaru = Penggeser(x + 30f * s, y + 70f * s, s, "SENSITIVITAS", sensitivitas, 0.3f, 2f, "x" + sensitivitas.ToString("0.0"));
            if (sensBaru != sensitivitas)
            {
                sensitivitas = sensBaru;
                PlayerPrefs.SetFloat("sensitivitas", sensitivitas);
            }

            if (Tombol(4, x, y + 190f * s, lebar, s, "KEMBALI"))
            {
                PlayerPrefs.Save();
                halaman = Halaman.Utama;
            }
        }

        void MenuKredit(float x, float y, float lebar, float s)
        {
            Panel(x, y - 20f * s, lebar, 120f * s + kredit.Length * 62f * s);

            for (int i = 0; i < kredit.Length; i++)
            {
                string[] isi = kredit[i].Split('|');
                float baris = y + i * 62f * s;
                GUI.color = cyan;
                GUI.Label(new Rect(x + 30f * s, baris, lebar, 26f * s), isi[0], gayaKecil);
                GUI.color = Color.white;
                GUI.Label(new Rect(x + 30f * s, baris + 24f * s, lebar, 34f * s), isi.Length > 1 ? isi[1] : "", gayaSlogan);
            }

            if (Tombol(5, x, y + 20f * s + kredit.Length * 62f * s, lebar, s, "KEMBALI")) halaman = Halaman.Utama;
        }

        // tombol: pas disorot warnanya pecah (merah & cyan) terus geser dikit
        bool Tombol(int id, float x, float y, float lebar, float s, string teks)
        {
            Rect r = new Rect(x, y, lebar * 0.6f, 70f * s);
            bool disorot = r.Contains(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint)
                hover[id] = Mathf.Lerp(hover[id], disorot ? 1f : 0f, 12f * Time.unscaledDeltaTime);
            float h = hover[id];

            Rect rTeks = new Rect(x + 30f * s * h, y, lebar, 70f * s);
            if (h > 0.05f)
            {
                TulisPecah(rTeks, teks, gayaTombol, Color.white, cyan, 4f * s * h, merah);
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.85f);
                GUI.Label(rTeks, teks, gayaTombol);
            }
            GUI.color = Color.white;

            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        // background: gambar dari artist, atau sementara grid gelap yang gerak pelan
        void GambarLatar(float s)
        {
            Rect layar = new Rect(0f, 0f, Screen.width, Screen.height);
            if (gambarLatar != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(layar, gambarLatar, ScaleMode.ScaleAndCrop);
                return;
            }

            GUI.color = new Color(0.03f, 0.04f, 0.07f);
            GUI.DrawTexture(layar, Texture2D.whiteTexture);

            float jarak = 80f * s;
            float jalan = (Time.unscaledTime * 12f * s) % jarak;
            GUI.color = new Color(cyan.r, cyan.g, cyan.b, 0.06f);
            for (float gx = -jalan; gx < Screen.width; gx += jarak)
                GUI.DrawTexture(new Rect(gx, 0f, Mathf.Max(1f, s), Screen.height), Texture2D.whiteTexture);
            for (float gy = jalan; gy < Screen.height; gy += jarak)
                GUI.DrawTexture(new Rect(0f, gy, Screen.width, Mathf.Max(1f, s)), Texture2D.whiteTexture);

            // garis neon tipis di kiri bawah biar nggak kosong banget
            GUI.color = new Color(magenta.r, magenta.g, magenta.b, 0.5f);
            GUI.DrawTexture(new Rect(0f, Screen.height * 0.82f, Screen.width * 0.4f, 3f * s), Texture2D.whiteTexture);
            GUI.color = new Color(cyan.r, cyan.g, cyan.b, 0.5f);
            GUI.DrawTexture(new Rect(0f, Screen.height * 0.82f + 10f * s, Screen.width * 0.28f, 2f * s), Texture2D.whiteTexture);

            GUI.color = new Color(1f, 1f, 1f, 0.25f);
            GUI.Label(new Rect(40f * s, Screen.height - 60f * s, 600f * s, 30f * s), "[ background sementara ]", gayaKecil);
            GUI.color = Color.white;
        }

        // tulisan dengan warna pecah: bayangan geser ke kiri & kanan
        void TulisPecah(Rect r, string teks, GUIStyle gaya, Color utama, Color kanan, float geser, Color? kiri = null)
        {
            Color warnaKiri = kiri ?? new Color(1f, 0.45f, 0.1f); // oranye
            GUI.color = new Color(warnaKiri.r, warnaKiri.g, warnaKiri.b, 0.85f);
            GUI.Label(new Rect(r.x - geser, r.y + geser * 0.4f, r.width, r.height), teks, gaya);
            GUI.color = new Color(kanan.r, kanan.g, kanan.b, 0.85f);
            GUI.Label(new Rect(r.x + geser, r.y - geser * 0.4f, r.width, r.height), teks, gaya);
            GUI.color = utama;
            GUI.Label(r, teks, gaya);
            GUI.color = Color.white;
        }

        // kotak gelap transparan di belakang pengaturan & kredit
        void Panel(float x, float y, float lebar, float tinggi)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(x, y, lebar * 0.75f, tinggi), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // penggeser (slider) buat pengaturan
        float Penggeser(float x, float y, float s, string label, float nilai, float min, float maks, string tampil)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, 300f * s, 40f * s), label, gayaKecil);

            Rect bar = new Rect(x + 260f * s, y + 16f * s, 280f * s, 8f * s);
            float k = Mathf.InverseLerp(min, maks, nilai);
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = magenta;
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * k, bar.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(bar.x + bar.width * k - 8f * s, bar.y - 8f * s, 16f * s, 24f * s), Texture2D.whiteTexture);
            GUI.color = cyan;
            GUI.Label(new Rect(bar.xMax + 25f * s, y, 120f * s, 40f * s), tampil, gayaKecil);
            GUI.color = Color.white;

            // klik atau geser mouse di area bar
            Rect area = new Rect(bar.x - 10f * s, y, bar.width + 20f * s, 40f * s);
            Event e = Event.current;
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && area.Contains(e.mousePosition))
            {
                nilai = Mathf.Lerp(min, maks, Mathf.Clamp01((e.mousePosition.x - bar.x) / bar.width));
                e.Use();
            }
            return nilai;
        }

        void SiapkanGaya(float s)
        {
            if (gayaJudul == null)
            {
                gayaJudul = BuatGaya(fontJudul);
                gayaSlogan = BuatGaya(fontTulisan);
                gayaTombol = BuatGaya(fontJudul);
                gayaKecil = BuatGaya(fontTulisan);
            }
            gayaJudul.fontSize = Mathf.RoundToInt(100f * s);
            gayaSlogan.fontSize = Mathf.RoundToInt(26f * s);
            gayaTombol.fontSize = Mathf.RoundToInt(46f * s);
            gayaKecil.fontSize = Mathf.RoundToInt(22f * s);
        }

        static GUIStyle BuatGaya(Font font)
        {
            GUIStyle gaya = new GUIStyle();
            gaya.font = font;
            gaya.alignment = TextAnchor.MiddleLeft;
            gaya.normal.textColor = Color.white;
            if (font == null) gaya.fontStyle = FontStyle.Bold;
            return gaya;
        }

        void Keluar()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
