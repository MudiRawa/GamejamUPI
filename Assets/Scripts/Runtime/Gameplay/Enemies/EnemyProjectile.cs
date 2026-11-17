using System.Collections;
using UnityEngine;
using VertigoShift.Core;
using VertigoShift.Gameplay.Player;

namespace VertigoShift.Gameplay.Enemies
{
    [DisallowMultipleComponent]
    public class EnemyProjectile : MonoBehaviour
    {
        void OnValidate()
        {
            if (speed < 1f) speed = 30f;
            if (lifetime < 0.1f) lifetime = 4f;
            if (hitRadius < 0.1f) hitRadius = 0.6f;
            if (hitMask.value == 0) hitMask = Physics.DefaultRaycastLayers;
        }

        void Awake()
        {
            if (speed < 1f) speed = 30f;
            if (lifetime < 0.1f) lifetime = 4f;
            if (hitRadius < 0.1f) hitRadius = 0.6f;
            if (hitMask.value == 0) hitMask = Physics.DefaultRaycastLayers;
        }
        [Header("Motion")]
        public float speed = 30f;
        public float lifetime = 4f;
        public float gravity = 0f;

        [Header("Damage")]
        public float damage = 10f;
        public float hitRadius = 0.6f;

        [Header("Collision")]
        public LayerMask hitMask = Physics.DefaultRaycastLayers;

        [Header("VFX (template slots)")]
        public GameObject vfxTrail;
        public GameObject vfxImpact;
        public GameObject vfxHitPlayer;

        [Header("Feel")]
        public bool destroyOnHit = true;

        Transform targetPlayer;
        Transform shooterRoot;
        Vector3 targetCenterOffset = Vector3.up * 1.0f;
        Vector3 velocity;
        GameObject trailInstance;
        float timer;
        bool done;

        public void Launch(Vector3 origin, Vector3 direction, float dmg, Transform playerTarget)
        {
            Launch(origin, direction, dmg, playerTarget, null, Vector3.up * 1.0f);
        }

        public void Launch(Vector3 origin, Vector3 direction, float dmg, Transform playerTarget, Vector3 centerOffset)
        {
            Launch(origin, direction, dmg, playerTarget, null, centerOffset);
        }

        public void Launch(Vector3 origin, Vector3 direction, float dmg, Transform playerTarget, Transform shooter, Vector3 centerOffset)
        {
            transform.position = origin;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            direction.Normalize();
            transform.rotation = Quaternion.LookRotation(direction);

            damage = dmg;
            targetPlayer = playerTarget;
            shooterRoot = shooter != null ? shooter.root : null;
            targetCenterOffset = centerOffset;
            velocity = direction * speed;
            timer = lifetime;
            done = false;

            if (vfxTrail != null)
            {
                trailInstance = Instantiate(vfxTrail, origin, transform.rotation, transform);
            }
        }

        void Update()
        {
            if (done) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                DestroySelf();
                return;
            }

            if (gravity != 0f)
            {
                velocity += Vector3.down * gravity * Time.deltaTime;
            }

            Vector3 dir = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : transform.forward;
            float stepLen = velocity.magnitude * Time.deltaTime;
            Vector3 step = dir * stepLen;

            RaycastHit wallHit = new RaycastHit();
            bool foundWall = false;
            Vector3 rayOrigin = transform.position;
            float remainingDist = stepLen + 0.05f;
            for (int i = 0; i < 8 && remainingDist > 0.0001f; i++)
            {
                RaycastHit h;
                if (!Physics.Raycast(rayOrigin, dir, out h, remainingDist, hitMask, QueryTriggerInteraction.Ignore))
                    break;

                bool isShooter = shooterRoot != null && h.collider != null && h.collider.transform.root == shooterRoot;
                bool isPlayer = targetPlayer != null && h.collider != null && h.collider.transform.root == targetPlayer.root;

                if (isShooter || isPlayer)
                {
                    float advance = h.distance + 0.01f;
                    rayOrigin = h.point + dir * 0.01f;
                    remainingDist = Mathf.Max(0f, remainingDist - advance);
                    continue;
                }

                wallHit = h;
                foundWall = true;
                break;
            }

            if (foundWall)
            {
                if (vfxImpact != null)
                    Instantiate(vfxImpact, wallHit.point, Quaternion.LookRotation(wallHit.normal));
                DestroySelf();
                return;
            }

            Vector3 sebelum = transform.position;
            transform.position += step;
            if (velocity.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir);

            if (targetPlayer != null)
            {
                Vector3 playerCenter = targetPlayer.position + targetCenterOffset;
                // dicek sepanjang langkah frame ini, bukan cuma titik akhirnya,
                // biar peluru cepet / fps rendah nggak "nembus" pemain
                Vector3 titikTerdekat = TitikTerdekat(sebelum, transform.position, playerCenter);
                float dist = Vector3.Distance(titikTerdekat, playerCenter);
                if (dist <= hitRadius)
                {
                    Vector3 toPlayer = playerCenter - transform.position;
                    float d = toPlayer.magnitude;
                    bool blocked = false;
                    if (d > 0.001f)
                    {
                        Vector3 bOrigin = transform.position;
                        float bRemaining = d;
                        while (bRemaining > 0.0001f)
                        {
                            RaycastHit blockHit;
                            if (!Physics.Raycast(bOrigin, toPlayer.normalized, out blockHit, bRemaining, hitMask, QueryTriggerInteraction.Ignore))
                                break;

                            bool isShooter = shooterRoot != null && blockHit.collider != null && blockHit.collider.transform.root == shooterRoot;
                            bool isPlayer = blockHit.collider != null && blockHit.collider.transform.root == targetPlayer.root;

                            if (isShooter)
                            {
                                float adv = blockHit.distance + 0.01f;
                                bOrigin = blockHit.point + toPlayer.normalized * 0.01f;
                                bRemaining = Mathf.Max(0f, bRemaining - adv);
                                continue;
                            }

                            if (!isPlayer)
                            {
                                blocked = true;
                                break;
                            }

                            break;
                        }
                    }

                    if (!blocked)
                    {
                        PlayerHealth darah = targetPlayer.GetComponentInParent<PlayerHealth>();
                        if (darah != null) darah.TakeDamage(damage, transform.position);

                        if (vfxHitPlayer != null)
                            Instantiate(vfxHitPlayer, transform.position, Quaternion.identity);

                        if (destroyOnHit) { DestroySelf(); return; }
                    }
                }
            }
        }

        static Vector3 TitikTerdekat(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude > 0.000001f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return a + ab * t;
        }

        void DestroySelf()
        {
            if (done) return;
            done = true;
            if (trailInstance != null) Destroy(trailInstance);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (trailInstance != null) Destroy(trailInstance);
        }
    }
}