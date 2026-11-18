using UnityEngine;
using UnityEngine.SceneManagement;
using VertigoShift.Gameplay.Player;

namespace VertigoShift.Audio
{
    // musik latar (BGM). nggak usah dipasang ke scene, otomatis kebikin sendiri pas game jalan
    // dan nggak ilang pas pindah scene, jadi lagunya nyambung dari main menu ke InGame.
    // file lagunya: Assets/Resources/Audio/BGM_VertigoShift.ogg
    // di main menu sama pas mati suaranya dibikin "teredam" (kayak dari balik tembok), pas main full.
    public class MusikLatar : MonoBehaviour
    {
        const string PathLagu = "Audio/BGM_VertigoShift";
        const float Volume = 0.4f;          // kenceng musiknya, kecilin kalau nutupin suara tembakan
        const float RedamMenu = 1400f;      // makin kecil makin teredam
        const float RedamMati = 600f;

        static MusikLatar ada;

        AudioSource sumber;
        AudioLowPassFilter redam;
        PlayerHealth pemain;
        bool diMenu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Mulai()
        {
            if (ada != null) return;
            AudioClip lagu = Resources.Load<AudioClip>(PathLagu);
            if (lagu == null) { Debug.LogWarning("Lagu " + PathLagu + " nggak ketemu di folder Resources."); return; }

            GameObject go = new GameObject("MusikLatar");
            DontDestroyOnLoad(go);
            ada = go.AddComponent<MusikLatar>();
            ada.sumber = go.AddComponent<AudioSource>();
            ada.sumber.clip = lagu;
            ada.sumber.loop = true;
            ada.sumber.playOnAwake = false;
            ada.sumber.spatialBlend = 0f;
            ada.sumber.volume = Volume;
            ada.sumber.priority = 0;
            ada.redam = go.AddComponent<AudioLowPassFilter>();
            ada.sumber.Play();

            SceneManager.sceneLoaded += (s, m) => { if (ada != null) ada.CekScene(); };
            ada.CekScene();
        }

        void CekScene()
        {
            diMenu = SceneManager.GetActiveScene().name == "MainMenu";
            pemain = FindAnyObjectByType<PlayerHealth>();
        }

        void Update()
        {
            bool mati = pemain != null && pemain.Mati;
            float mau = diMenu ? RedamMenu : mati ? RedamMati : 22000f;
            // geser pelan-pelan biar nggak kerasa nyentak
            redam.cutoffFrequency = Mathf.Lerp(redam.cutoffFrequency, mau, Time.unscaledDeltaTime * 3f);
        }
    }
}
