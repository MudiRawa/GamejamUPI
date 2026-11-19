using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VertigoShift.UI;

// Tools > Vertigo Shift > UI > Pasang Gaya Sci-Fi HUD (Scene Ini)
// buka scene InGame, klik menu ini, terus Ctrl+S.
// gambar-gambarnya ada di Assets/UI/HUD (setting import-nya diatur otomatis di sini)
public static class HUDSciFiBuilder
{
    const string Folder = "Assets/UI/HUD/";

    [MenuItem("Tools/Vertigo Shift/UI/Pasang Gaya Sci-Fi HUD (Scene Ini)")]
    static void Pasang()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode dulu."); return; }

        GameObject go = GameObject.Find("Canvas_HUD");
        Canvas hud = go != null ? go.GetComponent<Canvas>() : null;
        if (hud == null) { Debug.LogWarning("Canvas_HUD nggak ketemu. Buka scene InGame dulu (atau bangun HUD-nya lewat Tools > Vertigo Shift > UI)."); return; }

        HUDSciFi.Gambar g = new HUDSciFi.Gambar
        {
            platKiriAtas = AmbilSprite("HUD_PlatKiriAtas", false),
            platKiriBawah = AmbilSprite("HUD_PlatKiriBawah", false),
            platTengah = AmbilSprite("HUD_PlatTengah", false),
            lekukPusing = AmbilSprite("HUD_Lekuk_Pusing", false),
            lekukDarah = AmbilSprite("HUD_Lekuk_Darah", false),
            segmenPusing = AmbilSprite("HUD_Segmen_Pusing", false),
            segmenDarah = AmbilSprite("HUD_Segmen_Darah", true),
        };
        if (g.platKiriAtas == null || g.segmenDarah == null) { Debug.LogWarning("Gambar HUD nggak ketemu di " + Folder); return; }

        Undo.RegisterFullObjectHierarchyUndo(go, "Gaya Sci-Fi HUD");
        HUDSciFi.Pasang(hud, g);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("HUD sci-fi udah kepasang (Pusing, Wave, Darah). Jangan lupa Ctrl+S.");
    }

    // png -> sprite: tanpa mipmap, tanpa kompres biar tajem, ppu 200 karena gambarnya dibikin 2x
    static Sprite AmbilSprite(string nama, bool diulang)
    {
        string path = Folder + nama + ".png";
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return null;

        TextureImporterSettings st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        bool ubah = ti.textureType != TextureImporterType.Sprite || ti.spritePixelsPerUnit != 200f || ti.mipmapEnabled
            || ti.textureCompression != TextureImporterCompression.Uncompressed || st.spriteMeshType != SpriteMeshType.FullRect
            || ti.wrapMode != (diulang ? TextureWrapMode.Repeat : TextureWrapMode.Clamp);
        if (ubah)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 200f;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.wrapMode = diulang ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect; // wajib buat Image Tiled
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
