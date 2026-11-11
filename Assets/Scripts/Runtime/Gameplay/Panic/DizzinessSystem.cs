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

        float timeSinceShift = 99f;
        AudioLowPassFilter lowPass;

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
