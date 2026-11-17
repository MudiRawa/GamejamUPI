using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    /// <summary>
    /// Helper untuk mencari elemen UI yang sudah ada di dalam scene.
    /// Dipakai supaya UI yang dibangun lewat editor (Canvas asli di scene)
    /// bisa dipakai ulang saat runtime tanpa membuat duplikat.
    /// </summary>
    public static class UIBind
    {
        /// <summary>Ambil canvas yang sudah ada di scene, atau buat baru kalau tidak ada.</summary>
        public static Canvas AmbilCanvas(Canvas canvas, string namaDefault)
        {
            if (canvas != null) return canvas;

            GameObject go = GameObject.Find(namaDefault);
            if (go != null)
            {
                Canvas c = go.GetComponent<Canvas>();
                if (c != null) return c;
            }

            return UIFactory.BuatCanvas(namaDefault);
        }

        public static Transform Cari(Transform induk, string nama)
        {
            if (induk == null) return null;
            return induk.Find(nama);
        }

        public static Text Teks(Transform induk, string nama)
        {
            Transform t = Cari(induk, nama);
            return t != null ? t.GetComponent<Text>() : null;
        }

        public static Image Gambar(Transform induk, string nama)
        {
            Transform t = Cari(induk, nama);
            return t != null ? t.GetComponent<Image>() : null;
        }

        public static Button Tombol(Transform induk, string nama)
        {
            Transform t = Cari(induk, nama);
            return t != null ? t.GetComponent<Button>() : null;
        }

        public static Slider Slider(Transform induk, string nama)
        {
            Transform t = Cari(induk, nama);
            return t != null ? t.GetComponent<Slider>() : null;
        }

        public static RectTransform Rect(Transform induk, string nama)
        {
            return Cari(induk, nama) as RectTransform;
        }

        /// <summary>Pastikan ada EventSystem supaya tombol & slider bisa diklik.</summary>
        public static void PastikanEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
