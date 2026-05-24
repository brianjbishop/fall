using UnityEngine;

public class CannonSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject prefab;

    [Header("Spawn Origin")]
    [Tooltip("Uses this Transform's position as the muzzle; leave empty to use own position")]
    [SerializeField] private Transform spawnOrigin;

    [Header("Spawn Interval")]
    [SerializeField] private float startSpawnInterval = 2f;
    [SerializeField] private float minSpawnInterval   = 0.25f;
    [Tooltip("Interval reduction per second")]
    [SerializeField] private float intervalDecreasePerSecond = 0.05f;

    [Header("Target Rectangle (world coordinates, centered at origin)")]
    [Tooltip("Target rectangle half-width (X axis; total width = 2 x this value)")]
    [SerializeField] private float targetHalfWidth  = 7f;   // total width 14
    [Tooltip("Target rectangle half-height (Z axis; total height = 2 x this value)")]
    [SerializeField] private float targetHalfHeight = 6f;   // total height 12
    [Tooltip("Y height of the target rectangle")]
    [SerializeField] private float targetY = 0f;

    [Header("Parent Node (Optional)")]
    [SerializeField] private Transform spawnedParent;

    private float elapsedTime;
    private float spawnTimer;

    private void Update()
    {
        if (GameManager.Instance == null ||
            !GameManager.Instance.IsGameStarted ||
            GameManager.Instance.IsGameOver)
            return;

        elapsedTime += Time.deltaTime;
        spawnTimer  += Time.deltaTime;

        float interval = Mathf.Max(minSpawnInterval,
            startSpawnInterval - elapsedTime * intervalDecreasePerSecond);

        if (spawnTimer >= interval)
        {
            SpawnOne();
            spawnTimer = 0f;
        }
    }

    private void SpawnOne()
    {
        if (prefab == null) return;

        Vector3 origin = spawnOrigin != null ? spawnOrigin.position : transform.position;

        // Random point within the target rectangle
        Vector3 target = new Vector3(
            Random.Range(-targetHalfWidth,  targetHalfWidth),
            targetY,
            Random.Range(-targetHalfHeight, targetHalfHeight));

        Vector3 direction = (target - origin).normalized;

        GameObject obj = Instantiate(prefab, origin, Quaternion.LookRotation(direction));

        // Pass direction to the spawned object's movement script
        ProjectileMover mover = obj.GetComponent<ProjectileMover>();
        if (mover != null)
            mover.Init(direction);

        if (spawnedParent != null)
            obj.transform.SetParent(spawnedParent, true);
    }

    private void OnDrawGizmosSelected()
    {
        // Target rectangle
        Gizmos.color = new Color(1f, 0.8f, 0f, 1f);
        Vector3 c  = new Vector3(0f, targetY, 0f);
        Vector3 tl = c + new Vector3(-targetHalfWidth, 0f,  targetHalfHeight);
        Vector3 tr = c + new Vector3( targetHalfWidth, 0f,  targetHalfHeight);
        Vector3 br = c + new Vector3( targetHalfWidth, 0f, -targetHalfHeight);
        Vector3 bl = c + new Vector3(-targetHalfWidth, 0f, -targetHalfHeight);
        Gizmos.DrawLine(tl, tr); Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl); Gizmos.DrawLine(bl, tl);

        // Line from muzzle to rectangle center
        Vector3 origin = spawnOrigin != null ? spawnOrigin.position : transform.position;
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
        Gizmos.DrawLine(origin, c);
    }
}
