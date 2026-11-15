using UnityEngine;
using VertigoShift.Gameplay.Gravity;
using VertigoShift.Gameplay.Panic;
using VertigoShift.Gameplay.Player;
using VertigoShift.Gameplay.Weapons;

namespace VertigoShift.UI
{
    /// <summary>
    /// Debug HUD (IMGUI - no prefab wiring needed).
    /// Shows ammo, reload progress, active gravity side, the dizziness meter and the controls.
    /// </summary>
    public class PrototypeHUD : MonoBehaviour
    {
        public Revolver revolver;
        public GravityShift gravity;
        public DizzinessSystem dizziness;
        public PlayerMotor motor;

        [Header("Gambar UI Peluru (boleh kosong)")]
        public Texture2D gambarTabung;
        public Texture2D gambarPeluru;
        public Texture2D gambarLubang;

        [Header("Font Sci-fi (boleh kosong)")]
        public Font fontAngka;      // buat angka peluru
        public Font fontTulisan;    // buat tulisan kecil

        GUIStyle title;
        GUIStyle body;
        GUIStyle big;

        void EnsureStyles()
        {
            if (title != null) return;

            title = new GUIStyle(GUI.skin.label);
            title.fontSize = 17;
            title.fontStyle = FontStyle.Bold;
            title.normal.textColor = new Color(0.75f, 0.95f, 1f);

            body = new GUIStyle(title);
            body.fontSize = 13;
            body.fontStyle = FontStyle.Normal;
            body.normal.textColor = new Color(0.68f, 0.78f, 0.88f);

            big = new GUIStyle(title);
            big.fontSize = 32;
        }

        void OnGUI()
        {
            EnsureStyles();

            DrawCrosshair();
            DrawGravity();
            DrawDizziness();
            DrawAmmo();
            DrawHelp();
            DrawStats();
        }

