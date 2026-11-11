using UnityEngine;

namespace VertigoShift.Audio
{
    /// <summary>
    /// Procedural SFX so the prototype does not need any imported audio assets.
    /// Clips are synthesised once at runtime (noise burst + body for the gunshot, clicks for reload).
    /// </summary>
    [DisallowMultipleComponent]
    public class SfxBank : MonoBehaviour
    {
        public AudioSource gunSource;
        public AudioSource uiSource;

        AudioClip gunshot;
        AudioClip reload;
        AudioClip dryFire;
        AudioClip whoosh;

        void Awake()
        {
            if (gunSource == null) gunSource = gameObject.AddComponent<AudioSource>();
            if (uiSource == null) uiSource = gameObject.AddComponent<AudioSource>();

            Configure(gunSource, 1f, 0.95f);
            Configure(uiSource, 1f, 0.8f);

            gunshot = SynthesiseGunshot();
            reload = SynthesiseClick("Reload", 0.22f, 1800f, 2);
            dryFire = SynthesiseClick("DryFire", 0.09f, 2600f, 1);
            whoosh = SynthesiseWhoosh();
        }

        static void Configure(AudioSource src, float volume, float pitch)
        {
            if (src == null) return;
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            src.volume = volume;
            src.pitch = pitch;
        }

        public void PlayGunshot() { if (gunSource != null && gunshot != null) gunSource.PlayOneShot(gunshot); }
        public void PlayReload() { if (uiSource != null && reload != null) uiSource.PlayOneShot(reload); }
        public void PlayDryFire() { if (uiSource != null && dryFire != null) uiSource.PlayOneShot(dryFire); }
        public void PlayWhoosh() { if (uiSource != null && whoosh != null) uiSource.PlayOneShot(whoosh); }

        static AudioClip SynthesiseGunshot()
        {
            const int rate = 44100;
            float length = 0.42f;
            int samples = (int)(rate * length);
            float[] data = new float[samples];
            float lp = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Exp(-t * 20f);
                float noise = Random.Range(-1f, 1f);
                lp = lp * 0.4f + noise * 0.6f;
                float sweep = 160f * Mathf.Exp(-t * 10f);
                float body = Mathf.Sin(2f * Mathf.PI * sweep * t) * Mathf.Exp(-t * 16f);
                float crack = Mathf.Exp(-t * 90f) * Random.Range(-1f, 1f) * 0.6f;
                data[i] = Mathf.Clamp((lp * env * 0.9f + body * 0.55f + crack) * 0.85f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_Gunshot", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip SynthesiseClick(string name, float length, float tone, int clicks)
        {
            const int rate = 44100;
            int samples = (int)(rate * length);
            float[] data = new float[samples];

            for (int c = 0; c < clicks; c++)
            {
                int start = (int)(samples * (c / (float)Mathf.Max(1, clicks)));
                float clickLen = samples / (float)clicks;
                for (int i = start; i < samples && i < start + (int)clickLen; i++)
                {
                    float t = (i - start) / rate;
                    float env = Mathf.Exp(-t * 55f);
                    float noise = Random.Range(-1f, 1f) * 0.5f;
                    float tone2 = Mathf.Sin(2f * Mathf.PI * tone * t) * 0.6f;
                    data[i] = Mathf.Clamp((noise + tone2) * env, -1f, 1f);
                }
            }

            AudioClip clip = AudioClip.Create("SFX_" + name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip SynthesiseWhoosh()
        {
            const int rate = 44100;
            float length = 0.55f;
            int samples = (int)(rate * length);
            float[] data = new float[samples];
            float lp = 0f, hp = 0f, prev = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / length));
                float noise = Random.Range(-1f, 1f);
                lp = lp * 0.86f + noise * 0.14f;          // low pass sweep
                hp = lp - prev * 0.92f;                     // cheap high pass
                prev = lp;
                data[i] = Mathf.Clamp(hp * env * 2.4f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_Whoosh", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
