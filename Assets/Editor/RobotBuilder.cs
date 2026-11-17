using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VertigoShift.Gameplay.Enemies;

// Tools > Musuh > Pasang Robot ke Enemy
// ganti kubus musuh jadi model Robot dari 3D artist:
// - animasi Idle & "Attack & Walk" dibikin looping, terus dibikinin Animator (state Diam & Jalan)
// - model dipasang di prefab Enemy, diputer ngadep depan, dibesarin, collider & highlight disesuaiin
// - moncong tembakan dipindah ke depan badan robot
public static class RobotBuilder
{
    const string PathModel = "Assets/Models/Char/Robot.fbx";
    const string PathPrefab = "Assets/Prefabs/Enemy.prefab";
    const string PathAnimator = "Assets/Models/Char/Robot_Animator.controller";
    const string PathKedip = "Assets/Models/Char/M_RobotKedip.mat";
    const string NamaModel = "Model_Robot";

    // nama bagian di FBX (dari Blender), dipakai buat nentuin depan & atas robot
    const string NamaBadan = "Cylinder";
    const string NamaMata = "Cylinder.001";
    const string NamaKepala = "Cube";

    const float Tinggi = 2.5f; // tinggi robot di game (meter). pemain 1.8 m. gedein/kecilin di sini
    const string FolderTekstur = "Assets/Models/Char/Textures"; // Robot_BaseColor, Robot_Normal, Robot_MetallicSmoothness, Robot_Emission
    const string PathBahan = "Assets/Models/Char/M_Robot.mat";

    [MenuItem("Tools/Musuh/Pasang Robot ke Enemy")]
    static void Pasang()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(PathModel);
        if (fbx == null) { Debug.LogWarning(PathModel + " nggak ketemu."); return; }

