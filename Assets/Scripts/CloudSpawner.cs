using UnityEngine;
using System.Collections;

public class CloudSpawner : MonoBehaviour
{
    [Header("Prefab Settings")]
    public GameObject prefabToSpawn;

    [Header("Spawn Interval")]
    public float spawnInterval = 3f;
    public float initialDelay  = 1f;

    [Header("Position Offset")]
    [Tooltip("Spawn center offset relative to this object's Transform (XYZ)")]
    public Vector3 centerOffset = Vector3.zero;

    [Header("Inner Rectangle Exclusion Zone (16:9)")]
    [Tooltip("Inner rectangle half-width; half-height is automatically = half-width x 9/16")]
    public float innerHalfWidth = 8f;

    [Header("Outer Rectangle Spawn Area (16:9)")]
    [Tooltip("Outer rectangle half-width; half-height is automatically = half-width x 9/16")]
    public float outerHalfWidth = 20f;

    float InnerHalfHeight => innerHalfWidth * 9f / 16f;
    float OuterHalfHeight => outerHalfWidth  * 9f / 16f;

    bool _initialized;

    void Start()
    {
        _initialized = true;
        Subscribe();
        if (GameManager.Instance != null && GameManager.Instance.IsGameStarted)
            StartCoroutine(SpawnRoutine());
    }

    void OnEnable()
    {
        // Re-subscribe only if Start has already run (object was disabled and re-enabled)
        if (_initialized) Subscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Subscribe()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.GameStarted       += OnGameStarted;
        GameManager.Instance.GameOverTriggered += OnGameOver;
    }

    void Unsubscribe()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.GameStarted       -= OnGameStarted;
        GameManager.Instance.GameOverTriggered -= OnGameOver;
    }

    void OnGameStarted()
    {
        StopAllCoroutines();
        StartCoroutine(SpawnRoutine());
    }

    void OnGameOver(GameManager.GameResult _)
    {
        StopAllCoroutines();
    }

    IEnumerator SpawnRoutine()
    {
        // Wait until we're in an actual level scene (not Start, Countdown, or GameOver)
        // and the game is running. This prevents clouds from appearing during the
        // intro drop or countdown even if GameStarted fires in those scenes.
        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameStarted)
            {
                int scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
                bool isLevelScene = scene != SceneIndex.Start &&
                                    scene != SceneIndex.Countdown &&
                                    scene != SceneIndex.GameOver;
                if (isLevelScene) break;
            }
            yield return null;
        }

        yield return new WaitForSeconds(initialDelay);
        while (this != null && gameObject.activeInHierarchy)
        {
            Spawn();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void Spawn()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[CloudSpawner] prefabToSpawn is not assigned!");
            return;
        }

        Vector3 center = transform.position + centerOffset;
        Vector3 pos    = SampleRingPosition(center);

        // Parent the cloud to this spawner so it lives in the DontDestroyOnLoad
        // hierarchy and survives scene transitions (countdown, level loads, etc.)
        GameObject cloud = Instantiate(prefabToSpawn, pos, Quaternion.Euler(0f, 0f, 0f));
        cloud.transform.SetParent(transform, worldPositionStays: true);
    }

    // Uniformly sample from the ring area between the outer and inner rectangles
    // Divide the ring into four rectangular strips (top/bottom/left/right) and select by area-weighted random
    Vector3 SampleRingPosition(Vector3 center)
    {
        float iW = innerHalfWidth;
        float iH = InnerHalfHeight;
        float oW = outerHalfWidth;
        float oH = OuterHalfHeight;

        // Top strip: x∈[-oW, oW], z∈[iH, oH]
        float aTop    = 2f * oW * (oH - iH);
        // Bottom strip: x∈[-oW, oW], z∈[-oH, -iH]
        float aBottom = aTop;
        // Left strip: x∈[-oW, -iW], z∈[-iH, iH]
        float aSide   = (oW - iW) * 2f * iH;
        // Right strip: x∈[iW, oW], z∈[-iH, iH]
        float total   = aTop + aBottom + 2f * aSide;

        float x, z;
        float r = Random.Range(0f, total);

        if (r < aTop)
        {
            x = Random.Range(-oW, oW);
            z = Random.Range(iH, oH);
        }
        else if (r < aTop + aBottom)
        {
            x = Random.Range(-oW, oW);
            z = Random.Range(-oH, -iH);
        }
        else if (r < aTop + aBottom + aSide)
        {
            x = Random.Range(-oW, -iW);
            z = Random.Range(-iH, iH);
        }
        else
        {
            x = Random.Range(iW, oW);
            z = Random.Range(-iH, iH);
        }

        return center + new Vector3(x, 0f, z);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position + centerOffset;

        Gizmos.color = new Color(1f, 0.25f, 0.25f, 1f);
        DrawRect(center, innerHalfWidth, InnerHalfHeight);

        Gizmos.color = new Color(0.25f, 1f, 0.25f, 1f);
        DrawRect(center, outerHalfWidth, OuterHalfHeight);
    }

    static void DrawRect(Vector3 c, float hw, float hh)
    {
        Vector3 tl = c + new Vector3(-hw, 0f,  hh);
        Vector3 tr = c + new Vector3( hw, 0f,  hh);
        Vector3 br = c + new Vector3( hw, 0f, -hh);
        Vector3 bl = c + new Vector3(-hw, 0f, -hh);
        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl);
        Gizmos.DrawLine(bl, tl);
    }
}
