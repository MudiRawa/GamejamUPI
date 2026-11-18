using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VertigoShift.Gameplay.Player;
using VertigoShift.Gameplay.Waves;
using VertigoShift.Gameplay.Weapons;

namespace VertigoShift.Audio
{
    // pengurus semua suara efek (selain tembakan pemain yg masih di SfxBank, sama musik di MusikLatar).
    // nggak usah dipasang ke scene, kebikin sendiri pas game jalan.
    //
    // cara pakai dari script lain:
    //   AudioManager.Putar("Klik");                       -> suara biasa (nggak ada arah)
    //   AudioManager.PutarDi("TembakMusuh", posisi);      -> suara 3D, kedengeran dari arah posisinya
    // nama = nama file di Assets/Resources/Audio/ tanpa "SFX_" (contoh SFX_Klik.wav -> "Klik")
    //
    // suara pemain, wave, sama tombol UI udah diurus sendiri di sini (ngintip dari PlayerHealth,
    // PlayerMotor, EnemyWaveManager, sama tombol di bawah mouse), jadi script itu nggak perlu diubah.
    public class AudioManager : MonoBehaviour
    {
        const int JumlahSumber = 16;   // maksimal suara 3D yang bunyi barengan

        static AudioManager ada;
        static readonly Dictionary<string, AudioClip> simpanan = new Dictionary<string, AudioClip>();

        AudioSource sumber2D;
        AudioSource sumberJantung;
        AudioSource sumberReload;   // sendiri biar pitch-nya nggak ketimpa suara lain
        readonly List<AudioSource> sumber3D = new List<AudioSource>();
        int giliran;

        // yang diintip
        PlayerHealth darah;
        PlayerMotor motor;
        EnemyWaveManager wave;
        Revolver revolver;
        int pelurMasukTadi;
        bool reloadTadi;
        float darahTadi;
        bool matiTadi, napakTadi, seluncurTadi;
        float waktuMelayang, jarakLangkah;
        int waveTadi, sisaTadi;
        GameObject tombolTadi;
        readonly List<RaycastResult> hasilUI = new List<RaycastResult>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Mulai()
        {
            if (ada != null) return;
            GameObject go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            ada = go.AddComponent<AudioManager>();
            ada.Siapin();
            SceneManager.sceneLoaded += (s, m) => { if (ada != null) ada.CariYangDiintip(); };
            ada.CariYangDiintip();
        }

        void Siapin()
        {
            sumber2D = gameObject.AddComponent<AudioSource>();
            sumber2D.playOnAwake = false;
            sumber2D.spatialBlend = 0f;

            sumberReload = gameObject.AddComponent<AudioSource>();
            sumberReload.playOnAwake = false;
            sumberReload.spatialBlend = 0f;

            sumberJantung = gameObject.AddComponent<AudioSource>();
            sumberJantung.playOnAwake = false;
            sumberJantung.loop = true;
            sumberJantung.volume = 0f;
            sumberJantung.clip = Ambil("Jantung");

            for (int i = 0; i < JumlahSumber; i++)
            {
                GameObject go = new GameObject("Suara3D_" + i);
                go.transform.SetParent(transform, false);
                AudioSource s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 3f;
                s.maxDistance = 60f;
                s.dopplerLevel = 0f;
                sumber3D.Add(s);
            }
        }

        // ------------------------------------------------ dipanggil dari luar
        public static void Putar(string nama, float volume = 1f, float pitchAcak = 0.05f)
        {
            if (ada == null) return;
            AudioClip c = Ambil(nama);
            if (c == null) return;
            ada.sumber2D.pitch = 1f + Random.Range(-pitchAcak, pitchAcak);
            ada.sumber2D.PlayOneShot(c, volume);
        }

        public static void PutarDi(string nama, Vector3 posisi, float volume = 1f, float pitchAcak = 0.08f)
        {
            if (ada == null) return;
            AudioClip c = Ambil(nama);
            if (c == null) return;
            // pakai sumber berikutnya (muter), jadi suara lama yg paling tua yang ketiban
            AudioSource s = ada.sumber3D[ada.giliran];
            ada.giliran = (ada.giliran + 1) % ada.sumber3D.Count;
            s.transform.position = posisi;
            s.pitch = 1f + Random.Range(-pitchAcak, pitchAcak);
            s.clip = c;
            s.volume = volume;
            s.Play();
        }

        static AudioClip Ambil(string nama)
        {
            AudioClip c;
            if (simpanan.TryGetValue(nama, out c)) return c;
            c = Resources.Load<AudioClip>("Audio/SFX_" + nama);
            if (c == null) Debug.LogWarning("Suara SFX_" + nama + " nggak ketemu di Resources/Audio");
            simpanan[nama] = c;
            return c;
        }

        // ------------------------------------------------ ngintip game
        void CariYangDiintip()
        {
            darah = FindAnyObjectByType<PlayerHealth>();
            motor = FindAnyObjectByType<PlayerMotor>();
            wave = FindAnyObjectByType<EnemyWaveManager>();
            revolver = FindAnyObjectByType<Revolver>();
            reloadTadi = false;
            pelurMasukTadi = 0;
            darahTadi = darah != null ? darah.darah : 0f;
            matiTadi = false;
            napakTadi = true;
            seluncurTadi = false;
            waveTadi = 0;
            sisaTadi = 0;
            sumberJantung.volume = 0f;
            sumberJantung.Stop();
        }

