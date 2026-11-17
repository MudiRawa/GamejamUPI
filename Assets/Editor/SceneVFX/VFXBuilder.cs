using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VertigoShift.Gameplay.Enemies;
using VertigoShift.Gameplay.Player;
using VertigoShift.Gameplay.Targets;
using VertigoShift.Gameplay.Weapons;
using VertigoShift.UI;

// Tools > VFX > Pasang VFX ke Scene
// isi kolom VFX di Revolver, PlayerMotor, sama semua musuh di scene
// sekalian isi gambar UI peluru, font sci-fi di semua HUD, sama pasang darah pemain
public static class VFXBuilder
{
    const string Folder = "Assets/VFX/Prefabs/";

    [MenuItem("Tools/VFX/Pasang VFX ke Scene")]
    static void Pasang()
    {
        foreach (Revolver revolver in Object.FindObjectsByType<Revolver>())
        {
            revolver.vfxTembakanRevolver = Ambil("VFX_TembakanRevolver");
            revolver.vfxPeluruTrail = Ambil("VFX_PeluruTrail");
            revolver.vfxImpactPeluru = Ambil("VFX_ImpactPeluru");
            revolver.vfxPeluruKenaMusuh = Ambil("VFX_PeluruKenaMusuh");
            EditorUtility.SetDirty(revolver);
        }

        foreach (PlayerMotor motor in Object.FindObjectsByType<PlayerMotor>())
        {
            motor.vfxAngin = Ambil("VFX_Angin");
            motor.vfxSlide = Ambil("VFX_Slide");
            EditorUtility.SetDirty(motor);
        }

        foreach (TargetDummy musuh in Object.FindObjectsByType<TargetDummy>())
        {
            musuh.vfxMusuhPecah = Ambil("VFX_MusuhPecah");
            EditorUtility.SetDirty(musuh);
        }

        // Enemy: isi slot VFX tembakan (template) biar efek musuh mudah diganti
        foreach (Enemy musuh in Object.FindObjectsByType<Enemy>())
        {
            musuh.vfxTembakan = Ambil("VFX_TembakanRevolver");
            // peluru musuh: pakai VFX_PeluruMusuh kalau udah dibikin (Tools > VFX > Buat VFX Peluru Musuh)
            musuh.vfxPeluruTrail = AmbilKalauAda("VFX_PeluruMusuh") ?? Ambil("VFX_PeluruTrail");
            musuh.vfxImpact = Ambil("VFX_ImpactPeluru");
            musuh.vfxPeluruKenaPemain = AmbilKalauAda("VFX_PeluruKenaPemain") ?? Ambil("VFX_PeluruKenaMusuh");
            EditorUtility.SetDirty(musuh);
        }

        foreach (PrototypeHUD hud in Object.FindObjectsByType<PrototypeHUD>())
        {
            hud.gambarTabung = AmbilGambar("UI_Tabung");
            hud.gambarPeluru = AmbilGambar("UI_Peluru");
            hud.gambarLubang = AmbilGambar("UI_Lubang");
            hud.fontAngka = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Font/Tektur-900.ttf");
            hud.fontTulisan = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Font/Tektur-700.ttf");
            EditorUtility.SetDirty(hud);
        }

        Font angka = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Font/Tektur-900.ttf");
        Font tulisan = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Font/Tektur-700.ttf");

        foreach (WaveHUD wave in Object.FindObjectsByType<WaveHUD>())
        {
            wave.fontAngka = angka;
            wave.fontTulisan = tulisan;
            EditorUtility.SetDirty(wave);
        }

        // darah pemain: pasang PlayerHealth ke Player kalau belum ada
        foreach (PlayerMotor motor in Object.FindObjectsByType<PlayerMotor>())
        {
            PlayerHealth darah = motor.GetComponent<PlayerHealth>();
            if (darah == null) darah = motor.gameObject.AddComponent<PlayerHealth>();
            darah.fontAngka = angka;
            darah.fontTulisan = tulisan;
            EditorUtility.SetDirty(darah);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("VFX udah kepasang, jangan lupa Ctrl+S.");
    }

    static Texture2D AmbilGambar(string nama)
    {
        Texture2D gambar = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Revolver/" + nama + ".png");
        if (gambar == null) Debug.LogWarning(nama + " nggak ketemu di Assets/UI/Revolver/");
        return gambar;
    }

    static GameObject AmbilKalauAda(string nama)
    {
        GameObject vfx = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + nama + ".prefab");
        return vfx != null ? vfx : null; // biar ?? jalan bener sama objek Unity
    }

    static GameObject Ambil(string nama)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + nama + ".prefab");
        if (prefab == null) Debug.LogWarning(nama + " nggak ketemu di " + Folder);
        return prefab;
    }
}
