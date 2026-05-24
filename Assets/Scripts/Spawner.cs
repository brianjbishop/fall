using UnityEngine;

public class Spawner : MonoBehaviour
{
    [System.Serializable]
    private class SpawnEntry
    {
        public GameObject prefab;
        [Tooltip("Spawn weight; higher value means higher chance of being selected (default 1)")]
        public float weight = 1f;
        [Tooltip("Delay in seconds after game start before this prefab can be spawned (default 0)")]
        public float startDelay = 0f;
        [Tooltip("Max consecutive spawns (0 = unlimited, 1 = cannot appear twice in a row)")]
        public int maxConsecutive = 0;
    }

    [SerializeField] private SpawnEntry[] spawnEntries;
    [SerializeField] private Transform spawnedParent;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float maxPositionOffset = 1f;
    [SerializeField] private float tightPositionOffset = 2f;
    [SerializeField] private float tightOffsetDuration = 1f;
    [SerializeField] private float tightOffsetStartInterval = 8f;
    [SerializeField] private float tightOffsetMinInterval = 2f;
    [SerializeField] private float tightOffsetIntervalDecreasePerSecond = 0.05f;
    [SerializeField] private float maxRotationOffset = 15f;
    [SerializeField] private float startSpawnInterval = 2f;
    [SerializeField] private float minSpawnInterval = 0.25f;
    [SerializeField] private float spawnIntervalDecreasePerSecond = 0.05f;
    [Header("Bias Towards Player")]
    [SerializeField, Range(0f, 1f)] private float spawnBiasTowardsPlayer = 0.4f; // 0 = fully random, 1 = fully towards player
    [SerializeField] private bool biasOnlyWhenTight = true;
    private float elapsedTime;
    private float spawnTimer;
    private float tightOffsetTimer;
    private float tightOffsetCooldownTimer;
    private bool isTightOffsetActive;
    private int lastSpawnedIndex = -1;
    private int consecutiveCount;

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null)
                p = GameObject.Find("Player");
            if (p != null)
                playerTransform = p.transform;
        }

        if (spawnedParent == null)
        {
            GameObject plane = GameObject.Find("Plane");
            if (plane != null)
            {
                spawnedParent = plane.transform;
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameStarted || GameManager.Instance.IsGameOver)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        spawnTimer += Time.deltaTime;
        UpdateTightOffset(Time.deltaTime);

        float currentSpawnInterval = Mathf.Max(minSpawnInterval, startSpawnInterval - elapsedTime * spawnIntervalDecreasePerSecond);

        if (spawnTimer >= currentSpawnInterval)
        {
            SpawnPrefab();
            spawnTimer = 0f;
        }
    }

    private void UpdateTightOffset(float deltaTime)
    {
        if (isTightOffsetActive)
        {
            tightOffsetTimer += deltaTime;
            if (tightOffsetTimer >= tightOffsetDuration)
            {
                isTightOffsetActive = false;
                tightOffsetTimer = 0f;
                tightOffsetCooldownTimer = 0f;
            }

            return;
        }

        tightOffsetCooldownTimer += deltaTime;
        float currentTightOffsetInterval = Mathf.Max(
            tightOffsetMinInterval,
            tightOffsetStartInterval - elapsedTime * tightOffsetIntervalDecreasePerSecond);

        if (tightOffsetCooldownTimer >= currentTightOffsetInterval)
        {
            isTightOffsetActive = true;
            tightOffsetCooldownTimer = 0f;
            tightOffsetTimer = 0f;
        }
    }

    private void SpawnPrefab()
    {
        if (spawnEntries == null || spawnEntries.Length == 0)
        {
            return;
        }

        // Select all candidates that have reached startDelay; exclude those exceeding the consecutive limit
        float totalWeight = 0f;
        for (int i = 0; i < spawnEntries.Length; i++)
        {
            var e = spawnEntries[i];
            if (e == null || e.prefab == null) continue;
            if (elapsedTime < e.startDelay || e.weight <= 0f) continue;
            if (e.maxConsecutive > 0 && i == lastSpawnedIndex && consecutiveCount >= e.maxConsecutive) continue;
            totalWeight += e.weight;
        }

        if (totalWeight <= 0f) return;

        float r = Random.Range(0f, totalWeight);
        GameObject prefab = null;
        int selectedIndex = -1;
        float accum = 0f;
        for (int i = 0; i < spawnEntries.Length; i++)
        {
            var e = spawnEntries[i];
            if (e == null || e.prefab == null) continue;
            if (elapsedTime < e.startDelay || e.weight <= 0f) continue;
            if (e.maxConsecutive > 0 && i == lastSpawnedIndex && consecutiveCount >= e.maxConsecutive) continue;
            accum += e.weight;
            if (r <= accum)
            {
                prefab = e.prefab;
                selectedIndex = i;
                break;
            }
        }

        if (prefab == null) return;

        if (selectedIndex == lastSpawnedIndex)
            consecutiveCount++;
        else
        {
            lastSpawnedIndex = selectedIndex;
            consecutiveCount = 1;
        }

        float currentPositionOffset = isTightOffsetActive ? tightPositionOffset : maxPositionOffset;

        // random position offset
        Vector3 randomOffset = new Vector3(
            Random.Range(-currentPositionOffset, currentPositionOffset),
            0f,
            Random.Range(-currentPositionOffset, currentPositionOffset));

        // track player's XZ position, or use spawner position if player not available
        Vector3 basePosition = transform.position;
        if (playerTransform != null)
        {
            basePosition = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        }

        // apply bias towards player direction if requested
        if (playerTransform != null && ( !biasOnlyWhenTight || isTightOffsetActive ) && spawnBiasTowardsPlayer > 0f)
        {
            Vector3 spawnerPosXZ = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 playerPosXZ = new Vector3(playerTransform.position.x, 0f, playerTransform.position.z);
            Vector3 dirToPlayer = (playerPosXZ - spawnerPosXZ);
            if (dirToPlayer.sqrMagnitude > 0.0001f)
            {
                dirToPlayer.Normalize();
                // choose a forward distance biased towards player (0..currentPositionOffset)
                float forwardDist = Random.Range(0f, currentPositionOffset);
                Vector3 targetOffset = dirToPlayer * forwardDist;
                // keep some lateral randomness by lerping towards the target offset
                randomOffset = Vector3.Lerp(randomOffset, targetOffset, Mathf.Clamp01(spawnBiasTowardsPlayer));
            }
        }

        Quaternion randomRotation = Quaternion.Euler(
            Random.Range(-maxRotationOffset, maxRotationOffset),
            Random.Range(-maxRotationOffset, maxRotationOffset),
            Random.Range(-maxRotationOffset, maxRotationOffset));

        GameObject spawnedObject = Instantiate(prefab, basePosition + randomOffset, transform.rotation * randomRotation);

        if (spawnedParent != null)
        {
            spawnedObject.transform.SetParent(spawnedParent, true);
        }
    }
}