using UnityEngine;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    // gaya sci-fi buat 3 panel HUD: Pusing (kiri atas), Wave (tengah atas), Darah (kiri bawah).
    // ngikutin gaya panel peluru buatan lead: pelat logam gelap + garis cyan nyala + angka Tektur gede.
    //
    // dipanggil sekali dari editor (Tools > Vertigo Shift > UI > Pasang Gaya Sci-Fi HUD),
    // hasilnya kesimpen di scene. objek & nama yang dipakai script HUD lead nggak diganti,
    // cuma posisi, ukuran, sama gambarnya, jadi semua script HUD tetep jalan kayak biasa.
    public static class HUDSciFi
    {
        static readonly Color CyanGaris = new Color(0.16f, 0.94f, 1f, 0.6f);
        const float Glow = 14f; // ruang glow di sekeliling gambar pelat

        public struct Gambar
        {
            public Sprite platKiriAtas, platKiriBawah, platTengah;
            public Sprite lekukPusing, lekukDarah;
            public Sprite segmenPusing, segmenDarah;
        }

        public static void Pasang(Canvas hud, Gambar g)
        {
            Transform c = hud.transform;
            Pusing(c, g);
            Wave(c, g);
            Darah(c, g);
        }

        // ------------------------------------------------ KIRI ATAS: PUSING + GRAVITY
        static void Pusing(Transform c, Gambar g)
        {
            RectTransform p = c.Find("Panel_Pusing") as RectTransform;
            if (p == null) return;
            Taruh(p, new Vector2(0f, 1f), new Vector2(24f + 165f, -(20f + 63f)), new Vector2(330f, 126f));
            Plat(p, g.platKiriAtas);

            // isi panel, dihitung dari pojok kiri bawah panel (330 x 126)
            Vector2 kb = Vector2.zero;
            Teks(p, "Lbl_Pusing", kb, new Vector2(92f, 104f), new Vector2(140f, 20f), TextAnchor.MiddleLeft);
            Teks(p, "Txt_Status", kb, new Vector2(238f, 104f), new Vector2(140f, 20f), TextAnchor.MiddleRight);
            Taruh(p, "Garis_Label", kb, new Vector2(162f, 91f), new Vector2(280f, 2f));
            Taruh(p, "Ujung_Garis", kb, new Vector2(305f, 91f), new Vector2(6f, 8f));
            Teks(p, "Txt_Angka", kb, new Vector2(72f, 66f), new Vector2(100f, 46f), TextAnchor.MiddleRight);
            Teks(p, "Txt_Persen", kb, new Vector2(146f, 60f), new Vector2(40f, 24f), TextAnchor.MiddleLeft);

            // bar: cekungan gelap, segmen kosong (redup), segmen isi (diwarnain DizzinessDisplay)
            Transform latar = p.Find("Bar_Latar");
            Image lekuk = Anak(p, "Lekuk_Bar", g.lekukPusing, Color.white);
            Taruh((RectTransform)lekuk.transform, kb, new Vector2(165f, 38f), new Vector2(290f, 16f));
            if (latar != null) lekuk.transform.SetSiblingIndex(latar.GetSiblingIndex());

            Image barLatar = Ambil(p, "Bar_Latar");
            if (barLatar != null)
            {
                barLatar.sprite = g.segmenPusing;
                barLatar.type = Image.Type.Simple;
                Taruh(barLatar.rectTransform, kb, new Vector2(165f, 38f), new Vector2(286f, 12f));
            }
            Image barIsi = Ambil(p, "Bar_Isi");
            if (barIsi != null)
            {
                barIsi.sprite = g.segmenPusing;
                barIsi.type = Image.Type.Filled;
                barIsi.fillMethod = Image.FillMethod.Horizontal;
                barIsi.fillOrigin = (int)Image.OriginHorizontal.Left;
                Taruh(barIsi.rectTransform, kb, new Vector2(165f, 38f), new Vector2(286f, 12f));
            }
            Taruh(p, "Tanda_50", kb, new Vector2(22f + 286f * 0.5f, 38f), new Vector2(2f, 18f));
            Taruh(p, "Tanda_85", kb, new Vector2(22f + 286f * 0.85f, 38f), new Vector2(2f, 18f));

            // tulisan gravity (anak langsung canvas) ditaruh di baris paling bawah pelat,
            // urutannya dipindah ke setelah panel biar nggak ketutup pelat
            RectTransform grav = c.Find("Txt_Gravity") as RectTransform;
            if (grav != null)
            {
                Taruh(grav, new Vector2(0f, 1f), new Vector2(24f + 22f + 143f, -(20f + 126f - 14f)), new Vector2(286f, 18f));
                Text tg = grav.GetComponent<Text>();
                if (tg != null) tg.alignment = TextAnchor.MiddleLeft;
                grav.SetSiblingIndex(p.GetSiblingIndex() + 1);
            }
        }

        // ------------------------------------------------ TENGAH ATAS: WAVE
        static void Wave(Transform c, Gambar g)
        {
            RectTransform p = c.Find("Panel_Wave") as RectTransform;
            if (p == null) return;
            Taruh(p, new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(300f, 110f)); // nempel atas layar
            Plat(p, g.platTengah);

            Vector2 bt = new Vector2(0.5f, 0f), kb = Vector2.zero;
            Teks(p, "Lbl_Wave", bt, new Vector2(0f, 88f), new Vector2(200f, 18f), TextAnchor.MiddleCenter);
            Teks(p, "Txt_Wave", bt, new Vector2(0f, 58f), new Vector2(200f, 46f), TextAnchor.MiddleCenter);
            Teks(p, "Txt_Sisa", bt, new Vector2(0f, 24f), new Vector2(200f, 22f), TextAnchor.MiddleCenter);
            // garis kiri-kanan ngapit nomor wave
            Taruh(p, "Garis_Kiri", kb, new Vector2(75f, 58f), new Vector2(50f, 2f));
            Taruh(p, "Garis_Kanan", kb, new Vector2(225f, 58f), new Vector2(50f, 2f));
            Taruh(p, "Ujung_Kiri", kb, new Vector2(47f, 58f), new Vector2(6f, 8f));
            Taruh(p, "Ujung_Kanan", kb, new Vector2(253f, 58f), new Vector2(6f, 8f));
        }

        // ------------------------------------------------ KIRI BAWAH: DARAH
        static void Darah(Transform c, Gambar g)
        {
            RectTransform p = c.Find("Panel_Darah") as RectTransform;
            if (p == null) return;
            Taruh(p, Vector2.zero, new Vector2(24f + 170f, 20f + 42f), new Vector2(340f, 84f));
            Image kotakLama = p.GetComponent<Image>();
            if (kotakLama != null) kotakLama.enabled = false; // kotak hitam lama diganti pelat
            Plat(p, g.platKiriBawah);

            Vector2 ka = new Vector2(0f, 1f), kna = new Vector2(1f, 1f), kb = Vector2.zero;
            Teks(p, "Txt_Label", ka, new Vector2(86f, -25f), new Vector2(128f, 26f), TextAnchor.MiddleLeft);
            Teks(p, "Txt_Darah", kna, new Vector2(-101f, -29f), new Vector2(158f, 46f), TextAnchor.MiddleRight);

            // garis + ujung di bawah tulisan DARAH (kayak REVOLVER di panel peluru)
            Image garis = Anak(p, "Garis_Darah", null, CyanGaris);
            Taruh((RectTransform)garis.transform, ka, new Vector2(86f, -39f), new Vector2(128f, 2f));
            Image ujung = Anak(p, "Ujung_Darah", null, CyanGaris);
            Taruh((RectTransform)ujung.transform, ka, new Vector2(153f, -39f), new Vector2(6f, 8f));

            Transform sl = p.Find("Slider_Darah");
            Image lekuk = Anak(p, "Lekuk_Darah", g.lekukDarah, Color.white);
            Taruh((RectTransform)lekuk.transform, kb, new Vector2(170f, 21f), new Vector2(300f, 18f));
            if (sl != null)
            {
                lekuk.transform.SetSiblingIndex(sl.GetSiblingIndex());
                RectTransform rs = (RectTransform)sl;
                rs.anchorMin = new Vector2(0f, 0f);
                rs.anchorMax = new Vector2(1f, 0f);
                rs.pivot = new Vector2(0.5f, 0.5f);
                rs.anchoredPosition = new Vector2(0f, 21f);
                rs.sizeDelta = new Vector2(-44f, 14f);

                // segmen diulang (Tiled) biar nggak melar pas darah berkurang
                Image bg = Ambil(sl, "Background");
                if (bg != null) { bg.sprite = g.segmenDarah; bg.type = Image.Type.Tiled; }
                Slider slider = sl.GetComponent<Slider>();
                Image isi = slider != null && slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : Ambil(sl, "Fill");
                if (isi != null) { isi.sprite = g.segmenDarah; isi.type = Image.Type.Tiled; }
            }
        }

        // ------------------------------------------------ alat bantu
        // pelat di belakang semua isi panel, dilebihin dikit buat glow
        static void Plat(RectTransform panel, Sprite s)
        {
            Image plat = Anak(panel, "Plat_SciFi", s, Color.white);
            RectTransform r = plat.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = new Vector2(-Glow, -Glow);
            r.offsetMax = new Vector2(Glow, Glow);
            plat.transform.SetAsFirstSibling();
        }

        // ambil anak Image, kalau belum ada dibikin
        static Image Anak(Transform induk, string nama, Sprite s, Color warna)
        {
            Transform t = induk.Find(nama);
            if (t == null)
            {
                GameObject go = new GameObject(nama, typeof(RectTransform));
                go.layer = induk.gameObject.layer;
                t = go.transform;
                t.SetParent(induk, false);
            }
            Image img = t.GetComponent<Image>();
            if (img == null) img = t.gameObject.AddComponent<Image>();
            img.sprite = s;
            img.color = warna;
            img.raycastTarget = false;
            return img;
        }

        static Image Ambil(Transform induk, string nama)
        {
            Transform t = induk.Find(nama);
            return t != null ? t.GetComponent<Image>() : null;
        }

        static void Teks(Transform induk, string nama, Vector2 jangkar, Vector2 tengah, Vector2 ukuran, TextAnchor rata)
        {
            Transform t = induk.Find(nama);
            if (t == null) return;
            Taruh((RectTransform)t, jangkar, tengah, ukuran);
            Text teks = t.GetComponent<Text>();
            if (teks == null) return;
            teks.alignment = rata;
            teks.horizontalOverflow = HorizontalWrapMode.Overflow; // font Tektur suka kepotong kalau kotaknya pas-pasan
            teks.verticalOverflow = VerticalWrapMode.Overflow;
        }

        static void Taruh(Transform induk, string nama, Vector2 jangkar, Vector2 tengah, Vector2 ukuran)
        {
            Transform t = induk.Find(nama);
            if (t != null) Taruh((RectTransform)t, jangkar, tengah, ukuran);
        }

        static void Taruh(RectTransform r, Vector2 jangkar, Vector2 tengah, Vector2 ukuran)
        {
            r.anchorMin = jangkar;
            r.anchorMax = jangkar;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = tengah;
            r.sizeDelta = ukuran;
        }
    }
}
