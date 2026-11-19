using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Arena > Susun Arena      : nyusun model dari 3D artist ke arena (acak tapi rapi, ikut grid 2 m)
// Tools > Arena > Acak Ulang       : susunan acak yang baru
// Tools > Arena > Hapus Susunan    : balikin arena kayak semula
// (box udah dikurangin, plus pipa hiasan di dinding depan & belakang: tanpa collider)
//
// Model FBX taruh di Assets/Models/Arena, tekstur di Assets/Models/Arena/Textures.
// Jalankan di scene InGame (yang ada Arena, Targets, sama Player dari lead).
public static class ArenaBuilder
{
    // ================= ATUR DI SINI =================
    static readonly bool PakaiUbin = true;        // ubin di lantai, plafon, dinding kiri & kanan
    static readonly bool PakaiDinding = true;     // panel Wall / Wall 2 di dinding depan & belakang
    static readonly bool PakaiPondasi = true;     // Foundation Wall keliling di bawah & atas semua dinding + pojok pakai Wall L & Foundation L
    static readonly bool PakaiObstacle = true;    // Obstacle L + atap Frame Glass
    static readonly bool PakaiPeti = true;        // tumpukan Cube 1 & 2
    static readonly bool PakaiPipa = true;        // pipa hiasan di dinding depan & belakang (tanpa collider, cuma tempelan)
    static readonly bool SembunyikanLantaiLead = false; // true = lantai/dinding kotak bawaan lead disembunyiin
    static readonly float SkalaUbin = 2f;     // 1 = ubin 2x2 m, 2 = ubin 4x4 m (lebih ringan), ketebalan nggak ikut
    static readonly float SkalaDinding = 1f;  // panel dinding depan & belakang: 1 = 2x2 m kayak di Blender, 2 = 4x4 m
    // biar warnanya mirip di Blender: lampu warna-warni dikecilin, cahaya putih & ambient netral digedein
    static readonly float KekuatanLampu = 0.25f;   // lampu warna-warni dari lead dikaliin segini (1 = asli)
    static readonly float KekuatanMatahari = 1.6f; // Key Light (cahaya putih dari atas)
    static readonly float AmbientNetral = 0.35f;   // cahaya abu-abu rata dari segala arah (0 = pakai ambient biru gelap bawaan lead)
    static readonly float KilapTekstur = 0.7f;     // 1 = kilap penuh dari tekstur, kecilin kalau terlalu kayak cermin
    static readonly float KekuatanNeon = 1f;       // nyala pola biru di tekstur (dulu 2)
    static readonly float KecerahanTekstur = 1f;   // 1 = warna tekstur asli, kecilin biar lebih gelap
    static readonly bool PakaiTonemapping = true;  // biar cahaya terang nggak langsung jadi putih polos
    static readonly bool GabungMesh = true;    // ubin, panel dinding, pondasi & box digabung jadi 1 mesh per sisi (jauh lebih ringan). false = tiap potongan bisa diklik
    // posisinya acak tapi rapi: ikut grid, putarannya kelipatan 90, yang di pinggir punggungnya nempel tembok
    const int ObstacleNempelTembok = 3;  // Obstacle L yang nempel tembok, per permukaan
    const int ObstacleDiTengah = 2;      // Obstacle L di tengah, per permukaan
    const int TumpukanPeti = 3;          // tumpukan box, per permukaan (lantai, plafon, dinding kiri & kanan)
    const int TumpukanPetiDepanBelakang = 1; // tumpukan box di dinding depan & belakang
    const float PeluangPetiDiPinggir = 0.7f; // seberapa sering tumpukan box nempel tembok (sisanya di tengah)
    const float UkuranPetiMin = 1.5f, UkuranPetiMax = 2.3f; // ukuran box (aslinya 1 m)
    static readonly bool PakaiLantaiL = true; // 4 Floor L ngelilingin tengah tiap permukaan
    const int BingkaiWall2 = 0;          // berapa baris paling pinggir pakai Wall 2 (jadi bingkai), tengahnya Wall. 0 = Wall semua
    const float PanjangPondasi = 4f;     // panjang 1 potong pondasi (aslinya 2 m, dipanjangin biar ringan)
    // pipa di dinding depan & belakang (ukuran dalam meter, 0 = tengah dinding / lantai)
    const float PipaPinggir = 14f;       // pipa tegak kiri & kanan, jaraknya dari tengah dinding
    const float PipaBawah = 10f;         // tinggi pipa mendatar bawah
    const float PipaAtas = 34f;          // tinggi pipa mendatar atas
    // =================================================

    const string FolderModel = "Assets/Models/Arena";
    const string FolderTekstur = "Assets/Models/Arena/Textures";
    const string NamaInduk = "Arena_Kit";
    const float H = 20f;      // setengah lebar arena (sama kayak scene builder lead)
    const float Sel = 2f;     // ukuran 1 kotak grid
    const int N = 20;         // jumlah kotak per sisi (40 m / 2 m)
    const float TinggiBaut = 0.04f;
    const float TebalPanelWall = 0f;    // panel udah nggak di belakang pondasi, jadi pondasi nempel tembok langsung
    const float LebarPojok = 1.1f;      // ukuran Wall L / Foundation Wall L
    const float TinggiPondasi = 1f;
    const float TebalPondasi = 1.2f;    // pojok Wall L nonjol ~1.1 m dari dinding, obstacle jaga jarak segini // ubin punya baut yang nonjol 3 cm di atas permukaannya

    // lantai, plafon, dinding bawaan lead: disembunyiin pas arena disusun (collider-nya tetep dipakai)
    static readonly string[] KulitArena = { "Floor", "Ceiling", "Wall_Left", "Wall_Right", "Wall_Front", "Wall_Back" };

    static System.Random acak;
    static Dictionary<string, GameObject> model;
    static Material bahan, bahanKaca;
    static string modelHilang;

    // permukaan yang bisa diinjek (gravity shift): titik tengah + arah "atas"-nya
    struct Permukaan
    {
        public string nama;
        public Vector3 pusat;
        public Quaternion arah;
        public bool bisaDiinjek; // false = dinding depan/belakang: nggak pakai ubin (udah ada panel Wall)
        public float alas;       // tinggi alas benda dari permukaan (panel Wall depan/belakang tebelnya 11 cm)
        public Permukaan(string nama, Vector3 pusat, Vector3 atas, bool bisaDiinjek = true, float alas = 0f)
        {
            this.nama = nama;
            this.pusat = pusat;
            this.bisaDiinjek = bisaDiinjek;
            this.alas = alas;
            arah = Quaternion.LookRotation(Mathf.Abs(atas.z) > 0.5f ? Vector3.up : Vector3.forward, atas);
        }
        public Vector3 KeDunia(float u, float tinggi, float v) => pusat + arah * new Vector3(u, tinggi, v);
        public Vector3 KeLokal(Vector3 dunia) => Quaternion.Inverse(arah) * (dunia - pusat);
    }

    static readonly Permukaan[] semuaPermukaan =
    {
        new Permukaan("Lantai", Vector3.zero, Vector3.up),
        new Permukaan("Plafon", new Vector3(0f, 2f * H, 0f), Vector3.down),
        new Permukaan("DindingKiri", new Vector3(-H, H, 0f), Vector3.right),
        new Permukaan("DindingKanan", new Vector3(H, H, 0f), Vector3.left),
        // depan & belakang juga dipakai: isinya obstacle & box, alasnya di atas panel Wall (tebel 11 cm)
        new Permukaan("DindingDepan_Isi", new Vector3(0f, H, -H), Vector3.forward, false, 0.11f),
        new Permukaan("DindingBelakang_Isi", new Vector3(0f, H, H), Vector3.back, false, 0.11f),
    };

    // ---------------------------------------------------------------- MENU
    [MenuItem("Tools/Arena/Susun Arena")]
    static void Susun() => Bangun(EditorPrefs.GetInt("ArenaSeed", 1));

