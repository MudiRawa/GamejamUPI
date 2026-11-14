using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VertigoShift.Gameplay.Panic
{
    /// <summary>
    /// "Bar Pusing" / Dizziness meter (GDD section 3.B).
    /// Every gravity shift adds panic; kills give the player a chance to breathe.
    /// The value drives post-processing (chromatic aberration, vignette, saturation)
    /// and the audio low pass so the player *feels* the panic.
    /// </summary>
    [DisallowMultipleComponent]
    public class DizzinessSystem : MonoBehaviour
    {
        [Header("Meter")]
        [Range(0f, 100f)] public float value = 0f;
        [Tooltip("Passive recovery per second once the panic cooldown elapsed.")]
        public float decayPerSecond = 1.2f;
        [Tooltip("Seconds without a gravity shift before the meter starts recovering.")]
        public float decayDelay = 2.5f;

        [Header("Kill relief")]
        public float rangedKillRelief = 5f;
        public float closeKillRelief = 18f;

        [Header("Audio")]
        public AudioListener audioListener;
        public float lowPassClear = 22000f;
        public float lowPassPanic = 700f;

        [Header("VFX Pusing")]
        public float sipitMaks = 0.15f;   // seberapa sipit mata pas pusing penuh (bagian layar dari atas & bawah)
        public float kedipKuat = 0.2f;    // seberapa nutup mata pas ngedip waktu CRITICAL
        public float blurMaks = 1.2f;     // seberapa buram pas pusing penuh
        public float goyangLensa = 0.12f; // seberapa goyang layarnya

        float timeSinceShift = 99f;
        AudioLowPassFilter lowPass;

        // VFX
        DepthOfField blur;
        LensDistortion lensa;
        Texture2D kelopakTex;
        float sipit;
        float kedip;
        float jedaKedip = 2f;

        /// <summary>The meter mapped to 0..1.</summary>
        public float Normalized { get { return Mathf.Clamp01(value / 100f); } }

        public bool IsHighTension { get { return value >= 50f; } }
        public bool IsCritical { get { return value >= 85f; } }

        public string StateLabel
        {
            get
            {
                if (IsCritical) return "CRITICAL PANIC";
                if (IsHighTension) return "HIGH TENSION";
                return "STABLE";
            }
        }

        void Awake()
        {
            if (audioListener == null) audioListener = FindAnyObjectByType<AudioListener>();
            if (audioListener != null)
            {
                lowPass = audioListener.gameObject.GetComponent<AudioLowPassFilter>();
                if (lowPass == null) lowPass = audioListener.gameObject.AddComponent<AudioLowPassFilter>();
                lowPass.cutoffFrequency = lowPassClear;
            }
        }

        // VFX: volume sendiri buat blur + lensa goyang, sama tekstur kelopak mata
        void Start()
        {
            Volume volume = new GameObject("Volume_Pusing").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 50;
            VolumeProfile profil = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profil;

            blur = profil.Add<DepthOfField>(true);
            blur.mode.Override(DepthOfFieldMode.Off);
            lensa = profil.Add<LensDistortion>(true);
            lensa.intensity.Override(0f);

            // gradasi hitam: pekat di pinggir layar, makin ke tengah makin tipis
            kelopakTex = new Texture2D(1, 64);
            for (int y = 0; y < 64; y++)
            {
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.55f, y / 63f));
                kelopakTex.SetPixel(0, y, new Color(1f, 1f, 1f, a));
            }
            kelopakTex.wrapMode = TextureWrapMode.Clamp;
            kelopakTex.Apply();
        }

        public void AddDizziness(float amount)
        {
            value = Mathf.Clamp(value + amount, 0f, 100f);
            timeSinceShift = 0f;
        }

        public void RegisterShift()
        {
            timeSinceShift = 0f;
        }

        /// <summary>Killing an enemy clears panic. Close range execution clears a lot more.</summary>
        public void OnKill(bool closeRange)
        {
            value = Mathf.Clamp(value - (closeRange ? closeKillRelief : rangedKillRelief), 0f, 100f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            timeSinceShift += dt;

            if (timeSinceShift > decayDelay && value > 0f)
                value = Mathf.Max(0f, value - decayPerSecond * dt);

            ApplyFeedback();
            UpdateVfxPusing();
        }

        // VFX: blur, layar goyang, mata sipit, ngedip
        void UpdateVfxPusing()
        {
            float n = Normalized;
            float dt = Time.unscaledDeltaTime;
            float t = Time.unscaledTime;

            // blur sama goyang mulai kerasa dari HIGH TENSION (50%)
            float berat = Mathf.InverseLerp(0.5f, 1f, n);
            if (blur != null)
            {
                blur.mode.Override(berat > 0f ? DepthOfFieldMode.Gaussian : DepthOfFieldMode.Off);
                blur.gaussianStart.Override(Mathf.Lerp(20f, 0f, berat));
                blur.gaussianEnd.Override(Mathf.Lerp(40f, 6f, berat));
                blur.gaussianMaxRadius.Override(Mathf.Lerp(0.5f, blurMaks, berat));
            }
            if (lensa != null)
            {
                lensa.intensity.Override((-0.2f + Mathf.Sin(t * 1.7f) * goyangLensa) * berat);
                lensa.center.Override(new Vector2(0.5f + Mathf.Sin(t * 0.9f) * 0.03f * berat,
                                                  0.5f + Mathf.Cos(t * 1.3f) * 0.03f * berat));
            }

            // ngedip pelan sesekali pas CRITICAL
            if (IsCritical)
            {
                jedaKedip -= dt;
                if (jedaKedip <= 0f)
                {
                    kedip = 1f;
                    jedaKedip = Random.Range(1.5f, 3.5f);
                }
            }
            kedip = Mathf.MoveTowards(kedip, 0f, dt * 2.5f);

            // mata makin sipit makin pusing
            sipit = sipitMaks * n * n + Mathf.Sin(kedip * Mathf.PI) * kedipKuat;
        }

        // VFX: gambar kelopak mata atas & bawah
        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (kelopakTex == null || sipit <= 0.001f) return;

            float h = Screen.height * sipit * 2f;
            GUI.depth = 10; // di belakang HUD
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(-10f, 0f, Screen.width + 20f, h), kelopakTex);
            GUI.DrawTextureWithTexCoords(new Rect(-10f, Screen.height - h, Screen.width + 20f, h), kelopakTex, new Rect(0f, 1f, 1f, -1f));
            GUI.color = Color.white;
        }

        void ApplyFeedback()
        {
            float n = Normalized;

            // Audio: the world gets muffled the more panicked the player is.
            if (lowPass != null)
                lowPass.cutoffFrequency = Mathf.Lerp(lowPassClear, lowPassPanic, n * n);

            // Post processing: chromatic aberration + vignette + desaturation.
            var stack = VolumeManager.instance != null ? VolumeManager.instance.stack : null;
            if (stack == null) return;

            var ca = stack.GetComponent<ChromaticAberration>();
            if (ca != null)
            {
                ca.active = true;
                ca.intensity.Override(Mathf.Lerp(0f, 1.4f, n * n));
            }

            var vig = stack.GetComponent<Vignette>();
            if (vig != null)
            {
                vig.active = true;
                vig.intensity.Override(Mathf.Lerp(0.12f, 0.55f, n));
            }

            var adj = stack.GetComponent<ColorAdjustments>();
            if (adj != null)
            {
                adj.active = true;
                adj.saturation.Override(Mathf.Lerp(0f, -40f, n));
                adj.contrast.Override(Mathf.Lerp(0f, 25f, n));
            }
        }
    }
}
