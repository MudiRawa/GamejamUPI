using UnityEngine;

namespace VertigoShift.UI
{
    /// <summary>
    /// Pengatur seluruh HUD Canvas: crosshair, gravity, pusing, ammo, help, wave,
    /// efek kelopak mata, dan bar darah + layar kalah.
    /// Kalau komponen HUD sudah ada di scene (dibuat lewat menu Tools), komponen itu
    /// dipakai ulang. Kalau belum, HUDController membuatnya otomatis.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Referensi Gameplay")]
        public VertigoShift.Gameplay.Weapons.Revolver revolver;
        public VertigoShift.Gameplay.Gravity.GravityShift gravity;
        public VertigoShift.Gameplay.Panic.DizzinessSystem dizziness;
        public VertigoShift.Gameplay.Player.PlayerMotor motor;
        public VertigoShift.Gameplay.Waves.EnemyWaveManager waveManager;
        public VertigoShift.Gameplay.Player.PlayerHealth health;

        [Header("Canvas (kosongkan untuk auto-buat)")]
        public Canvas canvasHUD;
        public Canvas canvasKelopak;   // efek pusing (di belakang HUD)
        public Canvas canvasKalah;     // layar kalah (di atas HUD)

        [Header("Font (diisi otomatis dari PrototypeHUD)")]
        public Font fontAngka;
        public Font fontTulisan;

        [Header("Theme")]
        public UITheme theme;

        // Sub-HUD
        CrosshairDisplay crosshair;
        GravityDisplay gravityDisplay;
        DizzinessDisplay dizzinessDisplay;
        AmmoDisplay ammoDisplay;
        HelpDisplay helpDisplay;
        WaveAnnouncement waveAnnouncement;
        PanicVignette panicVignette;
        HealthDisplay healthDisplay;

        void Awake()
        {
            if (theme == null) theme = Resources.Load<UITheme>("UITheme");
            if (theme == null) theme = ScriptableObject.CreateInstance<UITheme>();

            // Terapkan font jika ada
            if (fontAngka != null) theme.fontAngka = fontAngka;
            if (fontTulisan != null) theme.fontTulisan = fontTulisan;

            canvasHUD = AmbilCanvas(canvasHUD, "Canvas_HUD", 0);
            canvasKelopak = AmbilCanvas(canvasKelopak, "Canvas_Kelopak", -10);
            canvasKalah = AmbilCanvas(canvasKalah, "Canvas_Kalah", 200);

            UIBind.PastikanEventSystem();

            // Inisialisasi referensi gameplay kalau kosong
            if (revolver == null) revolver = FindAnyObjectByType<VertigoShift.Gameplay.Weapons.Revolver>();
            if (gravity == null) gravity = FindAnyObjectByType<VertigoShift.Gameplay.Gravity.GravityShift>();
            if (dizziness == null) dizziness = FindAnyObjectByType<VertigoShift.Gameplay.Panic.DizzinessSystem>();
            if (motor == null) motor = FindAnyObjectByType<VertigoShift.Gameplay.Player.PlayerMotor>();
            if (waveManager == null) waveManager = FindAnyObjectByType<VertigoShift.Gameplay.Waves.EnemyWaveManager>();
            if (health == null) health = FindAnyObjectByType<VertigoShift.Gameplay.Player.PlayerHealth>();

            BuatSemuaHUD();
        }

        static Canvas AmbilCanvas(Canvas target, string nama, int sortingOrder)
        {
            if (target == null)
            {
                GameObject go = GameObject.Find(nama);
                if (go != null) target = go.GetComponent<Canvas>();
            }
            if (target == null)
            {
                target = UIFactory.BuatCanvas(nama);
                target.sortingOrder = sortingOrder;
            }
            return target;
        }

        void BuatSemuaHUD()
        {
            var t = theme;

            crosshair = AmbilAtauTambah<CrosshairDisplay>();
            crosshair.theme = t;
            crosshair.canvas = canvasHUD;

            gravityDisplay = AmbilAtauTambah<GravityDisplay>();
            gravityDisplay.theme = t;
            gravityDisplay.canvas = canvasHUD;
            if (gravityDisplay.gravity == null) gravityDisplay.gravity = gravity;

            dizzinessDisplay = AmbilAtauTambah<DizzinessDisplay>();
            dizzinessDisplay.theme = t;
            dizzinessDisplay.canvas = canvasHUD;
            if (dizzinessDisplay.dizziness == null) dizzinessDisplay.dizziness = dizziness;

            ammoDisplay = AmbilAtauTambah<AmmoDisplay>();
            ammoDisplay.theme = t;
            ammoDisplay.canvas = canvasHUD;
            if (ammoDisplay.revolver == null) ammoDisplay.revolver = revolver;

            helpDisplay = AmbilAtauTambah<HelpDisplay>();
            helpDisplay.theme = t;
            helpDisplay.canvas = canvasHUD;
            if (helpDisplay.motor == null) helpDisplay.motor = motor;

            waveAnnouncement = AmbilAtauTambah<WaveAnnouncement>();
            waveAnnouncement.theme = t;
            waveAnnouncement.canvas = canvasHUD;
            if (waveAnnouncement.waveManager == null) waveAnnouncement.waveManager = waveManager;

            panicVignette = AmbilAtauTambah<PanicVignette>();
            panicVignette.theme = t;
            panicVignette.canvas = canvasKelopak;
            if (panicVignette.dizziness == null) panicVignette.dizziness = dizziness;

            healthDisplay = AmbilAtauTambah<HealthDisplay>();
            healthDisplay.theme = t;
            healthDisplay.canvas = canvasHUD;
            healthDisplay.canvasKalah = canvasKalah;
            if (healthDisplay.health == null) healthDisplay.health = health;
        }

        T AmbilAtauTambah<T>() where T : Component
        {
            T komponen = GetComponent<T>();
            if (komponen == null) komponen = gameObject.AddComponent<T>();
            return komponen;
        }

        void Update()
        {
            // F1 untuk toggle help + stats (sama seperti HUD lama)
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame && helpDisplay != null)
            {
                helpDisplay.SetHelpAktif(!helpDisplay.helpAktif);
            }
        }
    }
}
