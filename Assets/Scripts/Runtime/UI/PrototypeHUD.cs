using UnityEngine;

namespace VertigoShift.UI
{
    /// <summary>
    /// Entry point HUD lama. Sekarang tugasnya hanya meneruskan referensi
    /// ke HUDController berbasis Canvas.
    /// </summary>
    public class PrototypeHUD : MonoBehaviour
    {
        public VertigoShift.Gameplay.Weapons.Revolver revolver;
        public VertigoShift.Gameplay.Gravity.GravityShift gravity;
        public VertigoShift.Gameplay.Panic.DizzinessSystem dizziness;
        public VertigoShift.Gameplay.Player.PlayerMotor motor;

        [Header("Gambar UI Peluru (boleh kosong)")]
        public UnityEngine.Texture2D gambarTabung;
        public UnityEngine.Texture2D gambarPeluru;
        public UnityEngine.Texture2D gambarLubang;

        [Header("Font Sci-fi (boleh kosong)")]
        public UnityEngine.Font fontAngka;
        public UnityEngine.Font fontTulisan;

        void Awake()
        {
            var hud = FindAnyObjectByType<HUDController>();
            if (hud == null)
            {
                var go = new GameObject("HUDController");
                hud = go.AddComponent<HUDController>();
            }

            // Hanya isi kalau ada nilainya, supaya referensi yang sudah
            // dipasang lewat scene / editor builder tidak tertimpa null.
            if (revolver != null) hud.revolver = revolver;
            if (gravity != null) hud.gravity = gravity;
            if (dizziness != null) hud.dizziness = dizziness;
            if (motor != null) hud.motor = motor;
            if (fontAngka != null) hud.fontAngka = fontAngka;
            if (fontTulisan != null) hud.fontTulisan = fontTulisan;

            // Teruskan referensi gambar ke AmmoDisplay jika ada
            var ammo = FindAnyObjectByType<AmmoDisplay>();
            if (ammo != null)
            {
                if (gambarTabung != null && ammo.gambarTabung == null)
                    ammo.gambarTabung = BuatSprite(gambarTabung);
                if (gambarPeluru != null && ammo.gambarPeluru == null)
                    ammo.gambarPeluru = BuatSprite(gambarPeluru);
                if (gambarLubang != null && ammo.gambarLubang == null)
                    ammo.gambarLubang = BuatSprite(gambarLubang);
            }
        }

        static UnityEngine.Sprite BuatSprite(UnityEngine.Texture2D tex)
        {
            return UnityEngine.Sprite.Create(
                tex,
                new UnityEngine.Rect(0, 0, tex.width, tex.height),
                new UnityEngine.Vector2(0.5f, 0.5f));
        }
    }
}
