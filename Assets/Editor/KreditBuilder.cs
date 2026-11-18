using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VertigoShift.UI;

// Tools > Menu > Isi Kredit
// buka scene MainMenu dulu, klik menu ini, terus Ctrl+S.
// mau ganti nama, nambah orang, atau ngatur ukuran kotaknya? ubah angka2 di bawah, terus klik menunya lagi.
public static class KreditBuilder
{
    // format: "PERAN|Nama"
    static readonly string[] Kredit =
    {
        "GAME DESIGNER|Aghna Naufal Caesaryan Rahmadani",
        "3D ARTIST|Joshua Bryan Alfarico",
        "PROGRAMMER|Muhammad Rafi Adnan Putra Wahyudi",
        "PROGRAMMER|Myron Zhafar Athaullah",
    };

    // ---- atur tampilan di sini
    const float LebarKotak = 1f;      // lebar kotak dibanding kolom menu (1 = selebar kolom, 0.75 = 3/4)
    const float JarakAtas = 36f;      // jarak tulisan pertama dari atas kotak
    const float JarakKiri = 40f;      // jarak tulisan dari kiri kotak
    const float JarakBaris = 84f;     // jarak antar orang
    const float JarakBawah = 36f;     // jarak tombol KEMBALI ke bawah kotak
    const int UkuranPeran = 22;
    const int UkuranNama = 30;

    const float TinggiPeran = 30f, TinggiNama = 40f, LebarTeks = 900f;

    [MenuItem("Tools/Menu/Isi Kredit")]
    static void Isi()
    {
        MainMenu menu = Object.FindAnyObjectByType<MainMenu>();
        MainMenuCanvas kanvas = Object.FindAnyObjectByType<MainMenuCanvas>();
        if (menu == null && kanvas == null) { Debug.LogWarning("Buka scene MainMenu dulu."); return; }

        // 1. isi daftar kredit di script (yang dipakai pas game jalan)
        if (menu != null) { Undo.RecordObject(menu, "Isi Kredit"); menu.kredit = (string[])Kredit.Clone(); EditorUtility.SetDirty(menu); }
        if (kanvas != null) { Undo.RecordObject(kanvas, "Isi Kredit"); kanvas.kredit = (string[])Kredit.Clone(); EditorUtility.SetDirty(kanvas); }

        RectTransform panel = CariPanel();
        if (panel == null) { Debug.LogWarning("Panel_Kredit nggak ketemu di scene, tapi daftar kreditnya udah keisi di script."); return; }

        // 2. kotaknya: lebar diatur, tinggi ngikutin jumlah orang
        Undo.RecordObject(panel, "Isi Kredit");
        panel.anchorMax = new Vector2(LebarKotak, panel.anchorMax.y);

        int barisAda = 0;
        while (panel.Find("Kredit_" + barisAda + "_Peran") != null) barisAda++;
        if (barisAda < Kredit.Length)
            Debug.LogWarning("Baris di panel cuma " + barisAda + ", orangnya " + Kredit.Length + ". Bangun ulang UI menu lewat Tools > Vertigo Shift > UI biar barisnya nambah.");

        // 3. tiap baris: peran (cyan, kecil) di atas, nama (putih, gede) di bawahnya
        for (int i = 0; i < barisAda; i++)
        {
            bool dipakai = i < Kredit.Length;
            string[] parts = dipakai ? Kredit[i].Split('|') : new string[0];
            float atas = JarakAtas + i * JarakBaris;
            AturTeks(panel.Find("Kredit_" + i + "_Peran"), dipakai, dipakai ? parts[0] : "", UkuranPeran, atas, TinggiPeran);
            AturTeks(panel.Find("Kredit_" + i + "_Nama"), dipakai, dipakai && parts.Length > 1 ? parts[1] : "",
                UkuranNama, atas + TinggiPeran - 2f, TinggiNama);
        }

        // 4. tombol KEMBALI di bawah orang terakhir
        int dipakaiSemua = Mathf.Min(barisAda, Kredit.Length);
        float atasTombol = JarakAtas + dipakaiSemua * JarakBaris + 10f;
        float tinggiTombol = 70f;
        RectTransform tombol = panel.Find("Btn_KembaliKredit") as RectTransform;
        if (tombol != null)
        {
            Undo.RecordObject(tombol, "Isi Kredit");
            tinggiTombol = tombol.sizeDelta.y;
            Taruh(tombol, JarakKiri, atasTombol, tombol.sizeDelta.x, tinggiTombol);
        }

        // 5. tinggi kotak pas sampai bawah tombol
        float tinggiKotak = atasTombol + tinggiTombol + JarakBawah;
        panel.offsetMin = new Vector2(panel.offsetMin.x, panel.offsetMax.y - tinggiKotak);

        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        Debug.Log("Kredit udah keisi & kotaknya udah dirapiin. Jangan lupa Ctrl+S.");
    }

    static RectTransform CariPanel()
    {
        // Panel_Kredit biasanya lagi disembunyiin, jadi dicari termasuk yang nonaktif
        foreach (RectTransform r in Resources.FindObjectsOfTypeAll<RectTransform>())
            if (r.name == "Panel_Kredit" && r.gameObject.scene.IsValid()) return r;
        return null;
    }

    // taruh kotak dihitung dari pojok kiri atas panel (sama kayak cara MainMenuCanvas)
    static void Taruh(RectTransform r, float x, float atas, float lebar, float tinggi)
    {
        r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(lebar, tinggi);
        r.anchoredPosition = new Vector2(x + lebar * 0.5f, -atas - tinggi * 0.5f);
    }

    static void AturTeks(Transform t, bool tampil, string isi, int ukuran, float atas, float tinggi)
    {
        if (t == null) return;
        Undo.RecordObject(t.gameObject, "Isi Kredit");
        t.gameObject.SetActive(tampil);
        RectTransform r = (RectTransform)t;
        Undo.RecordObject(r, "Isi Kredit");
        Taruh(r, JarakKiri, atas, LebarTeks, tinggi);

        // komponen Text diubah lewat SerializedObject biar folder Editor nggak perlu referensi UnityEngine.UI
        Component teks = t.GetComponent("Text");
        if (teks == null) return;
        SerializedObject so = new SerializedObject(teks);
        Atur(so, "m_Text", isi);
        Atur(so, "m_FontData.m_FontSize", ukuran);
        Atur(so, "m_FontData.m_VerticalOverflow", 1);    // 1 = boleh keluar kotak (biar font Tektur nggak kepotong)
        Atur(so, "m_FontData.m_HorizontalOverflow", 1);
        so.ApplyModifiedProperties();
    }

    static void Atur(SerializedObject so, string nama, string nilai)
    {
        SerializedProperty p = so.FindProperty(nama);
        if (p != null) p.stringValue = nilai;
    }

    static void Atur(SerializedObject so, string nama, int nilai)
    {
        SerializedProperty p = so.FindProperty(nama);
        if (p != null) p.intValue = nilai;
    }
}