        // 1. animasi dibikin looping
        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(PathModel);
        ModelImporterClipAnimation[] klip = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation k in klip) { k.loopTime = true; k.loopPose = true; }
        importer.clipAnimations = klip;
        importer.SaveAndReimport();

        AnimationClip diam = null, jalan = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(PathModel))
        {
            AnimationClip c = o as AnimationClip;
            if (c == null || c.name.StartsWith("__preview")) continue;
            if (diam == null && c.name.Contains("Idle")) diam = c;
            if (jalan == null && c.name.Contains("Walk")) jalan = c;
        }

        // 2. Animator: Diam (awal) & Jalan
        AssetDatabase.DeleteAsset(PathAnimator);
        AnimatorController animator = AnimatorController.CreateAnimatorControllerAtPath(PathAnimator);
        AnimatorStateMachine sm = animator.layers[0].stateMachine;
        AnimatorState stDiam = sm.AddState("Diam");
        stDiam.motion = diam;
        AnimatorState stJalan = sm.AddState("Jalan");
        stJalan.motion = jalan != null ? jalan : diam;
        sm.defaultState = stDiam;

        // 3. material putih buat kedip pas ketembak
        Material kedip = AssetDatabase.LoadAssetAtPath<Material>(PathKedip);
        if (kedip == null)
        {
            kedip = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            kedip.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(kedip, PathKedip);
        }

        // 4. material robot dari tekstur (tekstur bawaan FBX-nya nyasar ke file yang nggak ada)
        Material bahanRobot = BuatBahanRobot();

        // 5. buka prefab Enemy, pasang robotnya
        GameObject root = PrefabUtility.LoadPrefabContents(PathPrefab);
        Transform lama = root.transform.Find(NamaModel);
        if (lama != null) Object.DestroyImmediate(lama.gameObject);

        // wadah yang diputer & dibesarin. robotnya di dalem wadah, biar animasi robot
        // (yang ikut ngatur ukuran & posisi bawaan Blender) nggak ngebalikin ukurannya jadi kecil lagi
        GameObject wadah = new GameObject(NamaModel);
        wadah.transform.SetParent(root.transform, false);
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(fbx, wadah.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        foreach (Collider c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        if (bahanRobot != null)
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
            {
                Material[] semua = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < semua.Length; i++) semua[i] = bahanRobot;
                r.sharedMaterials = semua;
            }

        // pose-in robot pakai frame pertama animasi Idle dulu, baru diukur.
        // animasi dari Blender ikut muter Armature-nya, jadi kalau diukur dari pose diem,
        // pas game jalan robotnya bisa rebahan / ngadep atas
        if (diam != null) diam.SampleAnimation(model, 0f);

        Transform badan = Cari(model.transform, NamaBadan);
        Transform mata = Cari(model.transform, NamaMata);
        Transform kepala = Cari(model.transform, NamaKepala);
        if (badan == null || mata == null || kepala == null)
        {
            Debug.LogWarning("Bagian badan/mata/kepala nggak ketemu di Robot.fbx, cek namanya di RobotBuilder.cs");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        // puter: kepala ke atas (+Y), mata ngadep depan (+Z).
        // arahnya dibuletin ke sumbu terdekat, soalnya kepala robotnya agak ke belakang dari badan,
        // kalau nggak dibuletin robotnya jadi nunduk
        Bounds batasSemua = Batas(model.transform, root.transform);
        Vector3 tengahBadan = Batas(badan, root.transform).center;
        Vector3 sumbuAtas = SumbuTerbesar(batasSemua.size);
        Vector3 atas = sumbuAtas * Mathf.Sign(Vector3.Dot(Batas(kepala, root.transform).center - tengahBadan, sumbuAtas));
        Vector3 depan = Bulatkan(Vector3.ProjectOnPlane(Batas(mata, root.transform).center - tengahBadan, atas));
        wadah.transform.localRotation = Quaternion.Inverse(Quaternion.LookRotation(depan, atas));

        // besarin sesuai tinggi, terus kakinya pas di bawah (kubus lama: dari -0.5 sampai 0.5)
        Bounds b = Batas(model.transform, root.transform);
        float skala = Tinggi / Mathf.Max(0.001f, b.size.y);
        wadah.transform.localScale = Vector3.one * skala;
        b = Batas(model.transform, root.transform);
        wadah.transform.localPosition += new Vector3(-b.center.x, -0.5f - b.min.y, -b.center.z);
        b = Batas(model.transform, root.transform);

        // animator di model
        Animator anim = model.GetComponent<Animator>();
        if (anim == null) anim = model.AddComponent<Animator>();
        anim.runtimeAnimatorController = animator;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // collider ngikutin badan robot
        BoxCollider kotak = root.GetComponent<BoxCollider>();
        if (kotak != null)
        {
            kotak.center = b.center;
            kotak.size = new Vector3(Mathf.Max(0.6f, b.size.x), b.size.y, Mathf.Max(0.6f, b.size.z));
        }

        // kotak highlight (yang keliatan nembus tembok) juga disesuaiin
        Transform highlight = root.transform.Find("Highlight");
        if (highlight != null)
        {
            highlight.localPosition = b.center;
            highlight.localScale = b.size * 1.08f;
        }

        // moncong tembakan di depan dada robot
        Transform moncong = root.transform.Find("Moncong");
        if (moncong == null) { moncong = new GameObject("Moncong").transform; moncong.SetParent(root.transform, false); }
        moncong.localPosition = new Vector3(0f, b.center.y + b.size.y * 0.15f, b.max.z + 0.1f);

        Enemy musuh = root.GetComponent<Enemy>();
        musuh.model = wadah.transform;
        musuh.animator = anim;
        musuh.bahanKedip = kedip;
        musuh.muzzle = moncong;

        // kubus lamanya disembunyiin dari awal
        MeshRenderer kubus = root.GetComponent<MeshRenderer>();
        if (kubus != null) kubus.enabled = false;

        PrefabUtility.SaveAsPrefabAsset(root, PathPrefab);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        Debug.Log("Robot udah kepasang di prefab Enemy. Tinggi " + b.size.y.ToString("0.0") + " m (dibesarin " + skala.ToString("0.0") + "x). Animasi: " + (diam != null ? diam.name : "-") + " / " + (jalan != null ? jalan.name : "-"));
    }

    // material URP Lit dari 4 tekstur robot. kalau teksturnya belum dimasukin, robotnya putih polos
    static Material BuatBahanRobot()
    {
        Texture2D warna = Tekstur("Robot_BaseColor", false, false);
        Texture2D normal = Tekstur("Robot_Normal", true, false);
        Texture2D logam = Tekstur("Robot_MetallicSmoothness", false, true);
        Texture2D nyala = Tekstur("Robot_Emission", false, false);
        if (warna == null)
        {
            Debug.LogWarning("Tekstur robot nggak ketemu di " + FolderTekstur + ", robotnya bakal putih polos.");
            return null;
        }

        Material m = AssetDatabase.LoadAssetAtPath<Material>(PathBahan);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, PathBahan);
        }
        m.SetTexture("_BaseMap", warna);
        m.SetColor("_BaseColor", Color.white);
        if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
        if (logam != null)
        {
            m.SetTexture("_MetallicGlossMap", logam);
            m.SetFloat("_Smoothness", 1f);
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        if (nyala != null)
        {
            m.SetTexture("_EmissionMap", nyala);
            m.SetColor("_EmissionColor", Color.white * 2f); // mata & lampu merahnya nyala
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D Tekstur(string nama, bool normalMap, bool linear)
    {
        string path = FolderTekstur + "/" + nama + ".png";
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return null;
        bool ubah = false;
        if (normalMap && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ubah = true; }
        if (linear && ti.sRGBTexture) { ti.sRGBTexture = false; ubah = true; }
        if (ubah) ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // sumbu (x/y/z) yang ukurannya paling panjang = tinggi robot
    static Vector3 SumbuTerbesar(Vector3 ukuran)
    {
        if (ukuran.y >= ukuran.x && ukuran.y >= ukuran.z) return Vector3.up;
        return ukuran.x >= ukuran.z ? Vector3.right : Vector3.forward;
    }

    // buletin arah ke sumbu terdekat (+x, -x, +y, ...)
    static Vector3 Bulatkan(Vector3 v)
    {
        Vector3 a = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        if (a.x >= a.y && a.x >= a.z) return new Vector3(Mathf.Sign(v.x), 0f, 0f);
        if (a.y >= a.z) return new Vector3(0f, Mathf.Sign(v.y), 0f);
        return new Vector3(0f, 0f, Mathf.Sign(v.z));
    }

    // ------------------------------------------------ alat bantu
    static Transform Cari(Transform induk, string nama)
    {
        foreach (Transform t in induk.GetComponentsInChildren<Transform>(true))
            if (t.name == nama) return t;
        return null;
    }

    // kotak pembatas semua mesh di bawah objek ini (koordinat 'ruang')
    static Bounds Batas(Transform objek, Transform ruang)
    {
        bool ada = false;
        Bounds hasil = new Bounds();
        List<KeyValuePair<Mesh, Transform>> daftar = new List<KeyValuePair<Mesh, Transform>>();
        foreach (MeshFilter mf in objek.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null) daftar.Add(new KeyValuePair<Mesh, Transform>(mf.sharedMesh, mf.transform));
        foreach (SkinnedMeshRenderer sk in objek.GetComponentsInChildren<SkinnedMeshRenderer>())
            if (sk.sharedMesh != null) daftar.Add(new KeyValuePair<Mesh, Transform>(sk.sharedMesh, sk.transform));

        foreach (var isi in daftar)
        {
            Bounds m = isi.Key.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 sudut = m.center + Vector3.Scale(m.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = ruang.InverseTransformPoint(isi.Value.TransformPoint(sudut));
                if (!ada) { hasil = new Bounds(p, Vector3.zero); ada = true; }
                else hasil.Encapsulate(p);
            }
        }
        return hasil;
    }
}
