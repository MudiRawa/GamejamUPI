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
        [Tooltip("Musuh suka jaga jarak segini dari pemain: kejauhan maju, kedeketan mundur, di tengah muter-muter.")]
        public float jarakIdealMin = 6f;
        public float jarakIdealMax = 14f;
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
        public GameObject vfxPeluruKenaPemain;   // VFX: peluru kena pemain (template slot)
        public LineRenderer tracer;
        public Light muzzleFlash;

        [Header("Projectile (tidak auto-kenna)")]
        [Tooltip("Prefab projectile musuh. Kalau kosong, dibuat runtime (template).")]
        public GameObject enemyProjectilePrefab;
        [Tooltip("Kecepatan peluru musuh (m/s).")]
        public float projectileSpeed = 30f;
        [Tooltip("Jarak toleransi kena pemain (m). Kecil = tidak auto-kenna.")]
        public float projectileHitRadius = 0.6f;
        [Tooltip("Gravitasi peluru (0 = lurus).")]
        public float projectileGravity = 0f;
        [Tooltip("Nggak dipakai lagi, diganti melesetMeter.")]
        public float aimSpread = 4f;
        [Tooltip("Peluru meleset maksimal segini (meter) dari badan pemain. 0 = selalu pas.")]
        public float melesetMeter = 0.45f;
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

        // gerak: kecepatan sekarang, arah muter, dan daftar semua musuh (biar nggak numpuk)
        Vector3 kecepatan;
        float arahSamping = 1f, waktuGantiArah;
        PlayerMotor motorPemain;
        static readonly System.Collections.Generic.List<Enemy> semuaMusuh = new System.Collections.Generic.List<Enemy>();

        void OnEnable() { if (!semuaMusuh.Contains(this)) semuaMusuh.Add(this); }
        void OnDisable() { semuaMusuh.Remove(this); }

        /// <summary>Fired when this enemy dies (used by wave spawners).</summary>
        public System.Action<Enemy> onDeath;

        void OnValidate()
        {
            if (projectileSpeed < 1f) projectileSpeed = 30f;
            if (projectileHitRadius < 0.1f) projectileHitRadius = 0.6f;
            if (aimSpread < 0f) aimSpread = 0f;
        }

        void Awake()
        {
            col = GetComponent<Collider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();

            if (hitMask.value == 0) hitMask = Physics.DefaultRaycastLayers;
            if (tracerDuration < 0.001f) tracerDuration = 0.001f;
            if (muzzleFlashDuration < 0.001f) muzzleFlashDuration = 0.001f;
            if (fireInterval < 0.1f) fireInterval = 0.1f;
            if (moveSpeed < 0f) moveSpeed = 0f;
            if (projectileSpeed < 1f) projectileSpeed = 30f;
            if (projectileHitRadius < 0.1f) projectileHitRadius = 0.6f;
            if (aimSpread < 0f) aimSpread = 0f;
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

            // ---- nempel ke permukaan
            // permukaannya dikunci dari pas spawn, jadi musuh nggak tiba-tiba miring / loncat ke atas box
            Vector3 up = surfaceUp;
            float setengahTinggi = Mathf.Max(0.1f, skalaAsli.y * 0.5f); // pakai skala asli, bukan skala animasi
            RaycastHit groundHit;
            if (CariLantai(up, setengahTinggi, 0.35f, out groundHit))
            {
                grounded = true;
                verticalVelocity = 0f;
                Vector3 pas = groundHit.point + up * setengahTinggi;
                transform.position = Vector3.Lerp(transform.position, pas, 15f * Time.deltaTime); // turun tangga halus
            }
            else
            {
                grounded = false;
                verticalVelocity += gravityStrength * Time.deltaTime;
                float jatuh = verticalVelocity * Time.deltaTime;
                if (CariLantai(up, setengahTinggi, jatuh + 0.05f, out groundHit))
                {
                    transform.position = groundHit.point + up * setengahTinggi;
                    verticalVelocity = 0f;
                    grounded = true;
                }
                else transform.position -= up * jatuh;
            }

            // ---- mau gerak ke mana
            Vector3 toPlayer = player.position - transform.position;
            float dist = toPlayer.magnitude;
            Vector3 datar = Vector3.ProjectOnPlane(toPlayer, up);
            float jarakDatar = datar.magnitude;
            Vector3 maju = jarakDatar > 0.1f ? datar / jarakDatar : Vector3.ProjectOnPlane(transform.forward, up).normalized;
            Vector3 samping = Vector3.Cross(up, maju);

            // ganti arah muter tiap beberapa detik biar nggak ketebak
            if (Time.time > waktuGantiArah)
            {
                arahSamping = Random.value < 0.5f ? -1f : 1f;
                waktuGantiArah = Time.time + Random.Range(1.5f, 3.5f);
            }

            // kejauhan -> maju, kedeketan -> mundur, pas -> muter-muter (strafe)
            float arahMaju = jarakDatar > jarakIdealMax ? 1f : jarakDatar < Mathf.Max(stopDistance, jarakIdealMin) ? -1f : 0f;
            Vector3 mau = maju * arahMaju + samping * arahSamping * 0.6f;

            // jauhin musuh lain biar nggak numpuk
            foreach (Enemy lain in semuaMusuh)
            {
                if (lain == this || lain.dead) continue;
                Vector3 jauh = Vector3.ProjectOnPlane(transform.position - lain.transform.position, up);
                float d = jauh.magnitude;
                if (d > 0.01f && d < 2.5f) mau += jauh / d * (2.5f - d) / 2.5f * 1.5f;
            }
            if (mau.sqrMagnitude > 1f) mau.Normalize();

            // belum selesai muncul -> diem. lagi ngisi tembakan -> pelan
            float laju = moveSpeed;
            if (muncul < 1f) laju = 0f;
            else if (ngisi != null) laju *= 0.3f;

            // percepatan halus biar nggak patah-patah
            kecepatan = Vector3.MoveTowards(kecepatan, mau * laju, 6f * Time.deltaTime);
            Vector3 langkah = kecepatan * Time.deltaTime;
            if (langkah.sqrMagnitude > 0.000001f)
            {
                float boleh = JarakBebas(langkah.normalized, langkah.magnitude, up);
                if (boleh < langkah.magnitude)
                {
                    // nabrak: belok arah muternya, kecepatannya dibuang
                    arahSamping = -arahSamping;
                    kecepatan = Vector3.zero;
                    langkah = langkah.normalized * boleh;
                }
                transform.position += langkah;
            }

            if (facePlayer)
            {
                Quaternion targetRot = Quaternion.LookRotation(maju, up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }

            // Highlight if the player hasn't seen/shot this enemy for a while.
            UpdateHighlight();

            if (muncul < 1f) return; // belum selesai muncul, jangan nembak dulu

            // titik tengah badan pemain. pemainnya nggak ikut muter pas pindah gravitasi,
            // jadi "atas"-nya diambil dari PlayerMotor, bukan Vector3.up
            if (motorPemain == null) motorPemain = player.GetComponent<PlayerMotor>();
            Vector3 atasPemain = motorPemain != null ? motorPemain.Up : Vector3.up;
            Vector3 dadaPemain = player.position + atasPemain * 0.4f;
            Vector3 moncong = muzzle != null ? muzzle.position : transform.position + up * 0.3f + transform.forward * 0.6f;
            float jarakTembak = Vector3.Distance(moncong, dadaPemain);

            // VFX: tanda mau nembak, muncul sebentar sebelum peluru keluar
            if (vfxNgisi != null && ngisi == null && dist <= range && Time.time >= nextFireTime - waktuNgisi)
                ngisi = Instantiate(vfxNgisi, moncong, transform.rotation, transform);

            // Shooting.
            if (Time.time >= nextFireTime && dist <= range)
            {
                // cek ada yang ngehalangin nggak (garis lurus ke dada pemain)
                Vector3 lurus = (dadaPemain - moncong).normalized;
                bool blocked = false;
                RaycastHit losHit;
                if (Physics.Raycast(moncong, lurus, out losHit, jarakTembak, hitMask, QueryTriggerInteraction.Ignore))
                    if (losHit.collider.transform.root != transform.root && losHit.distance < jarakTembak - 0.3f) blocked = true;

                if (!blocked)
                {
                    // meleset dikit (dalam meter, jadi jauh-deket sama adilnya)
                    Vector3 titikBidik = dadaPemain + Random.insideUnitSphere * melesetMeter;
                    Vector3 dir = (titikBidik - moncong).normalized;
                    Fire(moncong, dir, atasPemain * 0.4f);
                    nextFireTime = Time.time + fireInterval;
                    ngisi = null; // VFX: biar tembakan berikutnya ada tandanya lagi
                }
                else
                {
                    // ketahan: geser ke samping dulu, coba lagi bentar
                    arahSamping = -arahSamping;
                    nextFireTime = Time.time + 0.25f;
                }
            }
        }

        // cari lantai di bawah kaki (searah permukaan). permukaan miring/samping box nggak dihitung lantai
        bool CariLantai(Vector3 up, float setengahTinggi, float lebih, out RaycastHit hasil)
        {
            Vector3 asal = transform.position + up * 0.2f;
            RaycastHit[] kena = Physics.RaycastAll(asal, -up, setengahTinggi + 0.2f + lebih, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            hasil = new RaycastHit();
            float terdekat = Mathf.Infinity;
            foreach (RaycastHit h in kena)
            {
                if (h.collider.transform.root == transform.root) continue;
                if (h.collider.GetComponentInParent<Enemy>() != null) continue;
                if (Vector3.Dot(h.normal, up) < 0.7f) continue;
                if (h.distance < terdekat) { terdekat = h.distance; hasil = h; }
            }
            return terdekat < Mathf.Infinity;
        }

        // seberapa jauh boleh jalan ke arah itu sebelum nabrak tembok / box
        float JarakBebas(Vector3 arah, float jarak, Vector3 up)
        {
            float boleh = jarak;
            Vector3 kanan = Vector3.Cross(up, arah).normalized;
            Vector3[] asal = { transform.position, transform.position + kanan * 0.4f, transform.position - kanan * 0.4f };
            foreach (Vector3 a in asal)
            {
                RaycastHit h;
                if (Physics.Raycast(a, arah, out h, jarak + 0.55f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    && h.collider.transform.root != transform.root
                    && Mathf.Abs(Vector3.Dot(h.normal, up)) < 0.5f)
                    boleh = Mathf.Min(boleh, Mathf.Max(0f, h.distance - 0.55f));
            }
            return boleh;
        }

        void Fire(Vector3 origin, Vector3 dir, Vector3 offsetDada)
        {
            if (sfx != null) sfx.PlayGunshot();

            // VFX: muzzle flash + efek tembakan di moncong
            PlayFlash();
            Quaternion rot = Quaternion.LookRotation(dir);
            if (vfxTembakan != null) Instantiate(vfxTembakan, origin, rot, muzzle);

            // Tracer pendek di moncong (opsional, null di prefab default)
            DrawTracer(origin, origin + dir * Mathf.Min(6f, range));

            // --- PELURU (tidak auto-kenna): spawn projectile yang butuh waktu tempuh ---
            // Template: gampang tambah efek, isi slot vfxPeluruTrail/vfxImpact/vfxPeluruKenaPemain
            // di prefab Enemy atau lewat VFXBuilder.
            GameObject projGo;
            if (enemyProjectilePrefab != null)
            {
                projGo = Instantiate(enemyProjectilePrefab, origin, Quaternion.LookRotation(dir));
            }
            else
            {
                projGo = new GameObject("EnemyProjectile");
                projGo.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(dir));
            }

            EnemyProjectile proj = projGo.GetComponent<EnemyProjectile>();
            if (proj == null) proj = projGo.AddComponent<EnemyProjectile>();

            // isi parameter + slot VFX (template) dari Enemy, biar efek musuh mudah diganti
            proj.speed = projectileSpeed;
            proj.damage = damage;
            proj.gravity = projectileGravity;
            proj.hitRadius = projectileHitRadius;
            proj.hitMask = hitMask;
            proj.vfxTrail = vfxPeluruTrail;
            proj.vfxImpact = vfxImpact;
            proj.vfxHitPlayer = vfxPeluruKenaPemain;

            proj.Launch(origin, dir, damage, player, transform, offsetDada);
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