        void DrawCrosshair()
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            GUI.color = new Color(0.8f, 0.95f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(center.x - 7f, center.y - 1f, 14f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(center.x - 1f, center.y - 7f, 2f, 14f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        GUIStyle gayaKiri; // tulisan kecil rata kiri

        // tulisan gravity kecil di bawah panel pusing
        void DrawGravity()
        {
            if (gravity == null) return;
            if (gayaKiri == null) gayaKiri = BuatGaya(fontTulisan, 14, TextAnchor.MiddleLeft);
            string side = gravity.CurrentSide.ToString().ToUpperInvariant();
            int streak = gravity.ShiftStreak;

            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            GUI.Label(new Rect(30f, 112f, 300f, 20f), "GRAVITY  " + side + (streak > 1 ? "  x" + streak : ""), gayaKiri);
            GUI.color = Color.white;
        }

        // ---------------- HUD PUSING (kiri atas) ----------------
        // gayanya sama kayak HUD revolver: label kecil cyan, garis tipis, angka gede, bar tipis
        float pusingTampil;   // isi bar yang ditampilin (naiknya halus)
        float getar;          // geter pas pusing naik
        GUIStyle gayaPusingAngka, gayaPusingKecil, gayaStatus;

        void DrawDizziness()
        {
            if (dizziness == null) return;
            if (gayaPusingAngka == null)
            {
                gayaPusingAngka = BuatGaya(fontAngka, 40, TextAnchor.MiddleLeft);
                gayaPusingKecil = BuatGaya(fontTulisan, 16, TextAnchor.MiddleLeft);
                gayaStatus = BuatGaya(fontTulisan, 14, TextAnchor.MiddleRight);
                if (gayaKiri == null) gayaKiri = BuatGaya(fontTulisan, 14, TextAnchor.MiddleLeft);
            }

            float n = dizziness.Normalized;
            float dt = Time.unscaledDeltaTime;
            if (Event.current.type == EventType.Repaint)
            {
                if (n > pusingTampil + 0.01f) getar = 1f;
                pusingTampil = Mathf.Lerp(pusingTampil, n, 10f * dt);
                getar = Mathf.MoveTowards(getar, 0f, 3f * dt);
            }

            Color cyan = new Color(0.16f, 0.94f, 1f);
            Color warna = cyan;
            string status = "STABIL";
            if (dizziness.IsHighTension) { warna = new Color(1f, 0.82f, 0.23f); status = "TEGANG"; }
            if (dizziness.IsCritical)
            {
                warna = new Color(1f, 0.25f, 0.25f);
                status = "PANIK";
                warna.a = 0.4f + Mathf.PingPong(Time.unscaledTime * 3f, 0.6f);
            }

            float x = 30f + Random.Range(-1f, 1f) * getar * 4f;
            float y = 18f + Random.Range(-1f, 1f) * getar * 4f;
            float panjang = 220f;

            // label + garis
            TulisGlow(new Rect(x, y, 120f, 20f), "PUSING", gayaKiri, cyan);
            GUI.color = new Color(cyan.r, cyan.g, cyan.b, 0.6f);
            GUI.DrawTexture(new Rect(x, y + 22f, panjang, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + panjang, y + 19f, 6f, 8f), Texture2D.whiteTexture);

            // angka persen + status
            string angka = Mathf.RoundToInt(dizziness.value).ToString();
            TulisGlow(new Rect(x, y + 26f, 120f, 46f), angka, gayaPusingAngka, Color.white);
            float lebarAngka = gayaPusingAngka.CalcSize(new GUIContent(angka)).x;
            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            GUI.Label(new Rect(x + lebarAngka + 4f, y + 40f, 40f, 24f), "%", gayaPusingKecil);
            GUI.color = warna;
            GUI.Label(new Rect(x + panjang - 120f, y + 38f, 120f, 24f), status, gayaStatus);

            // bar tipis + takik 50% & 85%
            float by = y + 76f;
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUI.DrawTexture(new Rect(x, by, panjang, 4f), Texture2D.whiteTexture);
            GUI.color = warna;
            GUI.DrawTexture(new Rect(x, by, panjang * pusingTampil, 4f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            GUI.DrawTexture(new Rect(x + panjang * 0.5f - 1f, by - 3f, 2f, 10f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + panjang * 0.85f - 1f, by - 3f, 2f, 10f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // ---------------- HUD PELURU ----------------
        Texture2D bulat;         // gambar lingkaran
        Texture2D cincin;        // gambar lingkaran kosong
        GUIStyle gayaAngka;
        GUIStyle gayaKecil;
        GUIStyle gayaLabel;
        float putar;             // putaran tabung sekarang
        float putarTujuan;       // putaran tabung yang dituju
        int tembakanTerakhir;
        float lepas;             // 1 = peluru baru kelempar keluar, 0 = udah ilang
        bool tadiReload;

        void DrawAmmo()
        {
            if (revolver == null) return;

            if (bulat == null)
            {
                bulat = BuatBulat(false);
                cincin = BuatBulat(true);
                gayaAngka = BuatGaya(fontAngka, 56, TextAnchor.MiddleRight);
                gayaKecil = BuatGaya(fontTulisan, 22, TextAnchor.MiddleLeft);
                gayaLabel = BuatGaya(fontTulisan, 14, TextAnchor.MiddleRight);
                tembakanTerakhir = revolver.ShotsFired;
                putar = putarTujuan = 360f / revolver.magazineSize; // lubang yang mau ditembak ada di atas
            }

            int peluru = revolver.bullets;
            int maks = revolver.magazineSize;
            float dt = Time.unscaledDeltaTime;

            // tiap nembak: tabung muter satu lubang, pelurunya kelempar keluar
            if (revolver.ShotsFired != tembakanTerakhir)
            {
                tembakanTerakhir = revolver.ShotsFired;
                putarTujuan += 360f / maks;
                lepas = 1f;
            }

            // gerakin animasinya sekali per frame aja
            if (Event.current.type == EventType.Repaint)
            {
                if (revolver.IsReloading) putarTujuan += 500f * dt; // pas reload tabung muter kenceng

                // reload selesai: lurusin lagi biar peluru yang mau ditembak pas di atas
                if (tadiReload && !revolver.IsReloading)
                {
                    float satu = 360f / maks;
                    putarTujuan = Mathf.Round((putarTujuan - satu) / 360f) * 360f + satu;
                }
                tadiReload = revolver.IsReloading;
                putar = Mathf.Lerp(putar, putarTujuan, 15f * dt);
                lepas = Mathf.MoveTowards(lepas, 0f, 2.5f * dt);
            }

            // pas reload, peluru keisi satu-satu
            int terisi = revolver.IsReloading ? Mathf.FloorToInt(revolver.ReloadProgress * maks) : peluru;

            // posisi tabung di pojok kanan bawah
            float cx = Screen.width - 110f;
            float cy = Screen.height - 105f;

            // pakai gambar kalau ada, kalau nggak pakai lingkaran dari kode
            Texture2D texPeluru = gambarPeluru != null ? gambarPeluru : bulat;
            Texture2D texLubang = gambarLubang != null ? gambarLubang : cincin;

            if (gambarTabung != null)
            {
                // gambar tabung ikut muter
                Matrix4x4 awal = GUI.matrix;
                GUIUtility.RotateAroundPivot(putar, new Vector2(cx, cy));
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(cx - 60f, cy - 60f, 120f, 120f), gambarTabung);
                GUI.matrix = awal;
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
                GUI.DrawTexture(new Rect(cx - 60f, cy - 60f, 120f, 120f), cincin);
            }

            for (int i = 0; i < maks; i++)
            {
                float sudut = (putar + i * 360f / maks - 90f) * Mathf.Deg2Rad;
                float x = cx + Mathf.Cos(sudut) * 36f;
                float y = cy + Mathf.Sin(sudut) * 36f;

                if (i < terisi)
                {
                    GUI.color = gambarPeluru != null ? Color.white : new Color(1f, 0.82f, 0.23f); // kuning
                    GUI.DrawTexture(new Rect(x - 14f, y - 14f, 28f, 28f), texPeluru);
                }
                else
                {
                    GUI.color = gambarLubang != null ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                    GUI.DrawTexture(new Rect(x - 14f, y - 14f, 28f, 28f), texLubang);
                }
            }

            // peluru yang baru ditembak keluar dari lubang paling atas, mental ke atas terus ilang
            if (lepas > 0f)
            {
                float k = 1f - lepas;
                float lx = cx + k * 30f;
                float ly = cy - 36f - k * 70f + k * k * 50f;
                GUI.color = gambarPeluru != null ? new Color(1f, 1f, 1f, lepas) : new Color(1f, 0.82f, 0.23f, lepas);
                GUI.DrawTexture(new Rect(lx - 14f, ly - 14f, 28f, 28f), texPeluru);
            }

            // ---- tulisan di kiri tabung
            Color cyan = new Color(0.16f, 0.94f, 1f);
            Color kuning = new Color(1f, 0.82f, 0.23f);
            Color merah = new Color(1f, 0.25f, 0.25f);
            float kanan = cx - 78f; // ujung kanan tulisan

            // label kecil di atas
            string tulisan = "REVOLVER";
            Color warnaTulisan = cyan;
            if (revolver.IsReloading)
            {
                tulisan = "RELOAD";
                warnaTulisan = kuning;
            }
            else if (peluru == 0)
            {
                tulisan = "[R] RELOAD";
                warnaTulisan = merah;
                warnaTulisan.a = 0.4f + Mathf.PingPong(Time.unscaledTime * 3f, 0.6f); // kedip
            }
            TulisGlow(new Rect(kanan - 170f, cy - 64f, 170f, 20f), tulisan, gayaLabel, warnaTulisan);

            // garis tipis di bawah label + kotak kecil di ujungnya
            GUI.color = new Color(cyan.r, cyan.g, cyan.b, 0.6f);
            GUI.DrawTexture(new Rect(kanan - 170f, cy - 42f, 170f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(kanan - 6f, cy - 45f, 6f, 8f), Texture2D.whiteTexture);

            // angka besar + "/ 6"
            TulisGlow(new Rect(kanan - 170f, cy - 36f, 120f, 64f), peluru.ToString(), gayaAngka, peluru == 0 ? merah : Color.white);
            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            GUI.Label(new Rect(kanan - 46f, cy - 8f, 60f, 30f), "/" + maks, gayaKecil);
            GUI.color = Color.white;
        }

        // tulisan dengan cahaya cyan tipis di belakangnya
        void TulisGlow(Rect r, string teks, GUIStyle gaya, Color warna)
        {
            GUI.color = new Color(0.16f, 0.94f, 1f, 0.25f * warna.a);
            GUI.Label(new Rect(r.x - 2f, r.y, r.width, r.height), teks, gaya);
            GUI.Label(new Rect(r.x + 2f, r.y, r.width, r.height), teks, gaya);
            GUI.Label(new Rect(r.x, r.y - 2f, r.width, r.height), teks, gaya);
            GUI.Label(new Rect(r.x, r.y + 2f, r.width, r.height), teks, gaya);
            GUI.color = warna;
            GUI.Label(r, teks, gaya);
            GUI.color = Color.white;
        }

        // bikin gaya tulisan, pakai font sci-fi kalau ada
        static GUIStyle BuatGaya(Font font, int ukuran, TextAnchor rata)
        {
            GUIStyle gaya = new GUIStyle(GUI.skin.label);
            gaya.font = font;
            gaya.fontSize = ukuran;
            gaya.alignment = rata;
            gaya.normal.textColor = Color.white;
            if (font == null) gaya.fontStyle = FontStyle.Bold;
            return gaya;
        }

        // bikin gambar lingkaran, penuh atau cuma pinggirnya
        static Texture2D BuatBulat(bool cumaPinggir)
        {
            Texture2D tex = new Texture2D(64, 64);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float jarak = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 32f;
                    bool kelihatan = jarak < 1f && (!cumaPinggir || jarak > 0.8f);
                    tex.SetPixel(x, y, kelihatan ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        void DrawHelp()
        {
            // petunjuk tombol cuma muncul pas F1 ditahan, biar layar bersih
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            bool tahanF1 = keyboard != null && keyboard.f1Key.isPressed;
            if (!tahanF1)
            {
                GUI.Label(new Rect(Screen.width * 0.5f - 60f, Screen.height - 30f, 120f, 20f), "[F1] KONTROL", body);
                return;
            }

            GUI.Label(new Rect(18f, Screen.height - 160f, 640f, 80f),
                "WASD move   SHIFT run   CTRL/C crouch + slide   SPACE jump   LMB fire   R reload\n" +
                "GRAVITY SHIFT: 1 floor / 2 left wall / 3 ceiling / 4 right wall  (Q / E rotate)   ESC cursor",
                body);
        }

        void DrawStats()
        {
            // info debug, cuma muncul pas F1 ditahan biar layar bersih
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null || !keyboard.f1Key.isPressed) return;
            string speed = motor != null ? motor.CurrentSpeed.ToString("0.0") + " m/s" : "-";
            string grounded = motor != null ? (motor.IsGrounded ? "GROUND" : "AIR") : "-";
            string slide = motor != null && motor.IsSliding ? "  SLIDE" : "";
            GUI.Label(new Rect(Screen.width - 240f, 16f, 220f, 22f), speed + "  " + grounded + slide, body);
        }

        static void DrawBar(Rect rect, float fill, Color color)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fill), rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
