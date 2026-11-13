using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VertigoShift.Gameplay.Player;
using VertigoShift.Gameplay.Targets;
using VertigoShift.Gameplay.Weapons;

// Tools > VFX > Pasang VFX ke Scene
// isi kolom VFX di Revolver, PlayerMotor, sama semua musuh di scene
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

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("VFX udah kepasang, jangan lupa Ctrl+S.");
    }

    static GameObject Ambil(string nama)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + nama + ".prefab");
        if (prefab == null) Debug.LogWarning(nama + " nggak ketemu di " + Folder);
        return prefab;
    }
}
