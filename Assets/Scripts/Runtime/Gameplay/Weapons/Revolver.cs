using System.Collections;
using UnityEngine;
using VertigoShift.Audio;
using VertigoShift.Gameplay.Player;
using VertigoShift.Gameplay.Targets;
using VertigoShift.Input;

namespace VertigoShift.Gameplay.Weapons
{
    /// <summary>
    /// Basic revolver: 6 rounds, hitscan raycast, reload delay.
    /// Adds game feel: tracer, muzzle flash, hitstop and a FOV kick.
    /// Firing is semi-auto: one shot per left-click, never auto-repeats while holding.
    /// </summary>
    [DisallowMultipleComponent]
    public class Revolver : MonoBehaviour
    {
        [Header("References")]
        public Camera cam;
        public Transform muzzle;
        public SfxBank sfx;
        public PlayerLook look;

        [Header("Weapon")]
        public int magazineSize = 6;
        public int bullets = 6;
        public float fireInterval = 0.28f;
        public float reloadTime = 1.9f;
        public float range = 120f;
        public float damage = 60f;
        [Tooltip("Layers the hitscan can hit. Defaults to everything except Ignore Raycast.")]
        public LayerMask hitMask = Physics.DefaultRaycastLayers;

        [Header("Feel")]
        [Tooltip("Freeze frame duration when a shot connects with a target.")]
        public float hitstopDuration = 0.045f;
        public float tracerDuration = 0.05f;
        public float muzzleFlashIntensity = 12f;
        public float muzzleFlashDuration = 0.05f;
        [Tooltip("Length of the tracer streak for a shot that hit nothing.")]
        public float tracerMissLength = 40f;

        [Header("FX")]
        public LineRenderer tracer;
        public Light muzzleFlash;

        [Header("VFX")]
        public GameObject vfxTembakanRevolver;
        public GameObject vfxPeluruTrail;
        public GameObject vfxImpactPeluru;
        public GameObject vfxPeluruKenaMusuh;

        public bool IsReloading { get; private set; }
        public float ReloadProgress { get; private set; }
        public int ShotsFired { get; private set; }

        float nextFireTime;
        float tracerTimer;
        float flashTimer;

        static readonly Collider[] overlapBuffer = new Collider[64];
        static int hitstopDepth = 0;
        static float savedTimeScale = 1f;
        bool isFreezing;

        void Awake()
        {
            if (cam == null) cam = GetComponentInChildren<Camera>();
            if (sfx == null) sfx = GetComponent<SfxBank>();
            if (look == null) look = GetComponent<PlayerLook>();
            if (muzzle == null && cam != null) muzzle = cam.transform;

            if (magazineSize < 1) magazineSize = 1;
            if (fireInterval < 0.01f) fireInterval = 0.01f;
            if (reloadTime < 0.01f) reloadTime = 0.01f;
            if (range < 1f) range = 1f;
            if (tracerDuration < 0.001f) tracerDuration = 0.001f;
            if (muzzleFlashDuration < 0.001f) muzzleFlashDuration = 0.001f;
            if (hitMask.value == 0) hitMask = Physics.DefaultRaycastLayers;
            if (bullets <= 0 || bullets > magazineSize) bullets = magazineSize;
        }

        void OnValidate()
        {
            if (magazineSize < 1) magazineSize = 1;
            if (fireInterval < 0.01f) fireInterval = 0.01f;
            if (reloadTime < 0.01f) reloadTime = 0.01f;
            if (range < 1f) range = 1f;
            if (tracerDuration < 0.001f) tracerDuration = 0.001f;
            if (muzzleFlashDuration < 0.001f) muzzleFlashDuration = 0.001f;
            if (hitMask.value == 0) hitMask = Physics.DefaultRaycastLayers;
            if (bullets < 0) bullets = 0;
            if (bullets > magazineSize) bullets = magazineSize;
        }

