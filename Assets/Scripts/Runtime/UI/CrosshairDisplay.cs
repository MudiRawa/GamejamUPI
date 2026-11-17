using UnityEngine;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    /// <summary>
    /// Crosshair di tengah layar.
    /// Kalau objek Crosshair_* sudah ada di Canvas scene, script ini hanya memakainya.
    /// Kalau belum ada, script membuatnya otomatis saat Start.
    /// </summary>
    public class CrosshairDisplay : MonoBehaviour
    {
        public UITheme theme;
        public Canvas canvas;

        Image garisAtas, garisBawah, garisKiri, garisKanan;

        void Start()
        {
            canvas = UIBind.AmbilCanvas(canvas, "Canvas_HUD");
            if (!CobaBind()) BuatCrosshair();
        }

        /// <summary>Dipanggil oleh editor builder untuk membuat UI langsung di scene.</summary>
        public void BangunUI(Canvas c, UITheme t)
        {
            canvas = c;
            theme = t;
            BuatCrosshair();
        }

        bool CobaBind()
        {
            if (canvas == null) return false;
            RectTransform atas = UIBind.Rect(canvas.transform, "Crosshair_Atas");
            if (atas == null) return false;

            garisAtas = atas.GetComponent<Image>();
            garisBawah = UIBind.Gambar(canvas.transform, "Crosshair_Bawah");
            garisKiri = UIBind.Gambar(canvas.transform, "Crosshair_Kiri");
            garisKanan = UIBind.Gambar(canvas.transform, "Crosshair_Kanan");
            return true;
        }

        void BuatCrosshair()
        {
            float ukuran = 14f;
            float tebal = 2f;
            Color warna = theme != null ? theme.cyan : new Color(0.8f, 0.95f, 1f);
            warna.a = 0.85f;

            Vector2 anchor = new Vector2(0.5f, 0.5f);

            garisAtas = UIFactory.BuatImage(canvas.transform, "Crosshair_Atas", null, warna,
                anchor, anchor,
                new Vector2(-ukuran * 0.5f, -ukuran), new Vector2(ukuran * 0.5f, -ukuran + tebal));
            garisBawah = UIFactory.BuatImage(canvas.transform, "Crosshair_Bawah", null, warna,
                anchor, anchor,
                new Vector2(-ukuran * 0.5f, ukuran - tebal), new Vector2(ukuran * 0.5f, ukuran));
            garisKiri = UIFactory.BuatImage(canvas.transform, "Crosshair_Kiri", null, warna,
                anchor, anchor,
                new Vector2(-ukuran, -tebal * 0.5f), new Vector2(-ukuran + tebal, tebal * 0.5f));
            garisKanan = UIFactory.BuatImage(canvas.transform, "Crosshair_Kanan", null, warna,
                anchor, anchor,
                new Vector2(ukuran - tebal, -tebal * 0.5f), new Vector2(ukuran, tebal * 0.5f));
        }
    }
}
