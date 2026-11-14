using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VertigoShift.Gameplay.Player;
using VertigoShift.Gameplay.Targets;
using VertigoShift.Gameplay.Weapons;
using VertigoShift.UI;

// Tools > VFX > Pasang VFX ke Scene
// isi kolom VFX di Revolver, PlayerMotor, sama semua musuh di scene
// sekalian isi gambar UI peluru di PrototypeHUD
public static class VFXBuilder
{
    const string Folder = "Assets/VFX/Prefabs/";

    [MenuItem("Tools/VFX/Pasang VFX ke Scene")]
    static void Pasang()
    {
        foreach (Revolver revolver in Object.FindObjectsByType<Revolver>(FindObjectsSortMode.None))
        {
            revolver.vfxTembakanRevolver = Ambil("VFX_TembakanRevolver");
            revolver.vfxPeluruTrail = Ambil("VFX_PeluruTrail");
            revolver.vfxImpactPeluru = Ambil("VFX_ImpactPeluru");
            revolver.vfxPeluruKenaMusuh = Ambil("VFX_PeluruKenaMusuh");
            EditorUtility.SetDirty(revolver);
        }

        foreach (PlayerMotor motor in Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None))
        {
            motor.vfxAngin = Ambil("VFX_Angin");
            motor.vfxSlide = Ambil("VFX_Slide");
            EditorUtility.SetDirty(motor);
        }

        foreach (TargetDummy musuh in Object.FindObjectsByType<TargetDummy>(FindObjectsSortMode.None))
        {
            musuh.vfxMusuhPecah = Ambil("VFX_MusuhPecah");
            EditorUtility.SetDirty(musuh);
        }

        foreach (PrototypeHUD hud in Object.FindObjectsByType<PrototypeHUD>(FindObjectsSortMode.None))
        {
            hud.gambarTabung = AmbilGambar("UI_Tabung");
            hud.gambarPeluru = AmbilGambar("UI_Peluru");
            hud.gambarLubang = AmbilGambar("UI_Lubang");
            hud.fontAngka = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Font/Tektur-900.ttf");
            hud.fontTulisan = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Font/Tektur-700.ttf");
            EditorUtility.SetDirty(hud);
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

    static GameObject Ambil(string nama)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + nama + ".prefab");
        if (prefab == null) Debug.LogWarning(nama + " nggak ketemu di " + Folder);
        return prefab;
    }
}