        void Update()
        {
            if (ProtoInput.ReloadPressed) StartReload();

            if (IsReloading)
            {
                ReloadProgress += Time.deltaTime / Mathf.Max(0.01f, reloadTime);
                if (ReloadProgress >= 1f)
                {
                    ReloadProgress = 0f;
                    IsReloading = false;
                    bullets = magazineSize;
                }
            }

            // FX timers run on unscaled time so tracers/muzzle flash do not linger
            // during the hitstop (timeScale 0.05).
            UpdateFxTimers(Time.unscaledDeltaTime);
        }

        /// <summary>
        /// Firing happens in LateUpdate on purpose: PlayerLook applies the camera
        /// orientation in Update, and the order between two Update methods is not
        /// guaranteed. Shooting from Update could therefore use last frame's camera
        /// direction and draw the tracer somewhere the crosshair is not pointing.
        /// </summary>
        void LateUpdate()
        {
            // Never shoot while the cursor is free (menus, ESC) or while the game is paused.
            if (Cursor.lockState != CursorLockMode.Locked) return;
            if (Time.timeScale <= 0f) return;

            // Semi-auto: one shot per click. FirePressed is true for a single frame.
            if (ProtoInput.FirePressed && !IsReloading && Time.time >= nextFireTime) Fire();
        }

        void UpdateFxTimers(float dt)
        {
            if (tracerTimer > 0f)
            {
                tracerTimer -= dt;
                if (tracerTimer <= 0f && tracer != null) tracer.enabled = false;
            }

            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                if (flashTimer <= 0f && muzzleFlash != null)
                {
                    muzzleFlash.intensity = 0f;
                    muzzleFlash.enabled = false;
                }
            }
        }

        void OnDisable()
        {
            // Restore the global time scale if this component is disabled/destroyed
            // while a hitstop it started is still running. Uses a shared counter so
            // overlapping freezes (multiple shooters) never clobber each other.
            if (isFreezing)
            {
                isFreezing = false;
                hitstopDepth = Mathf.Max(0, hitstopDepth - 1);
                if (hitstopDepth == 0) Time.timeScale = savedTimeScale;
            }
        }

        public void StartReload()
        {
            if (IsReloading || bullets >= magazineSize) return;
            IsReloading = true;
            ReloadProgress = 0f;
            if (sfx != null) sfx.PlayReload();
        }

