using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace VertigoShift.UI
{
    public class MainMenu : MonoBehaviour
    {
        [Header("Scene game")]
        public string sceneGame = "InGame";

        [Header("Tulisan (dikirim ke MainMenuCanvas)")]
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

        [Header("Gambar (isi nanti kalau art udah jadi)")]
        public Texture2D gambarLatar;
        public Texture2D gambarJudul;

        [Header("Font")]
        public Font fontJudul;
        public Font fontTulisan;

        [Header("Background / Efek kamera")]
        public float jarakFokus = 1.4f;
        public float goyangKamera = 0.6f;

        Transform kamera;
        Quaternion rotasiAwal;

        float gelap = 1f;
        bool masukGame;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            Camera cam = Camera.main;
            if (cam != null)
            {
                kamera = cam.transform;
                rotasiAwal = kamera.rotation;
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }

            // Post-processing menu (depth of field, bloom, vignette)
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

            // Inisialisasi Canvas UI (gantikan IMGUI OnGUI)
            var menuCanvas = FindAnyObjectByType<MainMenuCanvas>();
            if (menuCanvas == null)
            {
                // Belum ada di scene: buat baru dan isi dari nilai wrapper ini.
                var go = new GameObject("MainMenuCanvas");
                menuCanvas = go.AddComponent<MainMenuCanvas>();
                menuCanvas.sceneGame = sceneGame;
                menuCanvas.judul = judul;
                menuCanvas.slogan = slogan;
                menuCanvas.kredit = kredit;
                menuCanvas.fontJudul = fontJudul;
                menuCanvas.fontTulisan = fontTulisan;
            }
            else if (string.IsNullOrEmpty(menuCanvas.sceneGame))
            {
                // Canvas sudah ada di scene (dibuat lewat editor builder).
                // Biarkan nilai serialized-nya supaya mudah diubah di Inspector.
                menuCanvas.sceneGame = sceneGame;
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float t = Time.unscaledTime;

            if (kamera != null)
                kamera.rotation = rotasiAwal * Quaternion.Euler(Mathf.Sin(t * 0.5f) * goyangKamera, Mathf.Sin(t * 0.33f) * goyangKamera, 0f);

            // fade masuk / keluar (digunakan MainMenuCanvas untuk tombol Mainkan)
            if (masukGame)
            {
                gelap = Mathf.MoveTowards(gelap, 1f, dt * 2.5f);
                if (gelap >= 1f) SceneManager.LoadScene(sceneGame);
            }
            else
            {
                gelap = Mathf.MoveTowards(gelap, 0f, dt * 1.2f);
            }

        }

        // Dipakai MainMenuCanvas untuk menutupi layar dengan fade hitam
        public float Gelap => gelap;

        // Dipanggil oleh MainMenuCanvas saat tombol MAINKAN diklik
        public void MulaiGame()
        {
            masukGame = true;
        }

        public static void Keluar()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // OnGUI lama dihapus — UI sekarang pakai Canvas/uGUI lewat MainMenuCanvas
    }
}
