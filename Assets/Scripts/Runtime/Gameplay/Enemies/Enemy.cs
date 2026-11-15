using System.Collections;
using UnityEngine;
using VertigoShift.Audio;
using VertigoShift.Core;
using VertigoShift.Gameplay.Panic;
using VertigoShift.Gameplay.Player;

namespace VertigoShift.Gameplay.Enemies
{
    /// <summary>
    /// Slow-moving shooting enemy. Visual is just a cube by default so it can be
    /// replaced later with any asset: the script only relies on Transform/Collider,
    /// not on a specific mesh or renderer.
    /// </summary>
    [DisallowMultipleComponent]
    public class Enemy : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        public float maxHealth = 50f;
        public float health = 50f;

        public float Health { get { return health; } }

        [Header("References")]
        public Transform player;
        public DizzinessSystem dizziness;
        public GameObject vfxMusuhPecah;
        public GameObject vfxMuncul;     // VFX: pas musuh muncul
        public GameObject vfxNgisi;      // VFX: tanda musuh mau nembak
        public float waktuNgisi = 0.5f;  // VFX: tandanya muncul berapa detik sebelum nembak

        [Header("Movement")]
        [Tooltip("How fast the enemy walks toward the player (m/s).")]
        public float moveSpeed = 1.5f;
        [Tooltip("Stop this far from the player to avoid overlapping.")]
        public float stopDistance = 2f;
        [Tooltip("Surface normal the enemy sticks to (feet direction). Auto-detected at spawn if autoDetectSurface is on.")]
        public Vector3 surfaceUp = Vector3.up;
        public bool autoDetectSurface = true;
        public bool facePlayer = true;

        [Header("Shooting")]
        public Transform muzzle;
        public float fireInterval = 2f;
        public float range = 40f;
        public float damage = 10f;   // darah pemain yang ilang tiap kena tembak
        [Tooltip("Layers the enemy shot can hit (walls, cover).")]
        public LayerMask hitMask = Physics.DefaultRaycastLayers;
        public GameObject vfxTembakan;
        public GameObject vfxPeluruTrail;
        public GameObject vfxImpact;
        public LineRenderer tracer;
        public Light muzzleFlash;
        [Tooltip("Optional sound bank; if null, shooting is silent.")]
        public SfxBank sfx;

        [Header("Feel")]
        public float tracerDuration = 0.05f;
        public float muzzleFlashIntensity = 8f;
        public float muzzleFlashDuration = 0.05f;

        [Header("Death")]
        public float closeRangeDistance = 8f;
        [Tooltip("If true the enemy respawns after respawnDelay, otherwise it is destroyed.")]
        public bool respawn = true;
        public float respawnDelay = 5f;
        public float destroyDelay = 3f;

        Renderer rend;
        Collider col;
        Material instanceMaterial;
        Color baseColor = Color.white;
        float flashTimer;

        Transform highlight;

        float tracerTimer;
        float flashLightTimer;

        bool dead;
        float nextFireTime;

        [Header("Gravity")]
        public float gravityStrength = 20f;
        public float groundCheckDistance = 1.2f;
        float verticalVelocity;
        bool grounded;

        [Header("Highlight")]
        [Tooltip("Seconds without being seen/shot before the enemy is highlighted.")]
        public float highlightDelay = 6f;
        public Color highlightColor = new Color(1f, 0.85f, 0.2f);
        bool highlighted;
        float lastSeenTime;

        // VFX: muncul pelan dari 0, sama mental dikit pas ketembak
        Vector3 skalaAsli;
        float muncul, pukul;
        GameObject ngisi;

        /// <summary>Fired when this enemy dies (used by wave spawners).</summary>
        public System.Action<Enemy> onDeath;

        void Awake()
        {
            col = GetComponent<Collider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();

            if (hitMask.value == 0) hitMask = Physics.DefaultRaycastLayers;
            if (tracerDuration < 0.001f) tracerDuration = 0.001f;
            if (muzzleFlashDuration < 0.001f) muzzleFlashDuration = 0.001f;
            if (fireInterval < 0.1f) fireInterval = 0.1f;
            if (moveSpeed < 0f) moveSpeed = 0f;
        }

