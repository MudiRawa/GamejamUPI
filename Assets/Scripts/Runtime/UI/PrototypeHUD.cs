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

        void DrawGravity()
        {
            if (gravity == null) return;
            string side = gravity.CurrentSide.ToString().ToUpperInvariant();
            int streak = gravity.ShiftStreak;

            GUI.Label(new Rect(18f, 16f, 520f, 26f), "GRAVITY  " + side + (streak > 1 ? "   SPAM x" + streak : ""), title);

            Color c = gravity.CurrentSide == GravitySide.Floor ? new Color(0.5f, 0.9f, 1f)
                    : gravity.CurrentSide == GravitySide.Ceiling ? new Color(1f, 0.85f, 0.4f)
                    : new Color(0.85f, 0.6f, 1f);
            GUI.color = c;
            GUI.Label(new Rect(18f, 40f, 520f, 20f), "1 Floor / 2 Left / 3 Ceiling / 4 Right  -  Q / E rotate", body);
            GUI.color = Color.white;
        }

        // ---------------- HUD BAR PUSING ----------------
        float pusingTampil;   // isi bar yang ditampilin (naiknya halus)
        float getar;          // bar geter pas pusing naik
        GUIStyle gayaKanan;   // tulisan rata kanan

        void DrawDizziness()
        {
            if (dizziness == null) return;
            if (gayaKanan == null)
            {
                gayaKanan = new GUIStyle(title);
                gayaKanan.alignment = TextAnchor.MiddleRight;
            }

            float n = dizziness.Normalized;
            float dt = Time.unscaledDeltaTime;

            // gerakin animasinya sekali per frame aja
            if (Event.current.type == EventType.Repaint)
            {
                if (n > pusingTampil + 0.01f) getar = 1f; // pusing naik -> bar geter
                pusingTampil = Mathf.Lerp(pusingTampil, n, 10f * dt);
                getar = Mathf.MoveTowards(getar, 0f, 3f * dt);
            }

            // warna: biru -> kuning -> merah
            Color warna = new Color(0.16f, 0.94f, 1f);
            string status = "STABIL";
            if (dizziness.IsHighTension)
            {
                warna = new Color(1f, 0.8f, 0.2f);
                status = "TEGANG";
            }
            if (dizziness.IsCritical)
            {
                warna = new Color(1f, 0.25f, 0.25f);
                status = "PANIK!";
            }

            // pas panik bar kedip-kedip
            Color warnaBar = warna;
            if (dizziness.IsCritical) warnaBar.a = 0.5f + Mathf.PingPong(Time.unscaledTime * 3f, 0.5f);

            // posisi di pojok kiri bawah, ikut geter
            float x = 30f + Random.Range(-1f, 1f) * getar * 6f;
            float y = Screen.height - 50f + Random.Range(-1f, 1f) * getar * 6f;

            // 20 kotak kecil
            int jumlah = 20;
            float lebar = 13f;
            float jarak = 3f;
            float tinggi = 14f;
            float panjang = jumlah * (lebar + jarak) - jarak;
            int nyala = Mathf.RoundToInt(pusingTampil * jumlah);

            for (int i = 0; i < jumlah; i++)
            {
                GUI.color = i < nyala ? warnaBar : new Color(1f, 1f, 1f, 0.15f);
                GUI.DrawTexture(new Rect(x + i * (lebar + jarak), y, lebar, tinggi), Texture2D.whiteTexture);
            }

            // garis batas TEGANG (50%) sama PANIK (85%)
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.DrawTexture(new Rect(x + panjang * 0.5f - 1f, y - 4f, 2f, tinggi + 8f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + panjang * 0.85f - 1f, y - 4f, 2f, tinggi + 8f), Texture2D.whiteTexture);

            // tulisan di atas bar
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y - 28f, 120f, 24f), "PUSING", title);
            GUI.color = warna;
            GUI.Label(new Rect(x + panjang - 200f, y - 28f, 200f, 24f), Mathf.RoundToInt(dizziness.value) + "%  " + status, gayaKanan);
            GUI.color = Color.white;
        }

        // ---------------- HUD PELURU ----------------
        Texture2D bulat;         // gambar lingkaran
        Texture2D cincin;        // gambar lingkaran kosong
        GUIStyle gayaAngka;
        float putar;             // putaran silinder sekarang
        float putarTujuan;       // putaran silinder yang dituju
        int tembakanTerakhir;
        float lepas;             // 1 = peluru baru kelempar keluar, 0 = udah ilang
        int lubangLepas;         // lubang mana yang pelurunya kelempar

        void DrawAmmo()
        {
            if (revolver == null) return;

            if (bulat == null)
            {
                bulat = BuatBulat(false);
                cincin = BuatBulat(true);
                gayaAngka = new GUIStyle(big);
                gayaAngka.alignment = TextAnchor.MiddleRight;
                gayaAngka.fontStyle = FontStyle.BoldAndItalic;
                tembakanTerakhir = revolver.ShotsFired;
            }

            int peluru = revolver.bullets;
            int maks = revolver.magazineSize;
            float dt = Time.unscaledDeltaTime;

            // tiap nembak: silinder muter satu lubang, pelurunya kelempar keluar
            if (revolver.ShotsFired != tembakanTerakhir)
            {
                tembakanTerakhir = revolver.ShotsFired;
                putarTujuan += 360f / maks;
                lubangLepas = peluru;
                lepas = 1f;
            }

            // gerakin animasinya sekali per frame aja
            if (Event.current.type == EventType.Repaint)
            {
                if (revolver.IsReloading) putarTujuan += 500f * dt; // pas reload silinder muter kenceng
                putar = Mathf.Lerp(putar, putarTujuan, 15f * dt);
                lepas = Mathf.MoveTowards(lepas, 0f, 2.5f * dt);
            }

            // pas reload, peluru keisi satu-satu
            int terisi = revolver.IsReloading ? Mathf.FloorToInt(revolver.ReloadProgress * maks) : peluru;

            // posisi silinder di pojok kanan bawah
            float cx = Screen.width - 110f;
            float cy = Screen.height - 105f;

            GUI.color = new Color(1f, 1f, 1f, 0.35f);
            GUI.DrawTexture(new Rect(cx - 60f, cy - 60f, 120f, 120f), cincin);

            for (int i = 0; i < maks; i++)
            {
                float sudut = (putar + i * 360f / maks - 90f) * Mathf.Deg2Rad;
                float x = cx + Mathf.Cos(sudut) * 36f;
                float y = cy + Mathf.Sin(sudut) * 36f;

                if (i < terisi)
                {
                    GUI.color = new Color(1f, 0.82f, 0.23f); // kuning
                    GUI.DrawTexture(new Rect(x - 14f, y - 14f, 28f, 28f), bulat);
                }
                else
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.35f);
                    GUI.DrawTexture(new Rect(x - 14f, y - 14f, 28f, 28f), cincin);
                }

                // peluru yang baru ditembak kelempar keluar terus ilang
                if (i == lubangLepas && lepas > 0f)
                {
                    float jauh = (1f - lepas) * 70f;
                    float lx = x + Mathf.Cos(sudut) * jauh;
                    float ly = y + Mathf.Sin(sudut) * jauh;
                    GUI.color = new Color(1f, 0.82f, 0.23f, lepas);
                    GUI.DrawTexture(new Rect(lx - 14f, ly - 14f, 28f, 28f), bulat);
                }
            }

            // angka peluru, merah kalau habis
            GUI.color = peluru == 0 ? new Color(1f, 0.25f, 0.25f) : Color.white;
            GUI.Label(new Rect(cx - 260f, cy - 40f, 120f, 70f), peluru.ToString(), gayaAngka);
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            GUI.Label(new Rect(cx - 140f, cy - 10f, 60f, 40f), "/ " + maks, body);

            // tulisan kecil di atas angka
            string tulisan = "REVOLVER";
            if (revolver.IsReloading) tulisan = "RELOAD...";
            else if (peluru == 0) tulisan = "[R] RELOAD";
            GUI.color = Color.white;
            GUI.Label(new Rect(cx - 230f, cy - 65f, 150f, 22f), tulisan, body);
            GUI.color = Color.white;
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