    [MenuItem("Tools/Arena/Acak Ulang")]
    static void AcakUlang()
    {
        int seed = EditorPrefs.GetInt("ArenaSeed", 1) + 1;
        EditorPrefs.SetInt("ArenaSeed", seed);
        Bangun(seed);
    }

    [MenuItem("Tools/Arena/Hapus Susunan")]
    static void Hapus()
    {
        GameObject lama = GameObject.Find(NamaInduk);
        if (lama != null) Undo.DestroyObjectImmediate(lama);
        TampilkanKulitArena(true);
        AturCahaya(1f, 1f, 0f); // lampu balik ke kekuatan asli
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    static void Bangun(int seed)
    {
        acak = new System.Random(seed);
        modelHilang = "";
        jumlahPondasi = 0;
        MuatModel();
        if (model.Count == 0)
        {
            EditorUtility.DisplayDialog("Susun Arena", "Nggak ada FBX di " + FolderModel + ".\nMasukin dulu model dari 3D artist ke folder itu.", "OK");
            return;
        }
        SiapkanBahan();

        Hapus();
        Transform induk = new GameObject(NamaInduk).transform;
        Undo.RegisterCreatedObjectUndo(induk.gameObject, "Susun Arena");

        foreach (Permukaan p in semuaPermukaan)
        {
            Transform grup = new GameObject(p.nama).transform;
            grup.SetParent(induk, false);
            bool[,] terpakai = KotakTerhalang(p);
            if (PakaiPipa) TandaiJalurPipa(p, terpakai);
            if (PakaiObstacle) SusunObstacle(p, grup, terpakai);
            if (PakaiPeti)
            {
                Transform peti = new GameObject("Box").transform;
                peti.SetParent(grup, false);
                SusunPeti(p, peti, terpakai);
                if (GabungMesh) Gabungkan(peti, p.nama + "_Box");
            }
            if (PakaiUbin && p.bisaDiinjek)
            {
                Transform ubin = new GameObject("Ubin").transform;
                ubin.SetParent(grup, false);
                SusunUbin(p, ubin);
                if (GabungMesh) Gabungkan(ubin, p.nama + "_Ubin");
            }
        }
        if (PakaiPipa)
        {
            Transform pipaDepan = SusunPipaDinding(induk, -1f, "PipaDepan");
            Transform pipaBelakang = SusunPipaDinding(induk, 1f, "PipaBelakang");
            if (GabungMesh)
            {
                Gabungkan(pipaDepan, "PipaDepan");
                Gabungkan(pipaBelakang, "PipaBelakang");
            }
        }
        if (PakaiDinding)
        {
            Transform depan = SusunDinding(induk, new Vector3(0f, H, -H), Vector3.forward, "DindingDepan");
            Transform belakang = SusunDinding(induk, new Vector3(0f, H, H), Vector3.back, "DindingBelakang");
            if (GabungMesh)
            {
                Gabungkan(depan, "DindingDepan");
                Gabungkan(belakang, "DindingBelakang");
            }
        }

        if (PakaiPondasi)
        {
            Transform pondasi = new GameObject("Pondasi").transform;
            pondasi.SetParent(induk, false);
            SusunPondasi(pondasi);
            if (GabungMesh) Gabungkan(pondasi, "Pondasi");
        }

        if (SembunyikanLantaiLead) TampilkanKulitArena(false);
        AturCahaya(KekuatanLampu, KekuatanMatahari, AmbientNetral);
        if (PakaiTonemapping) PasangTonemapping();

        // biar ringan: semua dianggap diam (static batching)
        foreach (Transform t in induk.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        string pesan = "Arena udah disusun (acakan ke-" + seed + "), pondasi " + jumlahPondasi + " potong. Simpan pakai Ctrl+S.";
        if (modelHilang.Length > 0) pesan += "\n\nModel yang nggak ketemu (dilewati):\n" + modelHilang;
        Debug.Log(pesan);
        if (modelHilang.Length > 0) EditorUtility.DisplayDialog("Susun Arena", pesan, "OK");
    }

    // ---------------------------------------------------------------- CAHAYA
    // lampu dari scene builder lead + kekuatan aslinya. dikali dari angka asli, jadi diklik berkali-kali nggak makin redup
    static readonly Dictionary<string, float> LampuAsli = new Dictionary<string, float>
    {
        { "Light_Cyan", 260f }, { "Light_Magenta", 140f }, { "Light_Amber", 120f }, { "Light_Cyan_Back", 120f },
    };

    static void AturCahaya(float lampu, float matahari, float ambient)
    {
        if (ambient > 0f)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(ambient, ambient, ambient);
        }
        else
        {
            // balikin ambient bawaan lead
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.09f, 0.12f, 0.19f);
            RenderSettings.ambientEquatorColor = new Color(0.05f, 0.06f, 0.09f);
            RenderSettings.ambientGroundColor = new Color(0.03f, 0.03f, 0.04f);
        }

        foreach (var l in LampuAsli)
        {
            GameObject go = GameObject.Find(l.Key);
            if (go != null && go.TryGetComponent(out Light li)) { Undo.RecordObject(li, "Atur Cahaya"); li.intensity = l.Value * lampu; }
        }
        GameObject kunci = GameObject.Find("Key Light");
        if (kunci != null && kunci.TryGetComponent(out Light k)) { Undo.RecordObject(k, "Atur Cahaya"); k.intensity = 1.3f * matahari; }
    }

