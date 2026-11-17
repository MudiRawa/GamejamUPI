using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using VertigoShift.Gameplay.Gravity;
using VertigoShift.Gameplay.Panic;
using VertigoShift.Gameplay.Player;
using VertigoShift.Gameplay.Waves;
using VertigoShift.Gameplay.Weapons;
using VertigoShift.UI;

namespace VertigoShift.EditorTools
{
    /// <summary>
    /// Membuat Canvas dan seluruh elemen UI langsung di dalam scene,
    /// supaya bisa dilihat & diubah lewat Editor (bukan hanya dibuat saat play).
    ///
    /// Menu: Tools > Vertigo Shift > UI
    /// </summary>
    public static class CanvasUIBuilder
    {
        const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
        const string InGameScene = "Assets/Scenes/InGame.unity";

        const string ThemePath = "Assets/UI/UITheme.asset";
        const string FontJudulPath = "Assets/UI/Font/Tektur-900.ttf";
        const string FontTulisanPath = "Assets/UI/Font/Tektur-700.ttf";
        const string SpriteTabungPath = "Assets/UI/Revolver/UI_Tabung.png";
        const string SpritePeluruPath = "Assets/UI/Revolver/UI_Peluru.png";
        const string SpriteLubangPath = "Assets/UI/Revolver/UI_Lubang.png";

        // ---------------------------------------------------------------- Menu

        [MenuItem("Tools/Vertigo Shift/UI/Bangun UI Main Menu (Scene Ini)")]
        public static void BangunMainMenuSceneIni()
        {
            if (!CekBisaBangun()) return;
            BangunMainMenuDiScene();
            SimpanSceneAktif();
        }

        [MenuItem("Tools/Vertigo Shift/UI/Bangun UI HUD (Scene Ini)")]
        public static void BangunHUDSceneIni()
        {
            if (!CekBisaBangun()) return;
            BangunHUDDiScene();
            SimpanSceneAktif();
        }

        [MenuItem("Tools/Vertigo Shift/UI/Bangun Semua UI (Main Menu + InGame)")]
        public static void BangunSemuaUI()
        {
            if (!CekBisaBangun()) return;

            SimpanSceneAktif();

            Scene menuScene = EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
            BangunMainMenuDiScene();
            EditorSceneManager.SaveScene(menuScene);

            Scene gameScene = EditorSceneManager.OpenScene(InGameScene, OpenSceneMode.Single);
            BangunHUDDiScene();
            EditorSceneManager.SaveScene(gameScene);

            Debug.Log("[UI] Canvas Main Menu & HUD berhasil dibuat langsung di kedua scene.");
        }

        [MenuItem("Tools/Vertigo Shift/UI/Hapus UI Buatan (Scene Ini)")]
        public static void HapusUI()
        {
            HapusObjek("Canvas_MainMenu");
            HapusObjek("Canvas_HUD");
            HapusObjek("Canvas_Damage");
            HapusObjek("Canvas_Kelopak");
            HapusObjek("Canvas_Kalah");
            SimpanSceneAktif();
        }

        // ---------------------------------------------------------------- Main Menu

        public static void BangunMainMenuDiScene()
        {
            UITheme theme = AmbilTheme();

            MainMenu menu = Object.FindAnyObjectByType<MainMenu>();
            if (menu == null)
            {
                Debug.LogError("[UI] Tidak ada komponen MainMenu di scene ini. Buka scene MainMenu dulu.");
                return;
            }

            HapusObjek("Canvas_MainMenu");

            Canvas canvas = UIFactory.BuatCanvas("Canvas_MainMenu");
            canvas.transform.SetParent(menu.transform, false);

            MainMenuCanvas mc = menu.GetComponent<MainMenuCanvas>();
            if (mc == null) mc = menu.gameObject.AddComponent<MainMenuCanvas>();

            mc.canvasUtama = canvas;
            mc.theme = theme;
            mc.sceneGame = string.IsNullOrEmpty(menu.sceneGame) ? "InGame" : menu.sceneGame;
            mc.judul = menu.judul;
            mc.slogan = menu.slogan;
            mc.kredit = menu.kredit != null ? (string[])menu.kredit.Clone() : new string[0];
            mc.fontJudul = menu.fontJudul;
            mc.fontTulisan = menu.fontTulisan;

            mc.BangunUIScene();

            PastikanEventSystem();

            EditorUtility.SetDirty(mc);
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
            Selection.activeGameObject = canvas.gameObject;

            Debug.Log("[UI] Canvas_MainMenu dibuat langsung di scene.");
        }

        // ---------------------------------------------------------------- HUD