        void Start()
        {
            rend = GetComponent<Renderer>();
            if (rend != null)
            {
                instanceMaterial = rend.material;
                baseColor = instanceMaterial.HasProperty("_BaseColor")
                    ? instanceMaterial.GetColor("_BaseColor")
                    : Color.white;
            }

            highlight = transform.Find("Highlight");
            if (highlight != null) highlight.gameObject.SetActive(false);

            if (player == null)
            {
                GameObject p = GameObject.FindWithTag("Player");
                if (p == null) p = GameObject.Find("Player");
                if (p != null) player = p.transform;
            }
            if (dizziness == null) dizziness = FindAnyObjectByType<DizzinessSystem>();

            if (autoDetectSurface || surfaceUp == Vector3.zero)
            {
                DetectSurface();
            }
            if (surfaceUp.sqrMagnitude < 0.0001f) surfaceUp = Vector3.up;
            surfaceUp.Normalize();

            health = Mathf.Clamp(health, 0f, maxHealth);
            if (health <= 0f) health = maxHealth;

            lastSeenTime = Time.time;

            nextFireTime = Time.time + Random.Range(0.5f, fireInterval);

            // VFX: muncul
            skalaAsli = transform.localScale;
            muncul = 0f;
            transform.localScale = Vector3.zero;
            if (vfxMuncul != null) Instantiate(vfxMuncul, transform.position, Quaternion.LookRotation(surfaceUp));
        }

        void Update()
        {
            // FX timers (unscaled so they survive hitstop).
            UpdateFxTimers(Time.unscaledDeltaTime);

            // VFX: membesar pas muncul, mental dikit pas ketembak
            muncul = Mathf.MoveTowards(muncul, 1f, Time.deltaTime / 0.35f);
            pukul = Mathf.MoveTowards(pukul, 0f, Time.deltaTime * 6f);
            transform.localScale = skalaAsli * (Mathf.SmoothStep(0f, 1f, muncul) * (1f + 0.3f * pukul));

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (instanceMaterial != null && instanceMaterial.HasProperty("_BaseColor"))
                {
                    Color c = Color.Lerp(baseColor, Color.white, Mathf.Clamp01(flashTimer * 8f));
                    instanceMaterial.SetColor("_BaseColor", c);
                    if (instanceMaterial.HasProperty("_EmissionColor"))
                        instanceMaterial.SetColor("_EmissionColor", c * 2.5f);
                }
            }
            else if (instanceMaterial != null && instanceMaterial.HasProperty("_BaseColor"))
            {
                // Restore color to match highlight state once flash ends.
                instanceMaterial.SetColor("_BaseColor", highlighted ? highlightColor : baseColor);
                if (instanceMaterial.HasProperty("_EmissionColor"))
                    instanceMaterial.SetColor("_EmissionColor", highlighted ? highlightColor * 2f : Color.black);
            }

            if (dead) return;
            if (player == null) return;

            // Grounding & gravity along the current surface so enemies do not float.
            Vector3 up = surfaceUp;
            RaycastHit groundHit;
            bool foundGround = Physics.Raycast(transform.position, -up, out groundHit, groundCheckDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && groundHit.collider != null
                && groundHit.collider.transform.root != transform.root;
            if (foundGround)
            {
                grounded = true;
                verticalVelocity = 0f;
                surfaceUp = groundHit.normal.normalized;
                up = surfaceUp;
                float halfHeight = Mathf.Max(0.1f, transform.lossyScale.y * 0.5f);
                transform.position = groundHit.point + up * halfHeight;
            }
            else
            {
                grounded = false;
                verticalVelocity += gravityStrength * Time.deltaTime;
                Vector3 fallStep = -up * verticalVelocity * Time.deltaTime;

                // Fall collision: use raycasts so mesh colliders are detected.
                RaycastHit fallHit = new RaycastHit();
                bool fallBlocked = false;
                float fallHitDist = fallStep.magnitude + 0.05f;
                Vector3 fallDir = -up;
                Vector3[] fallOrigins = { transform.position, transform.position + Vector3.right * 0.3f, transform.position - Vector3.right * 0.3f, transform.position + Vector3.forward * 0.3f, transform.position - Vector3.forward * 0.3f };
                foreach (var origin in fallOrigins)
                {
                    RaycastHit h;
                    if (Physics.Raycast(origin, fallDir, out h, fallStep.magnitude + 0.05f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (h.collider != null && h.collider.transform.root != transform.root && h.distance < fallHitDist)
                        {
                            fallHit = h;
                            fallHitDist = h.distance;
                            fallBlocked = true;
                        }
                    }
                }
                if (fallBlocked)
                {
                    surfaceUp = fallHit.normal.normalized;
                    up = surfaceUp;
                    float halfHeight = Mathf.Max(0.1f, transform.lossyScale.y * 0.5f);
                    transform.position = fallHit.point + up * halfHeight;
                    verticalVelocity = 0f;
                    grounded = true;
                    return;
                }

                transform.position += fallStep;
            }

            // Movement: walk slowly toward the player along the surface.
            Vector3 toPlayer = player.position - transform.position;
            float dist = toPlayer.magnitude;
            Vector3 moveDir = Vector3.ProjectOnPlane(toPlayer, surfaceUp);
            float moveDist = moveDir.magnitude;
            if (moveDist > stopDistance && moveSpeed > 0f)
            {
                moveDir.Normalize();
                Vector3 moveStep = moveDir * moveSpeed * Time.deltaTime;

                // Collision avoidance: do not walk through walls/obstacles.
                // Use several raycasts (center + sides) so non-convex mesh colliders
                // (which SphereCast may skip) are still detected.
                float allowedDist = moveStep.magnitude;
                Vector3 right = Vector3.Cross(surfaceUp, moveDir).normalized;
                Vector3[] origins = {
                    transform.position,
                    transform.position + right * 0.4f,
                    transform.position - right * 0.4f
                };
                foreach (var origin in origins)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(origin, moveDir, out hit, moveStep.magnitude + 0.5f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider != null && hit.collider.transform.root != transform.root
                            && Mathf.Abs(Vector3.Dot(hit.normal, surfaceUp)) < 0.5f)
                        {
                            float d = Mathf.Max(0f, hit.distance - 0.05f);
                            if (d < allowedDist) allowedDist = d;
                        }
                    }
                }
                if (allowedDist < moveStep.magnitude)
                    moveStep = moveDir * allowedDist;

                transform.position += moveStep;
            }