        void Update()
        {
            IntipDarah();
            IntipGerak();
            IntipWave();
            IntipReload();
            IntipTombol();
        }

        void IntipDarah()
        {
            if (darah == null) return;

            if (darah.darah < darahTadi - 0.01f && !darah.Mati) Putar("Sakit", 0.9f);
            darahTadi = darah.darah;

            if (darah.Mati && !matiTadi) Putar("Kalah", 1f, 0f);
            matiTadi = darah.Mati;

            // deg-degan pas darah tinggal dikit (di bawah 30%)
            float persen = darah.darahMaks > 0f ? darah.darah / darah.darahMaks : 1f;
            float mauVolume = !darah.Mati && persen < 0.3f ? Mathf.Lerp(0.9f, 0.3f, persen / 0.3f) : 0f;
            sumberJantung.volume = Mathf.MoveTowards(sumberJantung.volume, mauVolume, Time.unscaledDeltaTime);
            if (sumberJantung.volume > 0f && !sumberJantung.isPlaying && sumberJantung.clip != null) sumberJantung.Play();
            if (sumberJantung.volume <= 0f && sumberJantung.isPlaying) sumberJantung.Stop();
        }

        void IntipGerak()
        {
            if (motor == null || (darah != null && darah.Mati)) return;
            bool napak = motor.IsGrounded;

            // mendarat: baru napak setelah melayang agak lama
            if (!napak) waktuMelayang += Time.deltaTime;
            else
            {
                if (!napakTadi && waktuMelayang > 0.35f) Putar("Mendarat", Mathf.Clamp01(0.4f + waktuMelayang * 0.4f));
                waktuMelayang = 0f;
            }
            napakTadi = napak;

            // seluncur
            if (motor.IsSliding && !seluncurTadi) Putar("Seluncur", 0.7f);
            seluncurTadi = motor.IsSliding;

            // langkah kaki: tiap jalan ~2.2 meter
            if (napak && !motor.IsSliding && motor.CurrentSpeed > 1f)
            {
                jarakLangkah += motor.CurrentSpeed * Time.deltaTime;
                if (jarakLangkah > 2.2f)
                {
                    jarakLangkah = 0f;
                    Putar("Langkah", motor.IsCrouching ? 0.15f : 0.35f, 0.15f);
                }
            }
        }

        // reload: tiap peluru di UI tabung keisi, bunyi "tok" satu. barengan sama UI-nya
        // (UI ngisi peluru ke-1 s/d 5 sesuai ReloadProgress, peluru ke-6 pas reload kelar).
        // suara buka tabung di awal reload masih dari SfxBank (SFX_Reload)
        void IntipReload()
        {
            if (revolver == null) return;
            int maks = Mathf.Max(1, revolver.magazineSize);

            if (revolver.IsReloading)
            {
                if (!reloadTadi) pelurMasukTadi = 0;
                int masuk = Mathf.FloorToInt(revolver.ReloadProgress * maks);
                if (masuk > pelurMasukTadi && masuk > 0)
                {
                    // ada 3 versi suara peluru masuk, dipilih acak + pitch beda dikit biar nggak kedengeran diulang
                    PutarPitch("ReloadPeluru" + Random.Range(1, 4), 0.8f, Random.Range(0.96f, 1.04f));
                    pelurMasukTadi = masuk;
                }
            }
            else if (reloadTadi) PutarPitch("ReloadTutup", 1f, 1f); // peluru terakhir + tabung ditutup

            reloadTadi = revolver.IsReloading;
        }

        static void PutarPitch(string nama, float volume, float pitch)
        {
            if (ada == null) return;
            AudioClip c = Ambil(nama);
            if (c == null) return;
            ada.sumberReload.pitch = pitch;
            ada.sumberReload.PlayOneShot(c, volume);
        }

        void IntipWave()
        {
            if (wave == null) return;
            if (wave.CurrentWave > waveTadi) Putar("WaveMulai", 0.8f, 0f);
            // wave abis: musuhnya baru aja abis semua
            if (wave.CurrentWave > 0 && sisaTadi > 0 && wave.EnemiesRemaining == 0) Putar("WaveSelesai", 0.8f, 0f);
            waveTadi = wave.CurrentWave;
            sisaTadi = wave.EnemiesRemaining;
        }

        // bunyi "tik" pas mouse lewat tombol, "klik" pas tombolnya dipencet
        void IntipTombol()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || EventSystem.current == null || Cursor.lockState == CursorLockMode.Locked) { tombolTadi = null; return; }

            PointerEventData pd = new PointerEventData(EventSystem.current) { position = mouse.position.ReadValue() };
            hasilUI.Clear();
            EventSystem.current.RaycastAll(pd, hasilUI);
            GameObject tombol = null;
            if (hasilUI.Count > 0)
            {
                Selectable sel = hasilUI[0].gameObject.GetComponentInParent<Selectable>();
                if (sel != null && sel.interactable) tombol = sel.gameObject;
            }

            if (tombol != null && tombol != tombolTadi) Putar("Hover", 0.6f, 0.03f);
            if (tombol != null && mouse.leftButton.wasPressedThisFrame) Putar("Klik", 0.8f, 0.03f);
            tombolTadi = tombol;
        }
    }
}
