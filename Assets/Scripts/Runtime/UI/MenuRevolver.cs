using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    // revolver 3D di sebelah kiri main menu (gantiin tulisan "[ background sementara ]").
    // caranya: revolvernya ditaruh di "studio" kecil jauh di bawah scene, difoto kamera sendiri
    // ke RenderTexture, terus ditampilin di canvas pakai RawImage.
    // pistolnya ngambang pelan, tabungnya muter pas mouse di atas tombol MAINKAN,
    // dan pas MAINKAN diklik dia nembak (nendang + kilat + DOR).
    // dipasang lewat Tools > Menu > Pasang Revolver di Main Menu
    public class MenuRevolver : MonoBehaviour
    {
        public GameObject modelRevolver;   // SCI-FI Revolver.fbx

        [Header("Atur kalau kurang pas")]
        public float miring = 25f;         // seberapa nyerong ke kamera (derajat)
        public float besar = 0.85f;        // seberapa penuh ngisi kotaknya

        // nama bagian di FBX (sama kayak RevolverBuilder)
        const string NamaLaras = "Cube.003";
        const string NamaGagang = "Cube.002";
        static readonly string[] NamaTabung = { "Cylinder.002", "Cylinder.005" };

        static readonly Vector3 Studio = new Vector3(0f, -2000f, 0f);

        Transform poros;          // yang digoyang
        Transform model;
        Transform[] tabung;
        Vector3 pusatTabung, sumbuTabung;  // koordinat lokal poros
        Camera kameraStudio;
        RenderTexture rt;
        RawImage gambar;
        Light kilat;
        RectTransform tombolMainkan;
        AudioClip suaraTembak;

        float waktu, kick, putarTabung;
        bool udahNembak;

        void Start()
        {
            if (modelRevolver == null) { Debug.LogWarning("MenuRevolver: modelRevolver belum diisi."); return; }
            suaraTembak = Resources.Load<AudioClip>("Audio/SFX_Revolver");
            BikinStudio();
        }

        void BikinStudio()
        {
            GameObject studio = new GameObject("Studio_Revolver");
            studio.transform.position = Studio;

            poros = new GameObject("Poros").transform;
            poros.SetParent(studio.transform, false);

            model = Instantiate(modelRevolver, poros).transform;
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            foreach (Collider c in model.GetComponentsInChildren<Collider>()) Destroy(c);

            Transform laras = Cari(NamaLaras);
            Transform gagang = Cari(NamaGagang);
            tabung = new Transform[NamaTabung.Length];
            for (int i = 0; i < NamaTabung.Length; i++) tabung[i] = Cari(NamaTabung[i]);

            // 1. puter: laras ngadep kanan (ke arah menu), gagang ke bawah, nyerong dikit ke kamera
            if (laras != null && gagang != null && tabung[0] != null)
            {
                Vector3 tengahTabung = Batas(tabung[0]).center;
                Vector3 depan = (Batas(laras).center - tengahTabung).normalized;
                Vector3 atas = Vector3.ProjectOnPlane(tengahTabung - Batas(gagang).center, depan).normalized;
                Vector3 arahMau = Quaternion.Euler(0f, -miring, 0f) * Vector3.right;
                model.localRotation = Quaternion.LookRotation(arahMau, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(depan, atas));
            }

            // 2. kecilin jadi panjang 1 meter, tengahnya pas di poros
            Bounds b = Batas(model);
            float skala = 1f / Mathf.Max(0.001f, Mathf.Max(b.size.x, b.size.y));
            model.localScale *= skala;
            b = Batas(model);
            model.localPosition -= b.center;
            b = Batas(model);

            if (tabung[0] != null)
            {
                Bounds bt = Batas(tabung[0]);
                pusatTabung = bt.center;
                Vector3 laras2 = laras != null ? Batas(laras).center : pusatTabung + Vector3.right;
                sumbuTabung = (laras2 - pusatTabung).normalized;
            }

            // 3. lampu: satu putih dari depan atas, dua warna dari belakang biar pinggirannya nyala
            Lampu(studio.transform, "Lampu_Utama", new Vector3(-1.5f, 2f, -2.5f), Color.white, 3f);
            Lampu(studio.transform, "Lampu_Cyan", new Vector3(-2f, 0.5f, 2f), new Color(0.2f, 0.9f, 1f), 2.5f);
            Lampu(studio.transform, "Lampu_Magenta", new Vector3(2.5f, -0.5f, 1.5f), new Color(1f, 0.2f, 0.7f), 2.5f);

            kilat = new GameObject("Kilat_Moncong").AddComponent<Light>();
            kilat.transform.SetParent(poros, false);
            kilat.transform.localPosition = new Vector3(b.max.x + 0.1f, b.max.y * 0.5f, -0.2f);
            kilat.type = LightType.Point;
            kilat.color = new Color(1f, 0.75f, 0.4f);
            kilat.range = 3f;
            kilat.intensity = 0f;

            // 4. kamera studio -> RenderTexture (latarnya transparan)
            rt = new RenderTexture(Mathf.Max(256, Screen.width / 2), Mathf.Max(256, (int)(Screen.height * 0.8f)), 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            kameraStudio = new GameObject("Kamera_Studio").AddComponent<Camera>();
            kameraStudio.transform.SetParent(studio.transform, false);
            kameraStudio.clearFlags = CameraClearFlags.SolidColor;
            kameraStudio.backgroundColor = new Color(0f, 0f, 0f, 0f);
            kameraStudio.fieldOfView = 30f;
            kameraStudio.nearClipPlane = 0.1f;
            kameraStudio.farClipPlane = 20f;
            kameraStudio.targetTexture = rt;
            // mundurin kamera sampai pistolnya pas segini dari lebar layar
            float aspek = (float)rt.width / rt.height;
            float tinggiLihat = Mathf.Max(b.size.y, b.size.x / aspek) / besar;
            float jarak = tinggiLihat * 0.5f / Mathf.Tan(kameraStudio.fieldOfView * 0.5f * Mathf.Deg2Rad);
            kameraStudio.transform.localPosition = new Vector3(0f, 0f, -jarak);

            // 5. tampilin di canvas, di atas latar tapi di bawah menu
            GameObject canvasGo = GameObject.Find("Canvas_MainMenu");
            Transform latar = canvasGo != null ? Cari(canvasGo.transform, "Latar") : null;
            if (latar == null) { Debug.LogWarning("MenuRevolver: objek Latar di Canvas_MainMenu nggak ketemu."); return; }

            Transform sementara = Cari(latar, "Txt_LatarSementara");
            if (sementara != null) sementara.gameObject.SetActive(false);

            gambar = new GameObject("Gambar_Revolver", typeof(RectTransform)).AddComponent<RawImage>();
            gambar.transform.SetParent(latar, false);
            gambar.texture = rt;
            gambar.raycastTarget = false;
            RectTransform r = gambar.rectTransform;
            r.anchorMin = new Vector2(0.02f, 0.1f);
            r.anchorMax = new Vector2(0.52f, 0.9f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            r.SetAsLastSibling();

            Transform btn = Cari(canvasGo.transform, "Btn_Mainkan");
            tombolMainkan = btn as RectTransform;
        }

        void Update()
        {
            if (poros == null) return;
            float dt = Time.unscaledDeltaTime;
            waktu += dt;

            // mouse di atas tombol MAINKAN?
            bool hover = false;
            Mouse mouse = Mouse.current;
            if (tombolMainkan != null && mouse != null && tombolMainkan.gameObject.activeInHierarchy)
                hover = RectTransformUtility.RectangleContainsScreenPoint(tombolMainkan, mouse.position.ReadValue(), null);

            if (hover && !udahNembak && mouse.leftButton.wasPressedThisFrame) Tembak();

            // tabung muter kenceng pas hover, pelan pas diem
            float laju = hover ? 540f : 20f;
            float beda = laju * dt;
            putarTabung += beda;
            if (tabung != null)
                foreach (Transform t in tabung)
                    if (t != null) t.RotateAround(poros.TransformPoint(pusatTabung), poros.TransformDirection(sumbuTabung), beda);

            // ngambang + goyang dikit + nendang
            kick = Mathf.MoveTowards(kick, 0f, dt * 3f);
            float k = kick * kick;
            poros.localPosition = new Vector3(-0.12f * k, Mathf.Sin(waktu * 1.3f) * 0.03f, 0f);
            poros.localRotation = Quaternion.Euler(Mathf.Sin(waktu * 0.7f) * 3f, Mathf.Sin(waktu * 0.5f) * 8f, 25f * k);

            if (kilat != null) kilat.intensity = Mathf.MoveTowards(kilat.intensity, 0f, dt * 120f);
        }

        void Tembak()
        {
            udahNembak = true;
            kick = 1f;
            if (kilat != null) kilat.intensity = 12f;
            if (suaraTembak == null) return;

            // sumber suaranya dibikin nggak ikut ilang pas pindah scene, biar gemanya nggak kepotong
            GameObject go = new GameObject("Suara_TembakMenu");
            DontDestroyOnLoad(go);
            AudioSource s = go.AddComponent<AudioSource>();
            s.spatialBlend = 0f;
            s.PlayOneShot(suaraTembak);
            Destroy(go, suaraTembak.length + 0.1f);
        }

        void OnDestroy()
        {
            if (kameraStudio != null) kameraStudio.targetTexture = null;
            if (rt != null) rt.Release();
        }

        // ------------------------------------------------ alat bantu
        void Lampu(Transform induk, string nama, Vector3 pos, Color warna, float kuat)
        {
            Light l = new GameObject(nama).AddComponent<Light>();
            l.transform.SetParent(induk, false);
            l.transform.localPosition = pos;
            l.type = LightType.Point;
            l.range = 8f;
            l.color = warna;
            l.intensity = kuat;
        }

        Transform Cari(string nama) => Cari(model, nama);

        static Transform Cari(Transform induk, string nama)
        {
            foreach (Transform t in induk.GetComponentsInChildren<Transform>(true))
                if (t.name == nama) return t;
            return null;
        }

        // kotak pembatas mesh di bawah objek ini (koordinat poros)
        Bounds Batas(Transform objek)
        {
            bool ada = false;
            Bounds hasil = new Bounds();
            foreach (MeshFilter mf in objek.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                Bounds m = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 sudut = m.center + Vector3.Scale(m.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = poros.InverseTransformPoint(mf.transform.TransformPoint(sudut));
                    if (!ada) { hasil = new Bounds(p, Vector3.zero); ada = true; }
                    else hasil.Encapsulate(p);
                }
            }
            return hasil;
        }
    }
}