        public static void BangunHUDDiScene()
        {
            UITheme theme = AmbilTheme();

            PrototypeHUD proto = Object.FindAnyObjectByType<PrototypeHUD>();
            GameObject hudRoot;
            if (proto != null)
            {
                hudRoot = proto.gameObject;
            }
            else
            {
                hudRoot = new GameObject("PrototypeHUD");
                proto = hudRoot.AddComponent<PrototypeHUD>();
            }

            HapusObjek("Canvas_HUD");
            HapusObjek("Canvas_Damage");
            HapusObjek("Canvas_Kelopak");
            HapusObjek("Canvas_Kalah");

            Canvas canvasHUD = UIFactory.BuatCanvas("Canvas_HUD");
            canvasHUD.transform.SetParent(hudRoot.transform, false);

            Canvas canvasDamage = UIFactory.BuatCanvas("Canvas_Damage");
            canvasDamage.transform.SetParent(hudRoot.transform, false);
            canvasDamage.sortingOrder = 100; // efek damage selalu di atas HUD

            // Efek kelopak mata pusing: DI BELAKANG HUD
            Canvas canvasKelopak = UIFactory.BuatCanvas("Canvas_Kelopak");
            canvasKelopak.transform.SetParent(hudRoot.transform, false);
            canvasKelopak.sortingOrder = -10;

            // Layar kalah: PALING DEPAN
            Canvas canvasKalah = UIFactory.BuatCanvas("Canvas_Kalah");
            canvasKalah.transform.SetParent(hudRoot.transform, false);
            canvasKalah.sortingOrder = 200;

            HUDController hud = hudRoot.GetComponent<HUDController>();
            if (hud == null) hud = hudRoot.AddComponent<HUDController>();

            hud.canvasHUD = canvasHUD;
            hud.canvasKelopak = canvasKelopak;
            hud.canvasKalah = canvasKalah;
            hud.theme = theme;
            hud.revolver = Object.FindAnyObjectByType<Revolver>();
            hud.gravity = Object.FindAnyObjectByType<GravityShift>();
            hud.dizziness = Object.FindAnyObjectByType<DizzinessSystem>();
            hud.motor = Object.FindAnyObjectByType<PlayerMotor>();
            hud.waveManager = Object.FindAnyObjectByType<EnemyWaveManager>();
            hud.health = Object.FindAnyObjectByType<PlayerHealth>();
            hud.fontAngka = theme.fontAngka;
            hud.fontTulisan = theme.fontTulisan;

            // --- Sub HUD (urutan = urutan render, makin bawah makin di atas) ---
            CrosshairDisplay crosshair = AmbilAtauTambah<CrosshairDisplay>(hudRoot);
            crosshair.BangunUI(canvasHUD, theme);

            GravityDisplay gravity = AmbilAtauTambah<GravityDisplay>(hudRoot);
            gravity.gravity = hud.gravity;
            gravity.BangunUI(canvasHUD, theme);

            DizzinessDisplay dizzy = AmbilAtauTambah<DizzinessDisplay>(hudRoot);
            dizzy.dizziness = hud.dizziness;
            dizzy.BangunUI(canvasHUD, theme);

            AmmoDisplay ammo = AmbilAtauTambah<AmmoDisplay>(hudRoot);
            ammo.revolver = hud.revolver;
            // Pakai gambar asli kalau tersedia (di-import sebagai Sprite)
            Sprite sprTabung = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteTabungPath);
            Sprite sprPeluru = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePeluruPath);
            Sprite sprLubang = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteLubangPath);
            if (sprTabung != null) ammo.gambarTabung = sprTabung;
            if (sprPeluru != null) ammo.gambarPeluru = sprPeluru;
            if (sprLubang != null) ammo.gambarLubang = sprLubang;
            ammo.BangunUI(canvasHUD, theme);

            HelpDisplay help = AmbilAtauTambah<HelpDisplay>(hudRoot);
            help.motor = hud.motor;
            help.BangunUI(canvasHUD, theme);

            WaveAnnouncement wave = AmbilAtauTambah<WaveAnnouncement>(hudRoot);
            wave.waveManager = hud.waveManager;
            wave.BangunUI(canvasHUD, theme);

            DamageFeedback dmg = AmbilAtauTambah<DamageFeedback>(hudRoot);
            dmg.BangunUI(canvasDamage, theme);

            PanicVignette panic = AmbilAtauTambah<PanicVignette>(hudRoot);
            panic.dizziness = hud.dizziness;
            panic.BangunUI(canvasKelopak, theme);

            HealthDisplay healthDisplay = AmbilAtauTambah<HealthDisplay>(hudRoot);
            healthDisplay.health = hud.health;
            healthDisplay.BangunUI(canvasHUD, canvasKalah, theme);

            PastikanEventSystem();

            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(hudRoot.scene);
            Selection.activeGameObject = canvasHUD.gameObject;

            Debug.Log("[UI] Canvas_HUD & Canvas_Damage dibuat langsung di scene.");
        }

        // ---------------------------------------------------------------- Helper

        static bool CekBisaBangun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[UI] Hentikan Play Mode dulu sebelum membangun UI di scene.");
                return false;
            }
            return true;
        }

        static void SimpanSceneAktif()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.isDirty) EditorSceneManager.SaveScene(scene);
        }

        static void HapusObjek(string nama)
        {
            GameObject lama = GameObject.Find(nama);
            if (lama != null)
            {
                Object.DestroyImmediate(lama);
            }
        }

        static T AmbilAtauTambah<T>(GameObject go) where T : Component
        {
            T komponen = go.GetComponent<T>();
            if (komponen == null) komponen = go.AddComponent<T>();
            return komponen;
        }

        static void PastikanEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        static UITheme AmbilTheme()
        {
            UITheme theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null)
            {
                PastikanFolder("Assets/UI");
                theme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            Font fontJudul = AssetDatabase.LoadAssetAtPath<Font>(FontJudulPath);
            Font fontTulisan = AssetDatabase.LoadAssetAtPath<Font>(FontTulisanPath);

            if (fontJudul != null) theme.fontJudul = fontJudul;
            if (fontTulisan != null)
            {
                theme.fontTulisan = fontTulisan;
                if (theme.fontAngka == null) theme.fontAngka = fontTulisan;
            }

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }

        static void PastikanFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string induk = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            string nama = System.IO.Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(induk)) PastikanFolder(induk);
            AssetDatabase.CreateFolder(induk, nama);
        }
    }
}