    // tambah Tonemapping Neutral ke Global Volume (ACES bikin lebih gelap)
    static void PasangTonemapping()
    {
        GameObject go = GameObject.Find("Global Volume");
        if (go == null || !go.TryGetComponent(out UnityEngine.Rendering.Volume vol) || vol.sharedProfile == null) return;
        var profil = vol.sharedProfile;
        if (!profil.TryGet(out UnityEngine.Rendering.Universal.Tonemapping tm))
            tm = profil.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
        tm.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.Neutral);
        tm.active = true;
        EditorUtility.SetDirty(profil);
    }

    static void TampilkanKulitArena(bool tampil)
    {
        GameObject arena = GameObject.Find("Arena");
        if (arena == null) return;
        foreach (string nama in KulitArena)
        {
            Transform t = arena.transform.Find(nama);
            Renderer r = t != null ? t.GetComponent<Renderer>() : null;
            if (r == null || r.enabled == tampil) continue;
            Undo.RecordObject(r, "Kulit Arena");
            r.enabled = tampil;
        }
    }

    // ---------------------------------------------------------------- MODEL & BAHAN
    // nama file dibikin sederhana: "Cube 1.fbx" -> "cube1", "Wall .fbx" -> "wall"
    static void MuatModel()
    {
        model = new Dictionary<string, GameObject>();
        if (!AssetDatabase.IsValidFolder(FolderModel)) return;
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { FolderModel }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string kunci = Path.GetFileNameWithoutExtension(path).ToLower().Replace(" ", "").Replace("_", "");

            // jangan bikin material/tekstur dobel dari FBX, sama hilangin rotasi -90 dari Blender
            ModelImporter imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp != null && (imp.materialImportMode != ModelImporterMaterialImportMode.None || !imp.bakeAxisConversion || !imp.isReadable))
            {
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.bakeAxisConversion = true;
                imp.isReadable = true; // biar mesh-nya bisa digabung
                imp.SaveAndReimport();
            }
            model[kunci] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
    }

    static GameObject Model(string kunci)
    {
        if (model.TryGetValue(kunci, out GameObject go)) return go;
        if (!modelHilang.Contains(kunci)) modelHilang += "- " + kunci + "\n";
        return null;
    }

    static void SiapkanBahan()
    {
        AturTekstur("TrimSheet_Normal.png", true, true);
        AturTekstur("TrimSheet_MetallicSmoothness.png", false, false);
        bahan = BuatBahan("M_TrimSheet", "TrimSheet_BaseColor.png", false);
        bahanKaca = BuatBahan("M_TrimSheet_Glass", "TrimSheet_BaseColorAlpha.png", true);
    }

    static void AturTekstur(string nama, bool normal, bool srgb)
    {
        string path = PathTekstur(nama);
        if (path == null) return;
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        TextureImporterType tipe = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (ti.textureType != tipe || ti.sRGBTexture != srgb)
        {
            ti.textureType = tipe;
            ti.sRGBTexture = srgb;
            ti.SaveAndReimport();
        }
    }

    // cari tekstur di Assets/Models/Arena/Textures, kalau nggak ada cari di seluruh proyek
    static string PathTekstur(string nama)
    {
        string path = FolderTekstur + "/" + nama;
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null) return path;
        foreach (string guid in AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(nama) + " t:Texture2D"))
        {
            string ketemu = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(ketemu) == nama) return ketemu;
        }
        if (!modelHilang.Contains(nama)) modelHilang += "- tekstur " + nama + "\n";
        return null;
    }

    static Texture2D Tekstur(string nama)
    {
        string path = PathTekstur(nama);
        return path != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(path) : null;
    }

    // satu material URP buat semua model (kaca dapet versi transparan)
    static Material BuatBahan(string nama, string teksturDasar, bool kaca)
    {
        string path = FolderModel + "/" + nama + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool baru = m == null;
        if (baru) m = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        m.SetTexture("_BaseMap", Tekstur(teksturDasar));
        m.SetColor("_BaseColor", new Color(KecerahanTekstur, KecerahanTekstur, KecerahanTekstur, 1f));
        m.SetTexture("_BumpMap", Tekstur("TrimSheet_Normal.png"));
        m.EnableKeyword("_NORMALMAP");
        m.SetTexture("_MetallicGlossMap", Tekstur("TrimSheet_MetallicSmoothness.png"));
        m.EnableKeyword("_METALLICSPECGLOSSMAP");
        m.SetFloat("_Smoothness", KilapTekstur);
        Texture2D emisi = Tekstur("TrimSheet_Emission.png");
        if (emisi != null)
        {
            m.SetTexture("_EmissionMap", emisi);
            m.SetColor("_EmissionColor", Color.white * KekuatanNeon); // pola biru di trim sheet nyala
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            // tanpa tekstur emisi, jangan nyalain apa-apa (kalau nyala, semuanya jadi putih)
            m.SetColor("_EmissionColor", Color.black);
            m.DisableKeyword("_EMISSION");
        }

        if (kaca)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f); // kelihatan dari dua sisi, jadi nggak bolong walau kebalik
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        if (baru) AssetDatabase.CreateAsset(m, path);
        else EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------------------------------------------------------- KOTAK YANG NGGAK BOLEH DIISI
    // cover, platform, target, sama tempat muncul pemain jangan ketiban barang
    static bool[,] KotakTerhalang(Permukaan p)
    {
        bool[,] terhalang = new bool[N, N];
        List<Bounds> benda = new List<Bounds>();

        GameObject arena = GameObject.Find("Arena");
        if (arena != null)
            foreach (Renderer r in arena.GetComponentsInChildren<Renderer>())
                if (r.name.StartsWith("Cover") || r.name.StartsWith("Plat")) benda.Add(r.bounds);

        GameObject target = GameObject.Find("Targets");
        if (target != null)
            foreach (Renderer r in target.GetComponentsInChildren<Renderer>()) benda.Add(r.bounds);

        GameObject pemain = GameObject.Find("Player");
        if (pemain != null) benda.Add(new Bounds(pemain.transform.position, Vector3.one * 6f));

        foreach (Bounds b in benda)
        {
            // ubah ke koordinat permukaan, cek deket permukaan atau nggak
            Vector3 min = Vector3.one * 999f, max = -Vector3.one * 999f;
            for (int k = 0; k < 8; k++)
            {
                Vector3 sudut = new Vector3((k & 1) == 0 ? b.min.x : b.max.x, (k & 2) == 0 ? b.min.y : b.max.y, (k & 4) == 0 ? b.min.z : b.max.z);
                Vector3 l = p.KeLokal(sudut);
                min = Vector3.Min(min, l);
                max = Vector3.Max(max, l);
            }
            if (min.y > 5f || max.y < -0.5f) continue; // jauh dari permukaan ini

            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                {
                    float u = -H + Sel * (i + 0.5f), v = -H + Sel * (j + 0.5f);
                    if (u > min.x - 2f && u < max.x + 2f && v > min.z - 2f && v < max.z + 2f) terhalang[i, j] = true;
                }
        }
        return terhalang;
    }

    // ---------------------------------------------------------------- OBSTACLE L
    static void SusunObstacle(Permukaan p, Transform grup, bool[,] terpakai)
    {
        // nempel tembok: pinggir mana aja (kiri/kanan/depan/belakang), atapnya ngadep ke dalem
        // dinding depan/belakang cuma di tengah, biar nggak ada obstacle "rebahan" mepet di atas lantai
        int dipasang = 0;
        int jumlahPinggir = p.bisaDiinjek ? ObstacleNempelTembok : 0;
        for (int coba = 0; coba < 60 && dipasang < jumlahPinggir; coba++)
        {
            TitikPinggir(acak.Next(4), 3 + acak.Next(N - 6), out Vector2 titik, out Vector3 kedalam, out Vector3 sepanjang);
            if (!Kosong(terpakai, SelDari(titik.x), SelDari(titik.y), 1)) continue;

            Quaternion rot = p.arah * Quaternion.LookRotation(kedalam, Vector3.up);
            Transform m = Pasang("obstaclel", grup, p.KeDunia(titik.x, 0f, titik.y), rot, bahan, out Transform slot);
            if (m == null) return;
            Arahkan(m, slot, 1, 2);                 // tinggi ke atas, tipis ke depan
            if (SisiAtap(m, slot) < 0) m.RotateAround(slot.position, slot.up, 180f); // atap ke dalem
            Ratakan(m, slot, Rata.Belakang, p.alas); // punggung nempel tembok
            KasihCollider(m, false);
            PasangKaca(m, slot);
            Tandai(terpakai, SelDari(titik.x), SelDari(titik.y), 1);
            dipasang++;
        }

        // di tengah: ikut grid, ngadep ke salah satu dari 4 arah
        dipasang = 0;
        for (int coba = 0; coba < 60 && dipasang < ObstacleDiTengah; coba++)
        {
            int i = 4 + acak.Next(N - 8), j = 4 + acak.Next(N - 8);
            if (!Kosong(terpakai, i, j, 2)) continue;

            Vector3[] empatArah = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            Quaternion rot = p.arah * Quaternion.LookRotation(empatArah[acak.Next(4)], Vector3.up);
            Transform m = Pasang("obstaclel", grup, p.KeDunia(TengahSel(i), 0f, TengahSel(j)), rot, bahan, out Transform slot);
            if (m == null) return;
            Arahkan(m, slot, 1, 2);
            if (SisiAtap(m, slot) < 0) m.RotateAround(slot.position, slot.up, 180f);
            Ratakan(m, slot, Rata.Bawah, p.alas);
            KasihCollider(m, false);
            PasangKaca(m, slot);
            Tandai(terpakai, i, j, 2);
            dipasang++;
        }
    }

    // titik di pinggir permukaan. sisi: 0 = v kecil, 1 = v besar, 2 = u kecil, 3 = u besar.
    // sel = posisi di sepanjang pinggir. udah dikasih jarak dari pondasi / pojok
    static void TitikPinggir(int sisi, int sel, out Vector2 titik, out Vector3 kedalam, out Vector3 sepanjang)
    {
        float jarak = H - (PakaiPondasi ? TebalPondasi : 0.6f);
        float t = TengahSel(sel);
        switch (sisi)
        {
            case 0: titik = new Vector2(t, -jarak); kedalam = Vector3.forward; sepanjang = Vector3.right; break;
            case 1: titik = new Vector2(t, jarak); kedalam = Vector3.back; sepanjang = Vector3.left; break;
            case 2: titik = new Vector2(-jarak, t); kedalam = Vector3.right; sepanjang = Vector3.back; break;
            default: titik = new Vector2(jarak, t); kedalam = Vector3.left; sepanjang = Vector3.forward; break;
        }
    }

    // titik tengah kotak grid ke-i
    static float TengahSel(int i) => -H + Sel * (i + 0.5f);
    static int SelDari(float x) => Mathf.Clamp(Mathf.FloorToInt((x + H) / Sel), 0, N - 1);

    // Obstacle L itu punya pijakan kecil di atas sama penyangga miring di satu sisi.
    // di file 3D artist, Frame Glass rebahan di atas pijakan itu, jadi atap.
    // pijakannya ada di sisi yang jauh dari pivot, hasilnya +1 kalau di depan slot, -1 kalau di belakang
    static float SisiAtap(Transform m, Transform slot)
    {
        MeshFilter mf = m.GetComponentInChildren<MeshFilter>();
        Vector3 tengah = mf.transform.TransformPoint(mf.sharedMesh.bounds.center);
        Vector3 d = slot.InverseTransformDirection(tengah - mf.transform.position);
        Vector3 b = BatasLokal(m, slot).size;
        float arah = b.x >= b.z ? d.z : d.x; // dicek di arah tipisnya obstacle
        return arah >= 0f ? 1f : -1f;
    }

    // taruh Frame Glass rebahan di atas obstacle, rata sama ujung atasnya, nongol 2 m ke sisi pijakan
    static void PasangKaca(Transform m, Transform slot)
    {
        Transform g = Pasang("frameglass", slot, slot.position, slot.rotation, bahanKaca, out Transform slotKaca);
        if (g == null) return;
        Arahkan(g, slotKaca, -1, 1); // tipis ke atas, jadi rebahan

        Bounds o = BatasLokal(m, slot);
        Bounds k = BatasLokal(g, slotKaca);
        int iPanjang = o.size.x >= o.size.z ? 0 : 2;
        int iSamping = 2 - iPanjang;
        float masuk = 0.09f; // sedikit masuk ke badan obstacle biar nggak ada celah

        Vector3 t = Vector3.zero;
        t[iPanjang] = o.center[iPanjang];
        t.y = o.max.y - k.extents.y;
        t[iSamping] = SisiAtap(m, slot) > 0 ? o.max[iSamping] - masuk + k.extents[iSamping] : o.min[iSamping] + masuk - k.extents[iSamping];
        g.position += slot.TransformDirection(t - k.center);
        KasihCollider(g, true);
    }

    // ---------------------------------------------------------------- PETI (CUBE)
    // bentuk-bentuk tumpukan box. tiap box: x, z, ukuran, numpuk di atas box ke-berapa (-1 = di lantai)
    struct Box { public float x, z, ukuran; public int diAtas; public Box(float x, float z, float ukuran, int diAtas) { this.x = x; this.z = z; this.ukuran = ukuran; this.diAtas = diAtas; } }
    static readonly Box[][] BentukTumpukan =
    {
        new[] { new Box(0f, 0f, 1f, -1) },                                                               // satu
        new[] { new Box(-0.55f, 0f, 1f, -1), new Box(0.55f, 0f, 1f, -1) },                              // berjejer 2
        new[] { new Box(-0.55f, -0.55f, 1f, -1), new Box(0.55f, -0.55f, 1f, -1), new Box(-0.55f, 0.55f, 1f, -1) }, // L
        new[] { new Box(-0.55f, -0.55f, 1f, -1), new Box(0.55f, -0.55f, 1f, -1), new Box(-0.55f, 0.55f, 1f, -1), new Box(0.55f, 0.55f, 1f, -1) }, // kotak 2x2
        new[] { new Box(-0.55f, -0.55f, 1f, -1), new Box(0.55f, -0.55f, 1f, -1), new Box(-0.55f, 0.55f, 1f, -1), new Box(0.55f, 0.55f, 1f, -1), new Box(0f, 0f, 1f, 0) }, // piramid
        new[] { new Box(0f, 0f, 1f, -1), new Box(0f, 0f, 0.9f, 0), new Box(0f, 0f, 0.8f, 1) },        // menara 3
        new[] { new Box(0f, 0f, 1.6f, -1), new Box(0.2f, 0.1f, 0.8f, 0) },                             // box gede + kecil di atas
        new[] { new Box(-1.1f, 0f, 1f, -1), new Box(0f, 0f, 1f, -1), new Box(0f, 0f, 1f, 1), new Box(1.1f, 0f, 1f, -1), new Box(1.1f, 0f, 1f, 3), new Box(1.1f, 0f, 1f, 4) }, // tangga
        new[] { new Box(-0.6f, 0f, 1.2f, -1), new Box(0.75f, 0.3f, 0.7f, -1), new Box(0.75f, -0.5f, 0.6f, -1) }, // gede + 2 kecil
    };

    static void SusunPeti(Permukaan p, Transform grup, bool[,] terpakai)
    {
        int dipasang = 0;
        int jumlah = p.bisaDiinjek ? TumpukanPeti : TumpukanPetiDepanBelakang;
        for (int coba = 0; coba < 120 && dipasang < jumlah; coba++)
        {
            Box[] bentuk = BentukTumpukan[acak.Next(BentukTumpukan.Length)];
            float s = UkuranPetiMin + (float)acak.NextDouble() * (UkuranPetiMax - UkuranPetiMin);

            // tempat & arah tumpukan: nempel tembok (pinggir mana aja) atau di tengah
            Vector2 titik; Vector3 kedalam, sepanjang;
            bool diPinggir = p.bisaDiinjek && acak.NextDouble() < PeluangPetiDiPinggir; // depan/belakang cuma di tengah
            if (diPinggir) TitikPinggir(acak.Next(4), 3 + acak.Next(N - 6), out titik, out kedalam, out sepanjang);
            else
            {
                Vector3[] empatArah = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
                kedalam = empatArah[acak.Next(4)];
                sepanjang = Vector3.Cross(Vector3.up, kedalam);
                titik = new Vector2(TengahSel(3 + acak.Next(N - 6)), TengahSel(3 + acak.Next(N - 6)));
            }
            if (!Kosong(terpakai, SelDari(titik.x), SelDari(titik.y), 2)) continue;

            // kalau nempel tembok, geser biar box paling belakang pas nempel tembok
            float belakang = 0f;
            if (diPinggir)
            {
                belakang = 999f;
                foreach (Box b in bentuk) belakang = Mathf.Min(belakang, b.z - b.ukuran * 0.5f);
            }

            float[] puncak = new float[bentuk.Length];
            for (int k = 0; k < bentuk.Length; k++)
            {
                Box b = bentuk[k];
                Vector3 geser = (sepanjang * b.x + kedalam * (b.z - belakang)) * s;
                float naik = b.diAtas >= 0 ? puncak[b.diAtas] : 0f;
                string jenis = acak.NextDouble() < 0.5 ? "cube1" : "cube2";
                float yaw = Vector3.SignedAngle(Vector3.forward, kedalam, Vector3.up) + 90f * acak.Next(4);
                puncak[k] = naik + PasangPeti(p, grup, titik.x + geser.x, titik.y + geser.z, naik, jenis, b.ukuran * s, yaw);
            }
            Tandai(terpakai, SelDari(titik.x), SelDari(titik.y), 2);
            dipasang++;
        }
    }

    static float PasangPeti(Permukaan p, Transform grup, float u, float v, float naik, string jenis, float ukuran, float yaw)
    {
        Transform m = Pasang(jenis, grup, p.KeDunia(u, 0f, v), p.arah, bahan, out Transform slot);
        if (m == null) return 1f;
        m.parent.localScale = Vector3.one * ukuran;
        m.RotateAround(slot.position, slot.up, yaw);
        Ratakan(m, slot, Rata.Bawah, naik + p.alas);
        KasihCollider(m, true);
        return BatasLokal(m, slot).size.y;
    }

    // ---------------------------------------------------------------- UBIN LANTAI
    static void SusunUbin(Permukaan p, Transform grup)
    {
        float ukuran = Sel * SkalaUbin;                 // ukuran 1 ubin di arena
        int jumlah = Mathf.RoundToInt(2f * H / ukuran); // jumlah ubin per sisi
        int separuh = jumlah / 2;
        bool[,] adaUbin = new bool[jumlah, jumlah];

        // 4 Floor L ngelilingin tengah, kotak kosongnya ngadep ke tengah (kayak kipas)
        if (PakaiLantaiL && jumlah >= 4)
        {
            int[,] blok = { { separuh - 2, separuh - 2, 3 }, { separuh, separuh - 2, 2 }, { separuh - 2, separuh, 1 }, { separuh, separuh, 0 } };
            for (int b = 0; b < 4; b++)
            {
                int i = blok[b, 0], j = blok[b, 1], kosongMau = blok[b, 2];
                float u = -H + ukuran * (i + 1), v = -H + ukuran * (j + 1);
                Transform m = PasangUbin("floorl", grup, p, u, v, 0, out Transform slot);
                if (m == null) break;
                for (int coba = 0; coba < 4 && KotakKosongLantaiL(m, slot) != kosongMau; coba++)
                    m.RotateAround(slot.position, slot.up, 90f);
                for (int q = 0; q < 4; q++)
                    if (q != kosongMau) adaUbin[i + (q & 1), j + (q >> 1)] = true;
            }
        }

        // sisanya Floor 1 / Floor 2 selang-seling kayak papan catur,
        // arahnya diputer per seperempat bagian biar simetris dari tengah
        for (int i = 0; i < jumlah; i++)
            for (int j = 0; j < jumlah; j++)
            {
                if (adaUbin[i, j]) continue;
                int putar = i < separuh ? (j < separuh ? 0 : 3) : (j < separuh ? 1 : 2);
                string jenis = (i + j) % 2 == 0 ? "floor1" : "floor2";
                Transform m = PasangUbin(jenis, grup, p, -H + ukuran * (i + 0.5f), -H + ukuran * (j + 0.5f), putar, out _);
                if (m == null) return;
            }
    }

    // pasang 1 ubin: hadap ke dalem arena, putar acak, dibesarin, permukaannya pas sama lantai
    static Transform PasangUbin(string kunci, Transform grup, Permukaan p, float u, float v, int putar, out Transform slot)
    {
        Transform m = Pasang(kunci, grup, p.KeDunia(u, 0f, v), p.arah, bahan, out slot);
        if (m == null) return null;
        Arahkan(m, slot, -1, 1);
        JanganKebalik(m, slot);
        m.RotateAround(slot.position, slot.up, 90f * putar);
        m.parent.localScale = new Vector3(SkalaUbin, 1f, SkalaUbin); // besarin lebar & panjang, tebelnya tetep
        Ratakan(m, slot, Rata.Atas, TinggiBaut); // permukaan ubin pas sama lantai, bautnya nonjol dikit
        return m;
    }

    // Floor L bentuknya L, cari 1 kotak yang kosong (0=kiri-belakang, 1=kanan-belakang, 2=kiri-depan, 3=kanan-depan)
    static int KotakKosongLantaiL(Transform m, Transform slot)
    {
        int[] hitung = new int[4];
        foreach (MeshFilter mf in m.GetComponentsInChildren<MeshFilter>())
            foreach (Vector3 titik in mf.sharedMesh.vertices)
            {
                Vector3 l = slot.InverseTransformPoint(mf.transform.TransformPoint(titik));
                if (Mathf.Abs(l.x) < 0.1f || Mathf.Abs(l.z) < 0.1f) continue;
                hitung[(l.x > 0f ? 1 : 0) + (l.z > 0f ? 2 : 0)]++;
            }
        int kosong = 0;
        for (int q = 1; q < 4; q++) if (hitung[q] < hitung[kosong]) kosong = q;
        return kosong;
    }

    // ---------------------------------------------------------------- PIPA HIASAN (DINDING DEPAN & BELAKANG)
    // cuma tempelan: NGGAK ada collider, jadi nggak ganggu jalan / peluru.
    // bentuknya kayak jaringan pipa beneran (simetris):
    //   - 2 pipa tegak di kiri & kanan, keluar dari pondasi bawah
    //   - pipa mendatar bawah nyambungin keduanya (pakai Pipe X), ujung luarnya belok masuk tembok
    //   - di atas belok ke tengah (Pipe L), ketemu di Pipe X tengah:
    //     ke atas masuk pondasi plafon, ke bawah sebentar terus belok masuk tembok
    // posisi di dinding: a = kiri-kanan (0 = tengah), b = tinggi dari lantai

    struct Ruas { public Vector2 dari, ke; public Ruas(Vector2 dari, Vector2 ke) { this.dari = dari; this.ke = ke; } }

    // pipa lurus di antara sambungan (ujung lengan Pipe L / Pipe X panjangnya 1 m dari titik tengahnya)
    static List<Ruas> JalurLurus()
    {
        float A = PipaPinggir, B = PipaBawah, T = PipaAtas;
        List<Ruas> r = new List<Ruas>();
        for (int s = -1; s <= 1; s += 2)
        {
            r.Add(new Ruas(new Vector2(s * A, 0.3f), new Vector2(s * A, B - 1f)));     // pondasi -> X bawah
            r.Add(new Ruas(new Vector2(s * A, B + 1f), new Vector2(s * A, T - 1f)));   // X bawah -> belokan atas
            r.Add(new Ruas(new Vector2(s * (A - 1f), T), new Vector2(s * 1f, T)));     // belokan atas -> X tengah
        }
        r.Add(new Ruas(new Vector2(-(A - 1f), B), new Vector2(A - 1f, B)));           // mendatar bawah
        r.Add(new Ruas(new Vector2(0f, T + 1f), new Vector2(0f, 2f * H - 0.5f)));     // X tengah -> masuk pondasi plafon
        return r;
    }

    // titik di dinding -> posisi dunia. sisi -1 = dinding depan (z kecil), +1 = belakang
    static Vector3 TitikDinding(float sisi, float a, float b, float keluar)
    {
        float tebal = (PakaiDinding ? 0.12f : 0f) + keluar;
        return new Vector3(a, b, sisi * (H - tebal));
    }

    // kotak grid yang dilewatin pipa ditandain, biar obstacle & box di dinding depan/belakang nggak nabrak pipa
    static void TandaiJalurPipa(Permukaan p, bool[,] terpakai)
    {
        if (p.bisaDiinjek) return;
        List<Vector2> titik = new List<Vector2>();
        foreach (Ruas r in JalurLurus())
        {
            int n = Mathf.CeilToInt(Vector2.Distance(r.dari, r.ke));
            for (int k = 0; k <= n; k++) titik.Add(Vector2.Lerp(r.dari, r.ke, n > 0 ? (float)k / n : 0f));
        }
        float A = PipaPinggir, B = PipaBawah;
        titik.Add(new Vector2(-(A + 2f), B)); titik.Add(new Vector2(A + 2f, B));

        for (int s = -1; s <= 1; s += 2)
            foreach (Vector2 t in titik)
            {
                Vector3 l = p.KeLokal(TitikDinding(s, t.x, t.y, 0.4f));
                if (Mathf.Abs(l.y) > 2f) continue; // pipa dinding seberang
                Tandai(terpakai, SelDari(l.x), SelDari(l.z), 1);
            }
    }

    static Transform SusunPipaDinding(Transform induk, float sisi, string nama)
    {
        Transform grup = new GameObject(nama).transform;
        grup.SetParent(induk, false);
        Vector3 keluar = Vector3.forward * -sisi;  // arah keluar dari tembok (ke dalem arena)

        // ukur pipa dulu: jari-jari & panjang 1 potong
        Transform contoh = Pasang("pipestraight", grup, Vector3.zero, Quaternion.identity, bahan, out Transform slotContoh);
        if (contoh == null) return grup;
        Arahkan(contoh, slotContoh, 2, -1);
        Vector3 ukuran = BatasLokal(contoh, slotContoh).size;
        Object.DestroyImmediate(slotContoh.gameObject);
        float jari = Mathf.Min(ukuran.x, ukuran.y) * 0.5f;
        float panjangPotong = Mathf.Max(ukuran.z, 0.3f);
        float jarak = jari + 0.02f;                // pipa nempel tembok

        // pipa lurus
        foreach (Ruas r in JalurLurus())
        {
            Vector3 dari = TitikDinding(sisi, r.dari.x, r.dari.y, jarak);
            Vector3 ke = TitikDinding(sisi, r.ke.x, r.ke.y, jarak);
            float panjang = Vector3.Distance(dari, ke);
            int n = Mathf.Max(1, Mathf.RoundToInt(panjang / panjangPotong));
            float potong = panjang / n;
            Vector3 arah = (ke - dari).normalized;
            for (int k = 0; k < n; k++)
                PasangPipaLurus(grup, dari + arah * potong * (k + 0.5f), arah, keluar, potong);
        }

        // sambungan
        float A = PipaPinggir, B = PipaBawah, T = PipaAtas;
        Vector3 naik = Vector3.up, samping = Vector3.right;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 kedalam = samping * -s; // ke arah tengah dinding
            PasangPipaX(grup, TitikDinding(sisi, s * A, B, jarak), keluar);                            // pertigaan bawah
            PasangPipaL(grup, TitikDinding(sisi, s * A, T, jarak), -naik, kedalam);                    // belokan atas
            PasangPipaL(grup, TitikDinding(sisi, s * (A + 2f), B, jarak), -samping * s, -keluar);      // ujung luar masuk tembok
        }
        PasangPipaX(grup, TitikDinding(sisi, 0f, T, jarak), keluar);                                   // pertemuan tengah
        PasangPipaL(grup, TitikDinding(sisi, 0f, T - 2f, jarak), naik, -keluar);                       // turun terus masuk tembok
        return grup;
    }

    // 1 potong pipa lurus, panjangnya dipas-in
    static void PasangPipaLurus(Transform grup, Vector3 tengah, Vector3 arah, Vector3 keluar, float panjang)
    {
        Transform m = Pasang("pipestraight", grup, tengah, Quaternion.LookRotation(arah, keluar), bahan, out Transform slot);
        if (m == null) return;
        Arahkan(m, slot, 2, -1);
        m.parent.localScale = new Vector3(1f, 1f, panjang / Mathf.Max(0.01f, BatasLokal(m, slot).size.z));
        Ratakan(m, slot, Rata.Tengah);
    }

    // Pipe X: tanda tambah, rata sama tembok
    static void PasangPipaX(Transform grup, Vector3 tengah, Vector3 keluar)
    {
        Transform m = Pasang("pipex", grup, tengah, Quaternion.LookRotation(keluar, Vector3.up), bahan, out Transform slot);
        if (m == null) return;
        Arahkan(m, slot, -1, 2); // tipisnya ngadep keluar tembok
        Ratakan(m, slot, Rata.Tengah);
    }

    // Pipe L: titik = tengah belokan, lengan1 & lengan2 = arah kedua ujungnya
    static void PasangPipaL(Transform grup, Vector3 titik, Vector3 lengan1, Vector3 lengan2)
    {
        Transform m = Pasang("pipel", grup, titik, Quaternion.LookRotation(lengan1, lengan2), bahan, out Transform slot);
        if (m == null) return;
        Arahkan(m, slot, -1, 0); // tipisnya ke samping, jadi kedua lengan ada di bidang depan-atas slot

        // cari sisi belokannya: di ujung belokan, pipanya melebar ke samping (ada lengan satunya),
        // di ujung lengan cuma selebar pipa. puter sampai belokannya di belakang-bawah slot,
        // jadi lengannya ngarah ke +depan & +atas
        if (Lebar(m, slot, 2, 1, true) > Lebar(m, slot, 2, 1, false)) m.RotateAround(slot.position, slot.up, 180f);
        if (Lebar(m, slot, 1, 2, true) > Lebar(m, slot, 1, 2, false)) m.RotateAround(slot.position, slot.forward, 180f);

        // tengah belokan (titik ketemunya 2 garis tengah pipa) dipas-in ke titik
        Bounds b = BatasLokal(m, slot);
        float r = b.size.x * 0.5f;
        Vector3 belokan = new Vector3(b.center.x, b.min.y + r, b.min.z + r);
        m.position -= slot.TransformDirection(belokan);
    }

    // seberapa lebar model (di sumbu 'sumbuLebar') di dekat ujung sumbu 'sumbuUjung' (ujung max / min)
    static float Lebar(Transform m, Transform slot, int sumbuUjung, int sumbuLebar, bool ujungMax)
    {
        Bounds b = BatasLokal(m, slot);
        float batas = ujungMax ? b.max[sumbuUjung] : b.min[sumbuUjung];
        float kecil = 999f, besar = -999f;
        foreach (MeshFilter mf in m.GetComponentsInChildren<MeshFilter>())
            foreach (Vector3 v in mf.sharedMesh.vertices)
            {
                Vector3 l = slot.InverseTransformPoint(mf.transform.TransformPoint(v));
                if (Mathf.Abs(l[sumbuUjung] - batas) > 0.15f) continue;
                kecil = Mathf.Min(kecil, l[sumbuLebar]);
                besar = Mathf.Max(besar, l[sumbuLebar]);
            }
        return besar > kecil ? besar - kecil : 0f;
    }

    // ---------------------------------------------------------------- DINDING DEPAN / BELAKANG
    // panel Wall, plus hiasan pipa. Dinding ini nggak bisa diinjek.
    static Transform SusunDinding(Transform induk, Vector3 pusat, Vector3 kedalam, string nama)
    {
        Transform grup = new GameObject(nama).transform;
        grup.SetParent(induk, false);
        Quaternion rot = Quaternion.LookRotation(kedalam, Vector3.up);
        Vector3 kanan = rot * Vector3.right;

        float ukuran = Sel * SkalaDinding;

        // kayak contoh artist: panel Wall berdiri DI ATAS pondasi, di antara 2 pojok Wall L,
        // sampai ke bawah pondasi atas. jadi nggak ada yang ketumpuk
        float kiri = -H, kanan2 = H, bawah = 0f, atas = 2f * H;
        if (PakaiPondasi)
        {
            float baut = PakaiUbin ? TinggiBaut : 0f;
            kiri += LebarPojok + baut; kanan2 -= LebarPojok + baut;
            bawah += TinggiPondasi + baut; atas -= TinggiPondasi + baut;
        }
        int kolom = Mathf.Max(1, Mathf.RoundToInt((kanan2 - kiri) / ukuran));
        int baris = Mathf.Max(1, Mathf.RoundToInt((atas - bawah) / ukuran));
        float lebarSel = (kanan2 - kiri) / kolom, tinggiSel = (atas - bawah) / baris;

        for (int i = 0; i < kolom; i++)
            for (int j = 0; j < baris; j++)
            {
                Vector3 pos = new Vector3(0f, 0f, pusat.z) + kanan * (kiri + lebarSel * (i + 0.5f)) * Mathf.Sign(kanan.x) + Vector3.up * (bawah + tinggiSel * (j + 0.5f));
                // Wall 2 cuma di pinggir biar jadi bingkai yang rapi, bukan tambal-tambalan
                int jarakKePinggir = Mathf.Min(Mathf.Min(i, j), Mathf.Min(kolom - 1 - i, baris - 1 - j));
                string jenis = jarakKePinggir < BingkaiWall2 && Model("wall2") != null ? "wall2" : "wall";
                Transform m = Pasang(jenis, grup, pos, rot, bahan, out Transform slot);
                if (m == null) continue;
                Arahkan(m, slot, -1, 2);       // tipis ke depan, jadi berdiri
                JanganKebalikDinding(m, slot);
                Bounds asli = BatasLokal(m, slot);
                m.parent.localScale = new Vector3(lebarSel / asli.size.x, tinggiSel / asli.size.y, 1f); // pas 1 sel, tebelnya tetep
                Ratakan(m, slot, Rata.Panel);
            }

        return grup;
    }

    // ---------------------------------------------------------------- PONDASI & POJOK
    // kayak contoh dari artist: pondasi di kaki dinding (dan di atas, nempel plafon),
    // pojok bawah/atas pakai Foundation Wall L, pojok tegak (ketemu dinding kiri/kanan) ditumpuk Wall L
    static int jumlahPondasi;

    static void SusunPondasi(Transform grup)
    {
        float tinggi = 1f;       // tinggi pondasi
        float lebarPojok = 1.1f; // ukuran pojok L
        float panjang = 2f * H - 2f * lebarPojok;

        // pondasi lurus di bawah & atas keempat dinding (depan, belakang, kiri, kanan)
        Vector3[] arahDinding = { Vector3.back, Vector3.forward, Vector3.left, Vector3.right };
        // biar nggak ketumpuk: pondasi ditaruh di depan panel Wall (depan/belakang) atau di depan ubin (kiri/kanan),
        // terus alasnya di atas ubin lantai/plafon
        float depanPanel = PakaiDinding ? TebalPanelWall : 0f;
        float depanUbin = PakaiUbin ? TinggiBaut : 0f;
        foreach (Vector3 d in arahDinding)
        {
            Vector3 kedalam = -d;    // ngadep ke tengah arena
            float mundur = d.z != 0f ? depanPanel : depanUbin;
            Vector3 garis = d * H + kedalam * mundur;
            JejerPondasi(grup, new Vector3(garis.x, depanUbin, garis.z), Vector3.up, kedalam, panjang);
            JejerPondasi(grup, new Vector3(garis.x, 2f * H - depanUbin, garis.z), Vector3.down, kedalam, panjang);
        }

        // 4 pojok tegak: Foundation L di bawah & atas, Wall L ditumpuk di tengahnya
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 kedalam = Vector3.forward * -sz; // menjauh dari dinding depan/belakang
                Vector3 menjauh = Vector3.right * -sx;   // menjauh dari dinding kiri/kanan
                float x = sx * (H - depanUbin), z = sz * (H - depanPanel);
                PasangPojok("foundationwalll", grup, new Vector3(x, depanUbin, z), kedalam, Vector3.up, menjauh, 1f);
                PasangPojok("foundationwalll", grup, new Vector3(x, 2f * H - depanUbin, z), kedalam, Vector3.down, menjauh, 1f);

                // Wall L tingginya 2 m, jangan ditarik biar garis-garisnya nggak melar
                float tegak = 2f * H - 2f * (tinggi + depanUbin);
                int jumlah = Mathf.Max(1, Mathf.RoundToInt(tegak / 2f));
                float potong = tegak / jumlah;
                for (int i = 0; i < jumlah; i++)
                    PasangPojok("walll", grup, new Vector3(x, depanUbin + tinggi + potong * i, z), kedalam, Vector3.up, menjauh, potong);
            }
    }

    // satu potong pojok L. titik = sudut luarnya, tinggi = tinggi potongan (dibesarin/dikecilin ke situ)
    static void PasangPojok(string kunci, Transform grup, Vector3 titik, Vector3 kedalam, Vector3 naik, Vector3 menjauh, float tinggi)
    {
        Transform m = Pasang(kunci, grup, titik, Quaternion.LookRotation(kedalam, naik), bahan, out Transform slot);
        if (m == null) return;

        // tingginya ke atas slot: Wall L sisi terpanjangnya tinggi, Foundation L sisi terpendeknya tinggi
        if (kunci == "walll") Arahkan(m, slot, 1, -1);
        else Arahkan(m, slot, -1, 1);
        if (RataRataVerteks(m, slot).y > BatasLokal(m, slot).center.y) m.RotateAround(slot.position, slot.forward, 180f);

        // verteks L numpuk di sudut luarnya. puter 90 derajat sampai sudut luarnya
        // nempel dinding (z kecil) dan nempel dinding samping
        float sx = Vector3.Dot(slot.right, menjauh) > 0f ? 1f : -1f;
        for (int coba = 0; coba < 4; coba++)
        {
            Vector3 beda = RataRataVerteks(m, slot) - BatasLokal(m, slot).center;
            if (beda.z < 0f && beda.x * sx < 0f) break;
            m.RotateAround(slot.position, slot.up, 90f);
        }

        Bounds b0 = BatasLokal(m, slot);
        m.parent.localScale = new Vector3(1f, tinggi / b0.size.y, 1f);

        // taruh sudut luarnya pas di titik
        Bounds b = BatasLokal(m, slot);
        Vector3 geser = new Vector3(sx > 0f ? -b.min.x : -b.max.x, -b.min.y, -b.min.z);
        m.position += slot.TransformDirection(geser);
        KasihCollider(m, false);
        jumlahPondasi++;
    }

    // satu baris pondasi lurus. naik = arah atasnya pondasi (keluar dari lantai/plafon)
    static void JejerPondasi(Transform grup, Vector3 tengah, Vector3 naik, Vector3 kedalam, float panjang)
    {
        Quaternion rot = Quaternion.LookRotation(kedalam, naik);
        Vector3 arahJejer = rot * Vector3.right;
        int jumlah = Mathf.Max(1, Mathf.RoundToInt(panjang / PanjangPondasi));
        float potong = panjang / jumlah;

        for (int i = 0; i < jumlah; i++)
        {
            Vector3 pos = tengah + arahJejer * (-panjang * 0.5f + potong * (i + 0.5f));
            Transform m = Pasang("foundationwall", grup, pos, rot, bahan, out Transform slot);
            if (m == null) return;
            Arahkan(m, slot, 0, 2);          // panjang ke samping, tipis ke depan
            HadapkanPondasi(m, slot);
            m.parent.localScale = new Vector3(potong / BatasLokal(m, slot).size.x, 1f, 1f);
            Ratakan(m, slot, Rata.Belakang); // alas di permukaan, punggung nempel dinding
            KasihCollider(m, true);
            jumlahPondasi++;
        }
    }

    // pondasi bagian atas-depannya miring, jadi verteksnya numpuk di bawah & belakang.
    // kalau rata-rata verteksnya di atas / di depan, berarti kebalik
    static void HadapkanPondasi(Transform m, Transform slot)
    {
        if (RataRataVerteks(m, slot).y > BatasLokal(m, slot).center.y) m.RotateAround(slot.position, slot.forward, 180f);
        if (RataRataVerteks(m, slot).z > BatasLokal(m, slot).center.z) m.RotateAround(slot.position, slot.up, 180f);
    }

    static Vector3 RataRataVerteks(Transform m, Transform slot)
    {
        Vector3 total = Vector3.zero;
        int n = 0;
        foreach (MeshFilter mf in m.GetComponentsInChildren<MeshFilter>())
            foreach (Vector3 v in mf.sharedMesh.vertices)
            {
                total += slot.InverseTransformPoint(mf.transform.TransformPoint(v));
                n++;
            }
        return n > 0 ? total / n : Vector3.zero;
    }

    // ---------------------------------------------------------------- GABUNG MESH
    // ratusan ubin = ratusan objek & draw call. digabung jadi 1 mesh, 1 material, tanpa bayangan
    // (ubin nempel di lantai lead, jadi bayangannya juga nggak kelihatan)
    static void Gabungkan(Transform grup, string nama)
    {
        List<CombineInstance> daftar = new List<CombineInstance>();
        foreach (MeshFilter mf in grup.GetComponentsInChildren<MeshFilter>())
            for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
            {
                CombineInstance c = new CombineInstance();
                c.mesh = mf.sharedMesh;
                c.subMeshIndex = s;
                c.transform = grup.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                daftar.Add(c);
            }
        if (daftar.Count == 0) return;
        bool adaCollider = grup.GetComponentInChildren<Collider>() != null; // misal pondasi

        Mesh mesh = new Mesh();
        mesh.name = nama;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // verteksnya bisa lebih dari 65 ribu
        mesh.CombineMeshes(daftar.ToArray(), true, true);

        // simpen jadi file biar scene-nya nggak bengkak
        string folder = FolderModel + "/Gabungan";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(FolderModel, "Gabungan");
        string path = folder + "/" + nama + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);

        for (int i = grup.childCount - 1; i >= 0; i--) Object.DestroyImmediate(grup.GetChild(i).gameObject);
        grup.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer r = grup.gameObject.AddComponent<MeshRenderer>();
        r.sharedMaterial = bahan;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (adaCollider) grup.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    // ---------------------------------------------------------------- ALAT BANTU
    // bikin "slot" (titik pasang) terus taruh modelnya di dalem
    static Transform Pasang(string kunci, Transform induk, Vector3 posisi, Quaternion rotasi, Material mat, out Transform slot)
    {
        slot = null;
        GameObject prefab = Model(kunci);
        if (prefab == null) return null;

        slot = new GameObject(prefab.name).transform;
        slot.SetParent(induk, false);
        slot.SetPositionAndRotation(posisi, rotasi);

        Transform skala = new GameObject("Skala").transform; // buat ngebesarin model tanpa ngerusak arahnya
        skala.SetParent(slot, false);
        GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(prefab, skala);
        // FBX bawa posisi dari scene Blender, nolin biar pivotnya pas di slot
        m.transform.localPosition = Vector3.zero;
        foreach (Renderer r in m.GetComponentsInChildren<Renderer>())
        {
            Material[] semua = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < semua.Length; i++) semua[i] = mat;
            r.sharedMaterials = semua;
        }
        return m.transform;
    }

    // ukuran model dilihat dari sudut pandang slot
    static Bounds BatasLokal(Transform m, Transform slot)
    {
        Bounds b = new Bounds();
        bool ada = false;
        foreach (MeshFilter mf in m.GetComponentsInChildren<MeshFilter>())
        {
            Bounds mb = mf.sharedMesh.bounds;
            for (int k = 0; k < 8; k++)
            {
                Vector3 sudut = new Vector3((k & 1) == 0 ? mb.min.x : mb.max.x, (k & 2) == 0 ? mb.min.y : mb.max.y, (k & 4) == 0 ? mb.min.z : mb.max.z);
                Vector3 l = slot.InverseTransformPoint(mf.transform.TransformPoint(sudut));
                if (!ada) { b = new Bounds(l, Vector3.zero); ada = true; }
                else b.Encapsulate(l);
            }
        }
        return b;
    }

    // putar model biar sisi terpanjangnya ke arah 'panjang' dan sisi tertipisnya ke arah 'tipis'
    // (0 = kanan, 1 = atas, 2 = depan, -1 = bebas)
    static void Arahkan(Transform m, Transform slot, int panjang, int tipis)
    {
        for (int coba = 0; coba < 3; coba++)
        {
            Vector3 s = BatasLokal(m, slot).size;
            int iPanjang = s.x >= s.y && s.x >= s.z ? 0 : (s.y >= s.z ? 1 : 2);
            int iTipis = s.x <= s.y && s.x <= s.z ? 0 : (s.y <= s.z ? 1 : 2);
            if (panjang >= 0 && iPanjang != panjang) { Putar90(m, slot, 3 - iPanjang - panjang); continue; }
            if (tipis >= 0 && iTipis != tipis) { Putar90(m, slot, 3 - iTipis - tipis); continue; }
            break;
        }
    }

    // ubin: titik pivot ada di permukaan atas, jadi bagian yang lebih tebel harus di bawah.
    // kalau kebalik (permukaannya ngadep ke luar arena), puter 180 derajat
    static void JanganKebalik(Transform m, Transform slot)
    {
        Bounds b = BatasLokal(m, slot);
        Vector3 p = Pivot(m, slot);
        if (b.max.y - p.y > p.y - b.min.y) m.RotateAround(slot.position, slot.forward, 180f);
    }

    // panel dinding: bagian tebel harus di belakang (nempel tembok), mukanya ngadep ke dalem arena
    // panel dinding cuma punya muka depan (belakangnya bolong), jadi diliat dari arah normal-nya:
    // kalau kebanyakan ngadep ke belakang, puter 180 derajat biar mukanya ngadep ke dalem arena
    static void JanganKebalikDinding(Transform m, Transform slot)
    {
        if (ArahMuka(m, slot).z < 0f) m.RotateAround(slot.position, slot.up, 180f);
    }

    static Vector3 ArahMuka(Transform m, Transform slot)
    {
        Vector3 total = Vector3.zero;
        foreach (MeshFilter mf in m.GetComponentsInChildren<MeshFilter>())
            foreach (Vector3 n in mf.sharedMesh.normals)
                total += slot.InverseTransformDirection(mf.transform.TransformDirection(n));
        return total;
    }

    // titik pivot mesh (dari Blender) dilihat dari slot
    static Vector3 Pivot(Transform m, Transform slot)
    {
        MeshFilter mf = m.GetComponentInChildren<MeshFilter>();
        return slot.InverseTransformPoint(mf != null ? mf.transform.position : m.position);
    }

    static void Putar90(Transform m, Transform slot, int sumbu)
    {
        Vector3 poros = sumbu == 0 ? slot.right : (sumbu == 1 ? slot.up : slot.forward);
        m.RotateAround(slot.position, poros, 90f);
    }

    // putar acak kelipatan 90 derajat
    static void PutarAcak(Transform m, Transform slot, Vector3? poros = null)
    {
        Vector3 p = slot.TransformDirection(poros ?? Vector3.up);
        m.RotateAround(slot.position, p, 90f * acak.Next(4));
    }

    enum Rata { Bawah, Atas, Belakang, Tengah, Panel }

    // geser model biar pas di slot:
    // Bawah = alas di permukaan, Atas = sisi atas sejajar permukaan (buat ubin),
    // Belakang = alas di permukaan + punggung nempel tembok, Tengah = pas di tengah,
    // Panel = tengahnya pas di titik pasang + punggung nempel tembok (buat panel dinding)
    static void Ratakan(Transform m, Transform slot, Rata rata, float naik = 0f)
    {
        Bounds b = BatasLokal(m, slot);
        Vector3 geser = -b.center;
        if (rata == Rata.Bawah || rata == Rata.Belakang) geser.y = -b.min.y;
        if (rata == Rata.Atas) geser.y = -b.max.y;
        if (rata == Rata.Belakang || rata == Rata.Panel) geser.z = -b.min.z;
        geser.y += naik;
        m.position += slot.TransformDirection(geser);
    }

    static void KasihCollider(Transform m, bool kotak)
    {
        foreach (MeshFilter mf in m.GetComponentsInChildren<MeshFilter>())
        {
            if (kotak) mf.gameObject.AddComponent<BoxCollider>();
            else mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }
    }

    static bool Kosong(bool[,] grid, int i, int j, int jarak)
    {
        for (int a = i - jarak; a <= i + jarak; a++)
            for (int b = j - jarak; b <= j + jarak; b++)
                if (a >= 0 && b >= 0 && a < N && b < N && grid[a, b]) return false;
        return true;
    }

    static void Tandai(bool[,] grid, int i, int j, int jarak)
    {
        for (int a = i - jarak; a <= i + jarak; a++)
            for (int b = j - jarak; b <= j + jarak; b++)
                if (a >= 0 && b >= 0 && a < N && b < N) grid[a, b] = true;
    }

    static void Kocok<T>(List<T> daftar)
    {
        for (int i = daftar.Count - 1; i > 0; i--)
        {
            int k = acak.Next(i + 1);
            (daftar[i], daftar[k]) = (daftar[k], daftar[i]);
        }
    }
}
