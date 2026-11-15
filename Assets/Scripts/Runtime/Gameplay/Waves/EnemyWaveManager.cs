using System.Collections;
using UnityEngine;
using VertigoShift.Gameplay.Enemies;

namespace VertigoShift.Gameplay.Waves
{
    /// <summary>
    /// Spawns enemies in waves with increasing counts. Enemies are spawned at
    /// random surface locations inside the arena and do not respawn individually.
    /// </summary>
    public class EnemyWaveManager : MonoBehaviour
    {
        [Header("Prefab")]
        public GameObject enemyPrefab;

        [Header("Wave progression")]
        [Tooltip("Enemies spawned in wave 1.")]
        public int baseEnemyCount = 3;
        [Tooltip("Additional enemies per wave.")]
        public int enemyCountIncrement = 2;
        [Tooltip("Seconds between waves.")]
        public float timeBetweenWaves = 8f;
        [Tooltip("Delay before the first wave.")]
        public float initialDelay = 3f;
        [Tooltip("Seconds between individual spawns inside a wave.")]
        public float spawnInterval = 0.4f;
        [Tooltip("0 = infinite waves.")]
        public int maxWaves = 0;

        [Header("Spawn area")]
        public Vector3 arenaMin = new Vector3(-18f, 0f, -18f);
        public Vector3 arenaMax = new Vector3(18f, 40f, 18f);
        public float minSpawnDistanceFromPlayer = 6f;
        public int maxSpawnAttempts = 40;

        [Header("References")]
        public Transform player;

        int currentWave;
        int aliveCount;
        bool running = true;

        public int CurrentWave { get { return currentWave; } }
        public int EnemiesRemaining { get { return aliveCount; } }

        void Start()
        {
            if (player == null)
            {
                GameObject p = GameObject.FindWithTag("Player");
                if (p == null) p = GameObject.Find("Player");
                if (p != null) player = p.transform;
            }

            if (enemyPrefab == null)
            {
                Debug.LogWarning("[EnemyWaveManager] No enemyPrefab assigned.");
                enabled = false;
                return;
            }

            StartCoroutine(RunWaves());
        }

        IEnumerator RunWaves()
        {
            yield return new WaitForSeconds(initialDelay);

            while (running && (maxWaves == 0 || currentWave < maxWaves))
            {
                currentWave++;
                int count = baseEnemyCount + (currentWave - 1) * enemyCountIncrement;
                Debug.Log("[EnemyWaveManager] Wave " + currentWave + " - spawning " + count + " enemies.");

                for (int i = 0; i < count; i++)
                {
                    SpawnOne();
                    yield return new WaitForSeconds(spawnInterval);
                }

                // Wait until all enemies of this wave are dead.
                while (aliveCount > 0)
                {
                    yield return null;
                }

                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        void SpawnOne()
        {
            Vector3 pos;
            if (!TryGetSpawnPosition(out pos))
            {
                Debug.LogWarning("[EnemyWaveManager] Could not find spawn position.");
                return;
            }

            GameObject go = Instantiate(enemyPrefab, pos, Quaternion.identity);
            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.respawn = false;
                if (player != null) enemy.player = player;
                enemy.onDeath += OnEnemyDeath;
            }
            aliveCount++;
        }

        void OnEnemyDeath(Enemy enemy)
        {
            aliveCount = Mathf.Max(0, aliveCount - 1);
            if (enemy != null) enemy.onDeath -= OnEnemyDeath;
        }

        bool TryGetSpawnPosition(out Vector3 position)
        {
            position = Vector3.zero;
            Vector3[] dirs = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                Vector3 randomPoint = new Vector3(
                    Random.Range(arenaMin.x, arenaMax.x),
                    Random.Range(arenaMin.y, arenaMax.y),
                    Random.Range(arenaMin.z, arenaMax.z));

                Vector3 dir = dirs[Random.Range(0, dirs.Length)];
                RaycastHit hit;
                if (Physics.Raycast(randomPoint, dir, out hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider == null) continue;
                    if (hit.collider.transform.root == transform.root) continue;

                    Vector3 spawnPos = hit.point + hit.normal * 0.6f;

                    if (player != null && Vector3.Distance(spawnPos, player.position) < minSpawnDistanceFromPlayer)
                        continue;

                    position = spawnPos;
                    return true;
                }
            }
            return false;
        }
    }
}
