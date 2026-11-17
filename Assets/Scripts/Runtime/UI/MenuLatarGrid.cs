using UnityEngine;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    /// <summary>
    /// Latar grid gelap yang bergerak pelan (pengganti GambarLatar versi IMGUI).
    /// Digambar langsung di Canvas supaya main menu tetap terlihat sama
    /// seperti versi lama walau tidak memakai OnGUI lagi.
    /// </summary>
    public class MenuLatarGrid : MaskableGraphic
    {
        [Header("Grid")]
        public float jarak = 80f;
        public float tebal = 1f;
        public float kecepatan = 12f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect r = GetPixelAdjustedRect();
            if (r.width <= 0f || r.height <= 0f) return;

            float spasi = Mathf.Max(8f, jarak);
            float garis = Mathf.Max(1f, tebal);
            float jalan = Mathf.Repeat(Time.unscaledTime * kecepatan, spasi);
            Color32 warna = color;

            // Garis vertikal (bergerak ke kiri)
            for (float x = r.xMin - jalan; x < r.xMax; x += spasi)
            {
                float x0 = Mathf.Max(x, r.xMin);
                float x1 = Mathf.Min(x + garis, r.xMax);
                if (x1 > x0) TambahKotak(vh, x0, r.yMin, x1, r.yMax, warna);
            }

            // Garis horizontal (bergerak ke bawah)
            for (float y = r.yMax - jalan; y > r.yMin; y -= spasi)
            {
                float y0 = Mathf.Max(y - garis, r.yMin);
                float y1 = Mathf.Min(y, r.yMax);
                if (y1 > y0) TambahKotak(vh, r.xMin, y0, r.xMax, y1, warna);
            }
        }

        static void TambahKotak(VertexHelper vh, float x0, float y0, float x1, float y1, Color32 warna)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(x0, y0, 0f), warna, Vector2.zero);
            vh.AddVert(new Vector3(x0, y1, 0f), warna, Vector2.zero);
            vh.AddVert(new Vector3(x1, y1, 0f), warna, Vector2.zero);
            vh.AddVert(new Vector3(x1, y0, 0f), warna, Vector2.zero);

            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        void Update()
        {
            // Grid bergerak pelan, jadi mesh perlu diperbarui tiap frame.
            SetVerticesDirty();
        }
    }
}
