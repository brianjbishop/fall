// ObstacleSpawner.cs
// ─────────────────────────────────────────────────────────────────────────────
// Spawns falling objects above the play area according to a designer-authored
// sequence of SpawnPhases.
//
// HOW LEVEL DESIGN WORKS
//   1. Add this component to a GameObject in your level scene.
//   2. Expand the "Phase Sequence" array in the Inspector.
//   3. Add as many SpawnPhase entries as you like — they play back in order.
//   4. Each phase lets you choose:
//        • Which quadrant(s) receive objects (check one or more boxes)
//        • How long the phase lasts
//        • Starting and minimum spawn interval (lower = faster = harder)
//        • How quickly the interval accelerates toward the minimum
//   5. When all phases finish, the level ends and the game moves to CountdownScene.
//
// QUADRANT LAYOUT (clockwise from top-right):
//
//   Q4 (top-left)   │  Q1 (top-right)
//   ────────────────┼────────────────
//   Q3 (bottom-left)│  Q2 (bottom-right)
//
// COROUTINE FLOW (what "coroutine" means here)
//   Instead of checking timers every frame in Update(), we use a coroutine:
//   a function that pauses itself with "yield return null" (come back next frame)
//   or "yield return new WaitForSeconds(N)" (come back after N seconds).
//   This lets the phase sequence read like a plain list of instructions:
//   "run phase 0, then run phase 1, then run phase 2, then done."
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    // ── SpawnEntry ─────────────────────────────────────────────────────────────
    // One prefab in the pool with its spawn weight and optional constraints.
    // Higher weight = selected more often; startDelay lets early-game items
    // appear first before harder ones unlock.
    [System.Serializable]
    private class SpawnEntry
    {
        public GameObject prefab;

        [Tooltip("Spawn weight — higher value = picked more often.")]
        public float weight = 1f;

        [Range(0f, 120f)]
        [Tooltip("Seconds after level start before this prefab can appear. 0 = available immediately.")]
        public float startDelay = 0f;

        [Tooltip("Max times this prefab spawns in a row (0 = unlimited).")]
        public int maxConsecutive = 0;
    }

    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Prefab Pool")]
    [Tooltip("The objects that can be spawned. Add prefabs here and tune their weights.")]
    [SerializeField] private SpawnEntry[] spawnEntries;

    [Header("Spawn Rectangle")]
    [Tooltip("Center of the spawn rectangle. Leave empty to use this object's position.")]
    [SerializeField] private Transform rectCenter;

    [Tooltip("Half the rectangle's width along the X axis (total width = 2 × this).")]
    [SerializeField] private float halfWidth  = 7f;

    [Tooltip("Half the rectangle's height along the Z axis (total height = 2 × this).")]
    [SerializeField] private float halfHeight = 6f;

    [Header("Parenting")]
    [Tooltip("Optional parent Transform for spawned objects (keeps the Hierarchy tidy).")]
    [SerializeField] private Transform spawnedParent;

    [Header("Random Rotation")]
    [Tooltip("Max degrees of random rotation applied to each spawned object.")]
    [SerializeField] private float maxRotationOffset = 15f;

    [Header("Phase Sequence")]
    [Tooltip("Floor for the spawn interval — no phase can spawn faster than this regardless of acceleration.")]
    [SerializeField] private float minSpawnInterval = 0.33f;

    [Tooltip("The level pattern. Each element is one time window with its own quadrant " +
             "targets and spawn rate. They play in order; when the last one finishes the " +
             "level ends.")]
    [SerializeField] private SpawnPhase[] phases;

    // ── Runtime state ──────────────────────────────────────────────────────────
    // These are set/cleared as the coroutine moves between phases.
    // currentPhaseIndex and activeQuadrants are used by the Gizmo so you can
    // see which phase is active while the game runs in the Editor.

    private int   currentPhaseIndex = -1;   // which phase is running (-1 = not started)
    private int[] activeQuadrants;           // indices of quadrants active in current phase
    private int   lastSpawnedIndex = -1;     // tracks consecutive spawns for maxConsecutive
    private int   consecutiveCount = 0;

    // ══════════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Start()
    {
        // Kick off the phase sequencer coroutine immediately.
        // It will wait internally until GameManager signals game start,
        // then run through the phases one by one.
        StartCoroutine(RunPhaseSequence());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PHASE SEQUENCER COROUTINE
    // ══════════════════════════════════════════════════════════════════════════

    private IEnumerator RunPhaseSequence()
    {
        // ── Wait for game start ───────────────────────────────────────────────
        // GameSettlementUI.Start() calls GameManager.StartGame() after subscribing
        // to events. We poll here once per frame until that happens.
        // "yield return null" means: pause this function, let the rest of the game
        // run for one frame, then come back and check again.
        while (GameManager.Instance == null || !GameManager.Instance.IsGameStarted)
            yield return null;

        // Bail early if no phases are configured (prevents a silent do-nothing)
        if (phases == null || phases.Length == 0)
        {
            Debug.LogWarning("[ObstacleSpawner] No phases configured — nothing will spawn.");
            yield break;
        }

        // Reset consecutive-spawn tracking at the start of a fresh level
        lastSpawnedIndex = -1;
        consecutiveCount = 0;

        // Record when this level started so startDelay on prefabs is measured
        // from level start, not total session time.
        float levelStartTime = Time.time;

        // ── Phase loop ────────────────────────────────────────────────────────
        // Iterate through each SpawnPhase in order.
        for (int i = 0; i < phases.Length; i++)
        {
            SpawnPhase phase = phases[i];
            currentPhaseIndex = i;

            // Convert the checked bools into a plain int array once per phase.
            // SpawnOne() picks randomly from this list each time it fires.
            activeQuadrants = phase.GetActiveQuadrants();

            Debug.Log($"[ObstacleSpawner] Starting phase {i}: \"{phase.phaseName}\" " +
                      $"({activeQuadrants.Length} quadrant(s), {phase.duration}s)");

            float phaseElapsed = 0f;  // how long we've been in this phase
            float spawnTimer   = 0f;  // counts up toward the next spawn event

            // ── Inner tick loop ───────────────────────────────────────────────
            // Run frame-by-frame until the phase duration is exhausted.
            while (phaseElapsed < phase.duration)
            {
                // If the game ended mid-phase (player lost last heart), stop everything.
                if (GameManager.Instance == null || GameManager.Instance.IsGameOver)
                    yield break;

                float dt = Time.deltaTime;
                phaseElapsed += dt;
                spawnTimer   += dt;

                // Calculate the current spawn interval.
                // It starts at startSpawnInterval and shrinks by
                // spawnIntervalDecreasePerSecond each second, floored at minSpawnInterval.
                float interval = Mathf.Max(
                    minSpawnInterval,
                    phase.StartSpawnInterval - phaseElapsed * phase.SpawnIntervalDecreasePerSecond);

                // Fire a spawn event when enough time has accumulated.
                if (spawnTimer >= interval && activeQuadrants.Length > 0)
                {
                    SpawnOne(activeQuadrants, Time.time - levelStartTime);
                    spawnTimer = 0f;  // reset timer for next spawn
                }

                // Pause here and resume next frame.
                yield return null;
            }
        }

        // ── All phases finished ───────────────────────────────────────────────
        // Tell GameManager the level is done; it will load CountdownScene.
        Debug.Log("[ObstacleSpawner] All phases complete — signalling level complete.");
        GameManager.Instance?.OnLevelComplete();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SPAWNING
    // ══════════════════════════════════════════════════════════════════════════

    // Picks a random prefab from the weighted pool and instantiates it
    // in a randomly chosen quadrant from the active set.
    // timeSinceLevel = seconds since this level started (not total session time).
    // Used to check startDelay so prefabs unlock relative to the current level.
    private void SpawnOne(int[] quadrants, float timeSinceLevel)
    {
        if (spawnEntries == null || spawnEntries.Length == 0) return;

        // ── Weighted random prefab selection ──────────────────────────────────
        // Sum the weights of all eligible entries, then pick a random point in
        // that range. Walk the list again to find which entry the point lands in.
        // Entries are ineligible if: prefab is null, startDelay hasn't elapsed,
        // or maxConsecutive would be exceeded.
        float elapsedTotal = timeSinceLevel;

        float totalWeight = 0f;
        for (int i = 0; i < spawnEntries.Length; i++)
        {
            var e = spawnEntries[i];
            if (e == null || e.prefab == null) continue;
            if (elapsedTotal < e.startDelay || e.weight <= 0f) continue;
            if (e.maxConsecutive > 0 && i == lastSpawnedIndex && consecutiveCount >= e.maxConsecutive) continue;
            totalWeight += e.weight;
        }
        if (totalWeight <= 0f) return;

        float r     = Random.Range(0f, totalWeight);
        float accum = 0f;
        GameObject prefab        = null;
        int        selectedIndex = -1;

        for (int i = 0; i < spawnEntries.Length; i++)
        {
            var e = spawnEntries[i];
            if (e == null || e.prefab == null) continue;
            if (elapsedTotal < e.startDelay || e.weight <= 0f) continue;
            if (e.maxConsecutive > 0 && i == lastSpawnedIndex && consecutiveCount >= e.maxConsecutive) continue;

            accum += e.weight;
            if (r <= accum) { prefab = e.prefab; selectedIndex = i; break; }
        }
        if (prefab == null) return;

        // Update consecutive-spawn tracker
        if (selectedIndex == lastSpawnedIndex) consecutiveCount++;
        else { lastSpawnedIndex = selectedIndex; consecutiveCount = 1; }

        // ── Pick a quadrant and spawn ─────────────────────────────────────────
        // When multiple quadrants are active, one is chosen at random each spawn.
        int q = quadrants[Random.Range(0, quadrants.Length)];
        Vector3 spawnPos = GetRandomPosInQuadrant(q);

        // Apply a small random rotation so objects don't all look identical
        Quaternion rot = transform.rotation * Quaternion.Euler(
            Random.Range(-maxRotationOffset, maxRotationOffset),
            Random.Range(-maxRotationOffset, maxRotationOffset),
            Random.Range(-maxRotationOffset, maxRotationOffset));

        GameObject obj = Instantiate(prefab, spawnPos, rot);

        // Parent spawned objects for a cleaner Hierarchy view
        if (spawnedParent != null)
            obj.transform.SetParent(spawnedParent, worldPositionStays: true);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // QUADRANT GEOMETRY
    // ══════════════════════════════════════════════════════════════════════════

    // Returns a random world position inside the requested quadrant.
    //
    //   Q0 top-right:     x ∈ [0, halfWidth],  z ∈ [0, halfHeight]
    //   Q1 top-left:      x ∈ [-halfWidth, 0], z ∈ [0, halfHeight]
    //   Q2 bottom-left:   x ∈ [-halfWidth, 0], z ∈ [-halfHeight, 0]
    //   Q3 bottom-right:  x ∈ [0, halfWidth],  z ∈ [-halfHeight, 0]
    private Vector3 GetRandomPosInQuadrant(int q)
    {
        Vector3 center = rectCenter != null ? rectCenter.position : transform.position;

        float xMin, xMax, zMin, zMax;
        switch (q)
        {
            case 0: xMin = 0;          xMax = halfWidth;  zMin = 0;           zMax = halfHeight; break;
            case 1: xMin = -halfWidth; xMax = 0;          zMin = 0;           zMax = halfHeight; break;
            case 2: xMin = -halfWidth; xMax = 0;          zMin = -halfHeight; zMax = 0;          break;
            default: xMin = 0;         xMax = halfWidth;  zMin = -halfHeight; zMax = 0;          break;
        }

        return new Vector3(
            center.x + Random.Range(xMin, xMax),
            center.y,
            center.z + Random.Range(zMin, zMax));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GIZMOS (Editor visualisation — invisible at runtime)
    // ══════════════════════════════════════════════════════════════════════════

    private void OnDrawGizmosSelected()
    {
        Vector3 c  = rectCenter != null ? rectCenter.position : transform.position;
        float   hw = halfWidth;
        float   hh = halfHeight;

        // Outer border of the full spawn rectangle
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
        DrawRect(c, hw, hh);

        // Cross lines dividing the rectangle into four quadrants
        Gizmos.DrawLine(c + new Vector3(-hw, 0f, 0f), c + new Vector3(hw, 0f, 0f));
        Gizmos.DrawLine(c + new Vector3(0f, 0f, -hh), c + new Vector3(0f, 0f, hh));

        // Highlight which quadrant(s) are currently active.
        // In Play mode: shows all active quadrants for the current phase.
        // In Edit mode: highlights Q0 as a reference preview.
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.25f);
        if (Application.isPlaying && activeQuadrants != null)
        {
            foreach (int q in activeQuadrants)
                DrawQuadrantFill(c, hw, hh, q);
        }
        else
        {
            DrawQuadrantFill(c, hw, hh, 0);  // preview Q0 in edit mode
        }
    }

    private static void DrawRect(Vector3 c, float hw, float hh)
    {
        Vector3 tl = c + new Vector3(-hw, 0f,  hh);
        Vector3 tr = c + new Vector3( hw, 0f,  hh);
        Vector3 br = c + new Vector3( hw, 0f, -hh);
        Vector3 bl = c + new Vector3(-hw, 0f, -hh);
        Gizmos.DrawLine(tl, tr); Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl); Gizmos.DrawLine(bl, tl);
    }

    private static void DrawQuadrantFill(Vector3 c, float hw, float hh, int q)
    {
        float xMin, xMax, zMin, zMax;
        switch (q)
        {
            case 0: xMin = 0;    xMax = hw;  zMin = 0;   zMax = hh;  break;
            case 1: xMin = -hw;  xMax = 0;   zMin = 0;   zMax = hh;  break;
            case 2: xMin = -hw;  xMax = 0;   zMin = -hh; zMax = 0;   break;
            default: xMin = 0;   xMax = hw;  zMin = -hh; zMax = 0;   break;
        }
        Vector3 tl = c + new Vector3(xMin, 0f, zMax);
        Vector3 tr = c + new Vector3(xMax, 0f, zMax);
        Vector3 br = c + new Vector3(xMax, 0f, zMin);
        Vector3 bl = c + new Vector3(xMin, 0f, zMin);
        Gizmos.DrawLine(tl, tr); Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl); Gizmos.DrawLine(bl, tl);
    }
}