            if (facePlayer)
            {
                Vector3 forward = Vector3.ProjectOnPlane(toPlayer, surfaceUp);
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.ProjectOnPlane(transform.forward, surfaceUp);
                if (forward.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(forward.normalized, surfaceUp);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
                }
            }

            // Highlight if the player hasn't seen/shot this enemy for a while.
            UpdateHighlight();

            // VFX: tanda mau nembak, muncul sebentar sebelum peluru keluar
            if (vfxNgisi != null && ngisi == null && dist <= range && Time.time >= nextFireTime - waktuNgisi)
            {
                Vector3 asal = muzzle != null ? muzzle.position : transform.position + Vector3.up * 0.5f;
                ngisi = Instantiate(vfxNgisi, asal, transform.rotation, transform);
            }

            // Shooting.
            if (Time.time >= nextFireTime && dist <= range)
            {
                Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 0.5f;
                Vector3 aimPoint = player.position + Vector3.up * 1.0f;
                Vector3 dir = (aimPoint - origin).normalized;
                if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;

                // Line of sight check.
                bool blocked = false;
                RaycastHit losHit;
                if (Physics.Raycast(origin, dir, out losHit, range, hitMask, QueryTriggerInteraction.Ignore))
                {
                    if (losHit.distance < dist - 0.5f) blocked = true;
                }

                if (!blocked)
                {
                    Fire(origin, dir);
                    nextFireTime = Time.time + fireInterval;
                    ngisi = null; // VFX: biar tembakan berikutnya ada tandanya lagi
                }
                else
                {
                    // Try again shortly.
                    nextFireTime = Time.time + 0.25f;
                }
            }
        }

        void Fire(Vector3 origin, Vector3 dir)
        {
            if (sfx != null) sfx.PlayGunshot();

            Vector3 endPoint = origin + dir * range;
            RaycastHit hit;
            bool hasHit = Physics.Raycast(origin, dir, out hit, range, hitMask, QueryTriggerInteraction.Ignore);
            if (hasHit) endPoint = hit.point;

            PlayFlash();
            DrawTracer(origin, endPoint);

            Quaternion rot = Quaternion.LookRotation(dir);
            if (vfxTembakan != null) Instantiate(vfxTembakan, origin, rot, muzzle);
            if (vfxPeluruTrail != null) Instantiate(vfxPeluruTrail, origin, rot);

            if (hasHit)
            {
                if (vfxImpact != null)
                    Instantiate(vfxImpact, hit.point, Quaternion.LookRotation(hit.normal));
            }

            // pemain ada di layer Ignore Raycast jadi nggak kena raycast,
            // makanya dicek manual: pelurunya lewat deket pemain & nggak ketahan tembok duluan
            if (player != null)
            {
                float jarakKePemain = Vector3.Dot(player.position - origin, dir);
                Vector3 titikTerdekat = origin + dir * jarakKePemain;
                bool lewatPemain = jarakKePemain > 0f && Vector3.Distance(titikTerdekat, player.position) < 1.5f;
                bool ketahan = hasHit && hit.distance < jarakKePemain - 0.5f;
                if (lewatPemain && !ketahan)
                {
                    // kena: kurangin darah pemain (PlayerHealth yang ngurus efek layar merah & kalah)
                    PlayerHealth darah = player.GetComponentInParent<PlayerHealth>();
                    if (darah == null) darah = player.gameObject.AddComponent<PlayerHealth>();
                    darah.TakeDamage(damage, origin);
                }
            }
        }

        public void TakeDamage(float amount, Vector3 hitPoint)
        {
            if (dead) return;
            health -= amount;
            flashTimer = 0.15f;
            pukul = 1f; // VFX: mental dikit
            lastSeenTime = Time.time;
            if (highlighted) SetHighlighted(false);

            if (health <= 0f) Die();
        }

        void SetHighlighted(bool on)
        {
            highlighted = on;
            if (highlight != null) highlight.gameObject.SetActive(on);

            // Keep base material change as fallback (visible when not occluded).
            if (instanceMaterial == null || !instanceMaterial.HasProperty("_BaseColor")) return;
            instanceMaterial.SetColor("_BaseColor", on ? highlightColor : baseColor);
            if (instanceMaterial.HasProperty("_EmissionColor"))
                instanceMaterial.SetColor("_EmissionColor", on ? highlightColor * 2f : Color.black);
        }

        void UpdateHighlight()
        {
            if (player == null) return;

            bool visible = false;
            Vector3 dir = transform.position - player.position;
            float d = dir.magnitude;
            if (d <= 60f && d > 0.001f)
            {
                RaycastHit hit;
                if (Physics.Raycast(player.position + Vector3.up * 1.0f, dir.normalized, out hit, d,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != null && hit.collider.transform.root == transform.root)
                        visible = true;
                }
            }

            if (visible)
            {
                lastSeenTime = Time.time;
                if (highlighted) SetHighlighted(false);
            }
            else if (!highlighted && Time.time - lastSeenTime > highlightDelay)
            {
                SetHighlighted(true);
            }
        }

        void Die()
        {
            dead = true;
            if (onDeath != null) onDeath(this);

            if (vfxMusuhPecah != null)
            {
                Vector3 arah = player != null ? transform.position - player.position : transform.forward;
                Instantiate(vfxMusuhPecah, transform.position, Quaternion.LookRotation(arah));
            }

            if (player != null)
            {
                PlayerLook look = player.GetComponent<PlayerLook>();
                if (look != null) look.AddShake(look.shakeMusuhPecah);
            }

            float distance = player != null ? Vector3.Distance(player.position, transform.position) : 999f;
            bool closeRange = distance <= closeRangeDistance;
            if (dizziness != null) dizziness.OnKill(closeRange);

            if (rend != null) rend.enabled = false;
            if (col != null) col.enabled = false;

            StartCoroutine(respawn ? RespawnRoutine() : DestroyRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            health = maxHealth;
            dead = false;
            muncul = 0f; // VFX: muncul lagi dari kecil
            if (vfxMuncul != null) Instantiate(vfxMuncul, transform.position, Quaternion.LookRotation(surfaceUp));
            highlighted = false;
            lastSeenTime = Time.time;
            if (highlight != null) highlight.gameObject.SetActive(false);
            if (rend != null) rend.enabled = true;
            if (col != null) col.enabled = true;
            nextFireTime = Time.time + Random.Range(0.5f, fireInterval);
            if (instanceMaterial != null && instanceMaterial.HasProperty("_BaseColor"))
            {
                instanceMaterial.SetColor("_BaseColor", baseColor);
                if (instanceMaterial.HasProperty("_EmissionColor"))
                    instanceMaterial.SetColor("_EmissionColor", Color.black);
            }
        }

        IEnumerator DestroyRoutine()
        {
            yield return new WaitForSeconds(destroyDelay);
            Destroy(gameObject);
        }


        void DetectSurface()
        {
            Vector3[] dirs = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            float bestDist = Mathf.Infinity;
            Vector3 bestNormal = Vector3.up;
            bool found = false;
            foreach (var d in dirs)
            {
                RaycastHit hit;
                Vector3 origin = transform.position + d * 0.05f;
                if (Physics.Raycast(origin, d, out hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != null && hit.collider.transform.root == transform.root) continue;
                    if (hit.distance < bestDist)
                    {
                        bestDist = hit.distance;
                        bestNormal = hit.normal;
                        found = true;
                    }
                }
            }
            if (found) surfaceUp = bestNormal;
        }

        void PlayFlash()
        {
            if (muzzleFlash == null) return;
            muzzleFlash.intensity = muzzleFlashIntensity;
            muzzleFlash.enabled = true;
            flashLightTimer = muzzleFlashDuration;
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

        void UpdateFxTimers(float dt)
        {
            if (tracerTimer > 0f)
            {
                tracerTimer -= dt;
                if (tracerTimer <= 0f && tracer != null) tracer.enabled = false;
            }

            if (flashLightTimer > 0f)
            {
                flashLightTimer -= dt;
                if (flashLightTimer <= 0f && muzzleFlash != null)
                {
                    muzzleFlash.intensity = 0f;
                    muzzleFlash.enabled = false;
                }
            }
        }
    }
}