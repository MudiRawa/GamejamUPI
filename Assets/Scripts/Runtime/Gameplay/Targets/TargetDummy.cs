using System.Collections;
using UnityEngine;
using VertigoShift.Core;
using VertigoShift.Gameplay.Panic;
using VertigoShift.Gameplay.Player;

namespace VertigoShift.Gameplay.Targets
{
    /// <summary>
    /// Shootable target. On death it bursts into low poly gibs and clears dizziness:
    /// a ranged kill gives a small breather, a close range execution clears a lot more (GDD 3.B).
    /// </summary>
    [DisallowMultipleComponent]
    public class TargetDummy : MonoBehaviour
    {
        [Header("Health")]
        public float maxHealth = 100f;
        public float health = 100f;

        [Header("References")]
        public DizzinessSystem dizziness;
        public Transform player;
        public Material gibMaterial;

        [Header("VFX")]
        public GameObject vfxMusuhPecah;

        [Header("Death")]
        public int gibCount = 10;
        public float gibSize = 0.22f;
        public float gibLife = 2.5f;
        public float gibLaunchSpeed = 6f;
        public float respawnDelay = 3.5f;
        [Tooltip("Distance that counts as a close range execution.")]
        public float closeRangeDistance = 8f;

        Renderer rend;
        Collider col;
        Material instanceMaterial;
        Color baseColor = Color.white;
        float flashTimer;
        bool dead;

        void Start()
        {
            rend = GetComponent<Renderer>();
            col = GetComponent<Collider>();
            if (rend != null)
            {
                instanceMaterial = rend.material;
                baseColor = instanceMaterial.HasProperty("_BaseColor")
                    ? instanceMaterial.GetColor("_BaseColor")
                    : Color.white;
            }
            if (gibMaterial == null && rend != null) gibMaterial = rend.sharedMaterial;
        }

        void Update()
        {
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
        }

        public void TakeDamage(float amount, Vector3 hitPoint)
        {
            if (dead) return;
            health -= amount;
            flashTimer = 0.15f;

            if (health <= 0f) Die();
        }

        void Die()
        {
            dead = true;

            // VFX musuh pecah, kalau kosong pakai gibs yang lama
            if (vfxMusuhPecah != null)
            {
                Vector3 arah = player != null ? transform.position - player.position : transform.forward;
                Instantiate(vfxMusuhPecah, transform.position, Quaternion.LookRotation(arah));
            }
            else
            {
                SpawnGibs();
            }

            // VFX: layar geter pas musuh pecah
            if (player != null)
            {
                PlayerLook look = player.GetComponent<PlayerLook>();
                if (look != null) look.AddShake(look.shakeMusuhPecah);
            }

            float distance = player != null ? Vector3.Distance(player.position, transform.position) : 999f;
            bool closeRange = distance <= closeRangeDistance;
            if (dizziness != null) dizziness.OnKill(closeRange);

            // The GameObject stays active so the respawn coroutine keeps running.
            if (rend != null) rend.enabled = false;
            if (col != null) col.enabled = false;

            StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            health = maxHealth;
            dead = false;
            if (rend != null) rend.enabled = true;
            if (col != null) col.enabled = true;
        }

        void SpawnGibs()
        {
            if (gibMaterial == null) return;

            for (int i = 0; i < gibCount; i++)
            {
                GameObject gib = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gib.name = "Gib";
                gib.transform.position = transform.position + Random.insideUnitSphere * 0.3f;
                gib.transform.rotation = Random.rotation;
                gib.transform.localScale = Vector3.one * gibSize * Random.Range(0.5f, 1.4f);

                Collider gibCollider = gib.GetComponent<Collider>();
                if (gibCollider != null) Destroy(gibCollider);

                gib.GetComponent<MeshRenderer>().sharedMaterial = gibMaterial;

                Rigidbody rbGib = gib.AddComponent<Rigidbody>();
                rbGib.mass = 0.15f;
                rbGib.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                RigidbodyCompat.SetLinearDamping(rbGib, 0.1f);
                rbGib.AddForce(Random.insideUnitSphere.normalized * gibLaunchSpeed, ForceMode.Impulse);
                rbGib.AddTorque(Random.insideUnitSphere * 12f, ForceMode.Impulse);

                Destroy(gib, gibLife);
            }
        }
    }
}
