using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    // ngerapiin HUD in-game tanpa ngubah desain & script HUD punya lead.
    // nggak usah dipasang, jalan sendiri tiap scene kebuka (cuma ngefek kalau ada Canvas_HUD).
    //
    // yang dibenerin:
    //   1. angka yang ilang (wave, darah, pusing, peluru) -> teksnya kepotong karena kotaknya kependekan
    //      buat font Tektur. sekarang teks dibolehin keluar kotak, jadi selalu keliatan
    //   2. tulisan susah dibaca di tembok yang terang -> tiap teks dikasih bayangan gelap tipis
    //   3. tulisan kekecilan -> HUD digedein 1.2x (posisi tetep sama, cuma skalanya)
    //   4. panel pusing, wave, sama peluru dikasih latar gelap transparan kayak panel darah,
    //      biar seragam & kebaca di mana aja
    //   5. angka peluru & darah digedein biar kebaca sekilas pas lagi perang
    public class HUDRapi : MonoBehaviour
    {
        const float SkalaHUD = 1.2f;
        static readonly Color WarnaLatar = new Color(0.02f, 0.03f, 0.05f, 0.55f);
        static readonly Color WarnaBayangan = new Color(0f, 0f, 0f, 0.75f);

        // panel yang dikasih latar + seberapa lebar latarnya keluar dari panel (kiri, bawah, kanan, atas)
        static readonly string[] PanelDiLatarin = { "Panel_Pusing", "Panel_Wave", "Panel_Ammo" };
        static readonly Vector4 Jarak = new Vector4(14f, 10f, 14f, 10f);

        static HUDRapi ada;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Mulai()
        {
            if (ada != null) return;
            GameObject go = new GameObject("HUDRapi");
            DontDestroyOnLoad(go);
            ada = go.AddComponent<HUDRapi>();
            SceneManager.sceneLoaded += (s, m) => { if (ada != null) ada.StartCoroutine(ada.Rapikan()); };
            ada.StartCoroutine(ada.Rapikan());
        }

        IEnumerator Rapikan()
        {
            // tunggu 2 frame: script HUD lead bikin/nyari UI-nya dulu di Start
            yield return null;
            yield return null;

            GameObject hud = GameObject.Find("Canvas_HUD");
            if (hud == null) yield break;

            // 3. gedein HUD
            CanvasScaler skala = hud.GetComponent<CanvasScaler>();
            if (skala != null && skala.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                skala.referenceResolution = new Vector2(1920f, 1080f) / SkalaHUD;

            // 4. latar gelap di belakang panel
            foreach (string nama in PanelDiLatarin)
            {
                Transform panel = Cari(hud.transform, nama);
                if (panel == null || panel.Find("Latar_Rapi") != null) continue;
                Image latar = new GameObject("Latar_Rapi", typeof(RectTransform)).AddComponent<Image>();
                latar.transform.SetParent(panel, false);
                latar.transform.SetAsFirstSibling();
                latar.color = WarnaLatar;
                latar.raycastTarget = false;
                RectTransform r = latar.rectTransform;
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(-Jarak.x, -Jarak.y);
                r.offsetMax = new Vector2(Jarak.z, Jarak.w);
            }

            // 5. angka penting digedein
            Gedein(hud.transform, "Txt_Jumlah", 44);  // sisa peluru
            Gedein(hud.transform, "Txt_Darah", 24);   // angka darah
            Gedein(hud.transform, "Txt_Wave", 36);    // nomor wave

            // 1 & 2. semua teks: jangan kepotong + kasih bayangan
            RapikanTeks(hud.transform);
            GameObject kalah = GameObject.Find("Canvas_Kalah");
            if (kalah != null) RapikanTeks(kalah.transform);
        }

        static void RapikanTeks(Transform induk)
        {
            foreach (Text t in induk.GetComponentsInChildren<Text>(true))
            {
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                if (t.GetComponent<Shadow>() == null)
                {
                    Shadow bayangan = t.gameObject.AddComponent<Shadow>();
                    bayangan.effectColor = WarnaBayangan;
                    bayangan.effectDistance = new Vector2(2f, -2f);
                }
            }
        }

        static void Gedein(Transform induk, string nama, int ukuranMin)
        {
            Transform t = Cari(induk, nama);
            Text teks = t != null ? t.GetComponent<Text>() : null;
            if (teks != null && teks.fontSize < ukuranMin) teks.fontSize = ukuranMin;
        }

        static Transform Cari(Transform induk, string nama)
        {
            foreach (Transform t in induk.GetComponentsInChildren<Transform>(true))
                if (t.name == nama) return t;
            return null;
        }
    }
}
