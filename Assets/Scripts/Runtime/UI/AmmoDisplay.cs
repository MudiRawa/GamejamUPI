using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    /// <summary>
    /// Display ammo revolver di pojok kanan bawah.
    /// Memakai objek Panel_Ammo yang sudah ada di Canvas scene bila tersedia,
    /// jadi posisi & ukurannya bisa diatur langsung di Editor.
    /// </summary>
    public class AmmoDisplay : MonoBehaviour
    {
        public UITheme theme;
        public Canvas canvas;
        public VertigoShift.Gameplay.Weapons.Revolver revolver;

        [Header("Gambar opsional (kosong = pakai lingkaran otomatis)")]
        public Sprite gambarTabung;
        public Sprite gambarPeluru;
        public Sprite gambarLubang;

        RectTransform panelTabung;
        Image imgTabung;
        Image[] imgPeluru = new Image[0];
        Image imgPeluruKeluar;
        Text lblStatus;
        Text txtJumlah;
        Text txtMaks;
        Image garisStatus;
        Image ujungGaris;

        // Geseran tabung dari titik tengah panel (tabung di kanan, teks di kiri)
        const float pusatTabungX = 80f;

        float putar;
        float putarTujuan;
        int tembakanTerakhir;
        float lepas;
        bool tadiReload;

        // Sprite lingkaran otomatis (dibuat sekali, dipakai semua instance)
        static Sprite _bulat;
        static Sprite _cincin;

        static Sprite SpriteBulat
        {
            get
            {
                if (_bulat == null) _bulat = BuatSpriteBulat(false);
                return _bulat;
            }
        }

        static Sprite SpriteCincin
        {
            get
            {
                if (_cincin == null) _cincin = BuatSpriteBulat(true);
                return _cincin;
            }
        }

        Sprite SprTabung { get { return gambarTabung != null ? gambarTabung : SpriteCincin; } }
        Sprite SprPeluru { get { return gambarPeluru != null ? gambarPeluru : SpriteBulat; } }
        Sprite SprLubang { get { return gambarLubang != null ? gambarLubang : SpriteCincin; } }

        int Maks
        {
            get { return revolver != null ? Mathf.Max(1, revolver.magazineSize) : 6; }
        }

        void Start()
        {
            canvas = UIBind.AmbilCanvas(canvas, "Canvas_HUD");
            if (!CobaBind()) BuatUI();
            Inisialisasi();
        }

        /// <summary>Dipanggil oleh editor builder untuk membuat UI langsung di scene.</summary>
        public void BangunUI(Canvas c, UITheme t)
        {
            canvas = c;
            theme = t;
            BuatUI();
            Inisialisasi();
        }

        bool CobaBind()
        {
            if (canvas == null) return false;
            panelTabung = UIBind.Rect(canvas.transform, "Panel_Ammo");
            if (panelTabung == null) return false;

            imgTabung = UIBind.Gambar(panelTabung, "Img_Tabung");
            imgPeluruKeluar = UIBind.Gambar(panelTabung, "Peluru_Keluar");
            lblStatus = UIBind.Teks(panelTabung, "Lbl_Status");
            txtJumlah = UIBind.Teks(panelTabung, "Txt_Jumlah");
            txtMaks = UIBind.Teks(panelTabung, "Txt_Maks");
            garisStatus = UIBind.Gambar(panelTabung, "Garis_Status");
            ujungGaris = UIBind.Gambar(panelTabung, "Ujung_Garis");

            if (imgTabung != null && imgTabung.sprite == null) imgTabung.sprite = SprTabung;
            if (imgPeluruKeluar != null && imgPeluruKeluar.sprite == null) imgPeluruKeluar.sprite = SprPeluru;
            return true;
        }

        void Inisialisasi()
        {
            int maks = Maks;
            tembakanTerakhir = revolver != null ? revolver.ShotsFired : 0;
            putar = putarTujuan = 360f / maks;
            lepas = 0f;
            tadiReload = false;
            SiapkanPeluru(maks);
        }

        void SiapkanPeluru(int maks)
        {
            if (panelTabung == null) return;

            // pusat & radius tabung diambil dari Img_Tabung yang ada di scene,
            // supaya peluru tetap sejajar walau ukuran/posisi tabung diubah di Editor.
            Vector2 pusatTabung = imgTabung != null ? imgTabung.rectTransform.anchoredPosition : new Vector2(pusatTabungX, 0f);
            float radiusTabung = imgTabung != null ? Mathf.Min(imgTabung.rectTransform.sizeDelta.x, imgTabung.rectTransform.sizeDelta.y) * 0.5f : 60f;
            float radiusPeluru = radiusTabung * 0.6f;

            var daftar = new List<Image>(maks);
            for (int i = 0; i < maks; i++)
            {
                Image img = UIBind.Gambar(panelTabung, "Peluru_" + i);
                if (img == null)
                {
                    float sudut = (i * 360f / maks - 90f) * Mathf.Deg2Rad;
                    float px = pusatTabung.x + Mathf.Cos(sudut) * radiusPeluru;
                    float py = pusatTabung.y + Mathf.Sin(sudut) * radiusPeluru;
                    float setengah = 14f;
                    img = UIFactory.BuatImage(panelTabung, "Peluru_" + i, SprPeluru, WarnaPeluru,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(px - setengah, py - setengah), new Vector2(px + setengah, py + setengah));
                }
                if (img.sprite == null) img.sprite = SprPeluru;
                daftar.Add(img);
            }

            // Matikan sisa peluru lama kalau jumlahnya berkurang
            for (int i = maks; ; i++)
            {
                Image sisa = UIBind.Gambar(panelTabung, "Peluru_" + i);
                if (sisa == null) break;
                sisa.gameObject.SetActive(false);
            }

            imgPeluru = daftar.ToArray();
        }

        Color WarnaPeluru { get { return gambarPeluru != null ? Color.white : WarnaKuning; } }
        Color WarnaKuning { get { return theme != null ? theme.kuning : new Color(1f, 0.82f, 0.23f); } }
        Color WarnaCyan { get { return theme != null ? theme.cyan : new Color(0.16f, 0.94f, 1f); } }
        Color WarnaMerah { get { return theme != null ? theme.merah : new Color(1f, 0.25f, 0.25f); } }

        void BuatUI()
        {
            Color cyan = WarnaCyan;
            Color kuning = WarnaKuning;
            float radius = 60f;

            // Panel lebar: tabung di kanan, teks ammo di kiri tabung
            panelTabung = UIFactory.BuatPanel(canvas.transform, "Panel_Ammo",
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-370f, 10f), new Vector2(-10f, 130f));

            imgTabung = UIFactory.BuatImage(panelTabung, "Img_Tabung", SprTabung,
                gambarTabung != null ? Color.white : new Color(1f, 1f, 1f, 0.35f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-radius, -radius), new Vector2(radius, radius));
            imgTabung.rectTransform.anchoredPosition = new Vector2(pusatTabungX, 0f);

            int maks = Maks;
            for (int i = 0; i < maks; i++)
            {
                float sudut = (i * 360f / maks - 90f) * Mathf.Deg2Rad;
                float px = pusatTabungX + Mathf.Cos(sudut) * 36f;
                float py = Mathf.Sin(sudut) * 36f;

                UIFactory.BuatImage(panelTabung, "Peluru_" + i, SprPeluru, kuning,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(px - 14f, py - 14f), new Vector2(px + 14f, py + 14f));
            }

            imgPeluruKeluar = UIFactory.BuatImage(panelTabung, "Peluru_Keluar", SprPeluru, kuning,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(pusatTabungX - 14f, -44f), new Vector2(pusatTabungX + 14f, -16f));
            imgPeluruKeluar.gameObject.SetActive(false);

            // Teks di kiri tabung (anchor kanan panel)
            lblStatus = UIFactory.BuatText(panelTabung, "Lbl_Status", "REVOLVER",
                theme != null ? theme.fontTulisan : null, theme != null ? theme.ukuranKecil : 14,
                cyan, TextAnchor.MiddleRight,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-348f, 44f), new Vector2(-178f, 64f));

            garisStatus = UIFactory.BuatImage(panelTabung, "Garis_Status",
                null, new Color(cyan.r, cyan.g, cyan.b, 0.6f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-348f, 40f), new Vector2(-184f, 42f));
            ujungGaris = UIFactory.BuatImage(panelTabung, "Ujung_Garis", null, new Color(cyan.r, cyan.g, cyan.b, 0.6f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-184f, 37f), new Vector2(-178f, 45f));

            txtJumlah = UIFactory.BuatText(panelTabung, "Txt_Jumlah", "0",
                theme != null ? theme.fontAngka : null, theme != null ? theme.ukuranAngkaBesar : 56,
                Color.white, TextAnchor.MiddleRight,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-348f, -28f), new Vector2(-224f, 36f));

            txtMaks = UIFactory.BuatText(panelTabung, "Txt_Maks", "/ " + maks,
                theme != null ? theme.fontTulisan : null, theme != null ? theme.ukuranKecil : 14,
                new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleRight,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-224f, -22f), new Vector2(-164f, 22f));
        }

        void Update()
        {
            if (revolver == null || panelTabung == null) return;

            int peluru = revolver.bullets;
            int maks = Maks;
            float dt = Time.unscaledDeltaTime;

            if (revolver.ShotsFired != tembakanTerakhir)
            {
                tembakanTerakhir = revolver.ShotsFired;
                putarTujuan += 360f / maks;
                lepas = 1f;
            }

            if (revolver.IsReloading) putarTujuan += 500f * dt;

            if (tadiReload && !revolver.IsReloading)
            {
                float satu = 360f / maks;
                putarTujuan = Mathf.Round((putarTujuan - satu) / 360f) * 360f + satu;
            }
            tadiReload = revolver.IsReloading;
            putar = Mathf.Lerp(putar, putarTujuan, 15f * dt);
            lepas = Mathf.MoveTowards(lepas, 0f, 2.5f * dt);

            int terisi = revolver.IsReloading ? Mathf.FloorToInt(revolver.ReloadProgress * maks) : peluru;

            if (imgTabung != null && gambarTabung != null)
                imgTabung.rectTransform.localEulerAngles = new Vector3(0f, 0f, putar);

            // pusat & radius tabung mengikuti Img_Tabung yang diubah di Editor
            Vector2 pusatTabung = imgTabung != null ? imgTabung.rectTransform.anchoredPosition : new Vector2(pusatTabungX, 0f);
            float radiusTabung = imgTabung != null ? Mathf.Min(imgTabung.rectTransform.sizeDelta.x, imgTabung.rectTransform.sizeDelta.y) * 0.5f : 60f;
            float radiusPeluru = radiusTabung * 0.6f;

            for (int i = 0; i < imgPeluru.Length; i++)
            {
                Image img = imgPeluru[i];
                if (img == null) continue;

                float sudut = (putar + i * 360f / maks - 90f) * Mathf.Deg2Rad;
                img.rectTransform.anchoredPosition = new Vector2(
                    pusatTabung.x + Mathf.Cos(sudut) * radiusPeluru,
                    pusatTabung.y + Mathf.Sin(sudut) * radiusPeluru);
                img.gameObject.SetActive(true);

                bool adaPeluru = i < terisi;
                img.sprite = adaPeluru ? SprPeluru : SprLubang;
                img.color = adaPeluru
                    ? WarnaPeluru
                    : (gambarLubang != null ? Color.white : new Color(1f, 1f, 1f, 0.35f));
            }

            if (imgPeluruKeluar != null)
            {
                if (lepas > 0f)
                {
                    float k = 1f - lepas;
                    imgPeluruKeluar.rectTransform.anchoredPosition =
                        new Vector2(pusatTabung.x + k * 30f, pusatTabung.y - radiusPeluru - k * 70f + k * k * 50f);
                    Color c = WarnaPeluru;
                    c.a = lepas;
                    imgPeluruKeluar.color = c;
                    imgPeluruKeluar.gameObject.SetActive(true);
                }
                else
                {
                    imgPeluruKeluar.gameObject.SetActive(false);
                }
            }

            string tulisan = "REVOLVER";
            Color warnaTulisan = WarnaCyan;
            if (revolver.IsReloading)
            {
                tulisan = "RELOAD";
                warnaTulisan = WarnaKuning;
            }
            else if (peluru == 0)
            {
                tulisan = "[R] RELOAD";
                warnaTulisan = WarnaMerah;
                warnaTulisan.a = 0.4f + Mathf.PingPong(Time.unscaledTime * 3f, 0.6f);
            }

            if (lblStatus != null)
            {
                lblStatus.text = tulisan;
                lblStatus.color = warnaTulisan;
            }
            if (txtJumlah != null)
            {
                txtJumlah.text = peluru.ToString();
                txtJumlah.color = peluru == 0 ? WarnaMerah : Color.white;
            }
            if (txtMaks != null) txtMaks.text = "/ " + maks;
        }

        static Sprite BuatSpriteBulat(bool cumaPinggir)
        {
            const int N = 64;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            float tengah = (N - 1) * 0.5f;
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float jarak = Vector2.Distance(new Vector2(x, y), new Vector2(tengah, tengah)) / (N * 0.5f);
                    bool tampil = jarak < 1f && (!cumaPinggir || jarak > 0.78f);
                    tex.SetPixel(x, y, tampil ? Color.white : Color.clear);
                }
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
