using System.Collections;
using UnityEngine;
using VertigoShift.Audio;
using VertigoShift.Gameplay.Panic;
using VertigoShift.Gameplay.Player;
using VertigoShift.Gameplay.Targets;
using VertigoShift.Input;

namespace VertigoShift.Gameplay.Weapons
{
    /// <summary>
    /// Basic revolver: 6 rounds, hitscan raycast, reload delay.
    /// Adds game feel: tracer, muzzle flash, hitstop and a FOV kick.
    /// </summary>
    [DisallowMultipleComponent]
    public class Revolver : MonoBehaviour
    {
        [Header("References")]
        public Camera cam;
        public Transform muzzle;
        public SfxBank sfx;
        public DizzinessSystem dizziness;
        public PlayerLook look;

        [Header("Weapon")]
        public int magazineSize = 6;
        public int bullets = 6;
        public float fireInterval = 0.28f;
        public float reloadTime = 1.9f;
        public float range = 120f;
        public float damage = 60f;

        [Header("Feel")]
        [Tooltip("Freeze frame duration when a shot connects.")]
        public float hitstopDuration = 0.045f;
        public float tracerDuration = 0.05f;
        public float muzzleFlashIntensity = 12f;
        public float muzzleFlashDuration = 0.05f;
        [Tooltip("Length of the tracer streak for a shot that hit nothing.")]
        public float tracerMissLength = 40f;

        [Header("FX")]
        public LineRenderer tracer;
        public Light muzzleFlash;

        public bool IsReloading { get; private set; }
        public float ReloadProgress { get; private set; }
        public int ShotsFired { get; private set; }

        float nextFireTime;
        float tracerTimer;
        float flashTimer;

        void Awake()
        {
            if (cam == null) cam = GetComponentInChildren<Camera>();
            if (sfx == null) sfx = GetComponent<SfxBank>();
            if (dizziness == null) dizziness = GetComponent<DizzinessSystem>();
            if (look == null) look = GetComponent<PlayerLook>();
            if (muzzle == null && cam != null) muzzle = cam.transform;
            bullets = magazineSize;
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
            if (ProtoInput.FireHeld && !IsReloading && Time.time >= nextFireTime) Fire();
        }

        /// <summary>
        /// Drives the tracer / muzzle flash lifetimes on unscaled time.
        /// They used to be driven by Invoke(), which runs on scaled time - during the
        /// hitstop (timeScale 0.05) the tracer stayed on screen for ~20x too long,
        /// which is exactly when the player notices the streak hanging in the world.
        /// </summary>
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
            if (Time.timeScale < 1f) Time.timeScale = 1f;
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
            if (bullets <= 0)
            {
                StartReload();
                // nextFireTime = Time.time + 0.35f;
                if (sfx != null) sfx.PlayDryFire();
                return;
            }

            bullets--;
            ShotsFired++;
            nextFireTime = Time.time + fireInterval;

            Vector3 origin = cam != null ? cam.transform.position : transform.position;
            Vector3 dir = cam != null ? cam.transform.forward : transform.forward;

            // The camera can end up embedded in level geometry (hugging a wall, or
            // standing on a wall after a gravity shift - the eye offset then points
            // sideways into the geometry). A raycast that starts inside a collider does
            // not report that collider, so the shot tunnels straight through the wall
            // and the tracer gets drawn on the far side of it. Backing the origin up to
            // just outside the geometry keeps the bullet on this side of the wall.
            Vector3 rayOrigin = origin;
            if (IsInsideGeometry(origin))
            {
                rayOrigin = PushOutOfGeometry(origin, dir);
            }

            RaycastHit hit;
            bool hasHit = Physics.Raycast(rayOrigin, dir, out hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Vector3 endPoint = hasHit ? hit.point : rayOrigin + dir * tracerMissLength;

            // Aim from the eye (so the crosshair is truthful) but draw the streak from
            // the muzzle: starting it at the eye puts the first vertex inside the near
            // clip plane, which made the streak pop out of the middle of the screen.
            // If the muzzle itself is buried in geometry, fall back to the ray origin.
            Vector3 tracerStart = muzzle != null ? muzzle.position : rayOrigin;
            if (IsInsideGeometry(tracerStart)) tracerStart = rayOrigin;

            if (sfx != null) sfx.PlayGunshot();
            if (look != null) look.AddFovKick(look.fireFovKick);
            PlayFlash();
            DrawTracer(tracerStart, endPoint);

            if (hasHit)
            {
                TargetDummy target = hit.collider.GetComponentInParent<TargetDummy>();
                if (target != null) target.TakeDamage(damage, hit.point);
                if (hitstopDuration > 0f) StartCoroutine(HitStopRoutine(hitstopDuration));
            }
        }

        /// <summary>True when the point sits strictly inside a collider (the player's own body is ignored).</summary>
        bool IsInsideGeometry(Vector3 point)
        {
            Collider[] hits = Physics.OverlapSphere(point, 0.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.root == transform.root) continue;
                // ClosestPoint returns the point itself only when it is inside the collider.
                if (hits[i].ClosestPoint(point) == point) return true;
            }
            return false;
        }

        /// <summary>
        /// Moves the point backwards along the shot direction until it leaves whatever
        /// solid geometry it is stuck in.
        /// </summary>
        Vector3 PushOutOfGeometry(Vector3 point, Vector3 dir)
        {
            Vector3 probe = point;
            for (int i = 0; i < 8; i++)
            {
                probe -= dir * 0.15f;
                if (!IsInsideGeometry(probe)) return probe;
            }
            return point - dir * 1.2f;
        }

        IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
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