        void Fire()
        {
            if (cam == null)
            {
                Debug.LogWarning("[Revolver] No camera assigned; cannot fire.");
                return;
            }

            if (bullets <= 0)
            {
                // Out of ammo: auto-reload. Do not also play a dry-fire click on top
                // of the reload sound (they would overlap and clip).
                if (magazineSize <= 0)
                {
                    if (sfx != null) sfx.PlayDryFire();
                    return;
                }
                StartReload();
                return;
            }

            bullets--;
            ShotsFired++;
            nextFireTime = Time.time + fireInterval;

            Vector3 origin = cam.transform.position;
            Vector3 dir = cam.transform.forward;

            // The camera can end up embedded in level geometry (hugging a wall, or
            // standing on a wall after a gravity shift - the eye offset then points
            // sideways into the geometry). A raycast that starts inside a collider
            // does not report that collider, so the shot tunnels straight through
            // the wall and the tracer gets drawn on the far side of it. Backing the
            // origin up to just outside the geometry keeps the bullet on this side.
            Vector3 rayOrigin = origin;
            if (IsInsideGeometry(origin))
            {
                rayOrigin = PushOutOfGeometry(origin, dir);
            }

            RaycastHit hit;
            bool hasHit = Physics.Raycast(rayOrigin, dir, out hit, range, hitMask, QueryTriggerInteraction.Ignore);
            Vector3 endPoint = hasHit ? hit.point : rayOrigin + dir * tracerMissLength;

            // Aim from the eye (so the crosshair is truthful) but draw the streak
            // from the muzzle: starting it at the eye puts the first vertex inside
            // the near clip plane, which made the streak pop out of the middle of
            // the screen. If the muzzle itself is buried in geometry, fall back to
            // the ray origin.
            Vector3 tracerStart = muzzle != null ? muzzle.position : rayOrigin;
            if (IsInsideGeometry(tracerStart)) tracerStart = rayOrigin;

            if (sfx != null) sfx.PlayGunshot();
            if (look != null) look.AddFovKick(look.fireFovKick);
            PlayFlash();
            DrawTracer(tracerStart, endPoint);

            // VFX tembakan + trail peluru
            Quaternion arahTembak = Quaternion.LookRotation(endPoint - tracerStart);
            if (vfxTembakanRevolver != null) Instantiate(vfxTembakanRevolver, tracerStart, arahTembak, muzzle);
            if (vfxPeluruTrail != null) Instantiate(vfxPeluruTrail, tracerStart, arahTembak);

            if (hasHit)
            {
                TargetDummy target = hit.collider.GetComponentInParent<TargetDummy>();
                if (target != null)
                {
                    target.TakeDamage(damage, hit.point);

                    // VFX kena musuh (kalau mati, pecahnya diurus TargetDummy)
                    if (target.health > 0f && vfxPeluruKenaMusuh != null)
                        Instantiate(vfxPeluruKenaMusuh, hit.point, Quaternion.LookRotation(hit.normal));

                    // Hitstop only on a real target hit (not on walls/floor), and
                    // coordinated through a shared counter so overlapping freezes
                    // never leave Time.timeScale stuck at 1 or 0.05.
                    if (hitstopDuration > 0f) StartCoroutine(HitStopRoutine(hitstopDuration));
                }
                else if (vfxImpactPeluru != null)
                {
                    // VFX nembak tembok
                    Instantiate(vfxImpactPeluru, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
        }

        /// <summary>True when the point sits strictly inside a collider (the player's own body is ignored).</summary>
        bool IsInsideGeometry(Vector3 point)
        {
            int count = Physics.OverlapSphereNonAlloc(point, 0.05f, overlapBuffer, hitMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider c = overlapBuffer[i];
                if (c == null) continue;
                if (c.transform.root == transform.root) continue;
                // ClosestPoint returns the point itself only when it is inside the collider.
                if (c.ClosestPoint(point) == point) return true;
            }
            return false;
        }

        /// <summary>
        /// Moves the point backwards along the shot direction until it leaves whatever
        /// solid geometry it is stuck in. If it cannot escape, the original point is
        /// kept rather than pushing the origin behind the player.
        /// </summary>
        Vector3 PushOutOfGeometry(Vector3 point, Vector3 dir)
        {
            Vector3 probe = point;
            for (int i = 0; i < 8; i++)
            {
                probe -= dir * 0.15f;
                if (!IsInsideGeometry(probe)) return probe;
            }
            return point;
        }

        IEnumerator HitStopRoutine(float duration)
        {
            if (hitstopDepth == 0) savedTimeScale = Time.timeScale;
            hitstopDepth++;
            isFreezing = true;
            Time.timeScale = 0.05f;

            yield return new WaitForSecondsRealtime(duration);

            isFreezing = false;
            hitstopDepth = Mathf.Max(0, hitstopDepth - 1);
            if (hitstopDepth == 0) Time.timeScale = savedTimeScale;
        }

        void PlayFlash()
        {
            if (muzzleFlash == null) return;
            muzzleFlash.intensity = muzzleFlashIntensity;
            muzzleFlash.enabled = true;
            flashTimer = muzzleFlashDuration;
        }

        void DrawTracer(Vector3 from, Vector3 to)
        {
            if (tracer == null) return;
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.SetPosition(0, from);
            tracer.SetPosition(1, to);
            tracer.enabled = true;
            tracerTimer = tracerDuration;
        }
    }
}