// SpawnPhase.cs
// ─────────────────────────────────────────────────────────────────────────────
// One "phase" of a level — a time window during which specific quadrant(s)
// receive falling objects at a controlled rate.
//
// Phases are stacked into an array on ObstacleSpawner and play back in order.
// This is where all your level design lives.
//
// QUADRANT LAYOUT (clockwise from top-right):
//
//   Q4 (top-left)   │  Q1 (top-right)
//   ────────────────┼────────────────
//   Q3 (bottom-left)│  Q2 (bottom-right)
//
// ─────────────────────────────────────────────────────────────────────────────

[System.Serializable]
public struct SpawnPhase
{
    // ── Inspector label ───────────────────────────────────────────────────────
    [UnityEngine.Tooltip("Friendly name — helps you read the sequence at a glance.")]
    public string phaseName;

    // ── Active quadrants (clockwise from top-right) ───────────────────────────
    // Check any combination. Multiple checked = each spawn event randomly picks
    // one of the active zones.
    //
    //   Q4 (top-left)   │  Q1 (top-right)
    //   ────────────────┼────────────────
    //   Q3 (bottom-left)│  Q2 (bottom-right)

    [UnityEngine.Tooltip("Top-right zone active during this phase.")]
    public bool q1Active;   // top-right

    [UnityEngine.Tooltip("Bottom-right zone active during this phase.")]
    public bool q2Active;   // bottom-right

    [UnityEngine.Tooltip("Bottom-left zone active during this phase.")]
    public bool q3Active;   // bottom-left

    [UnityEngine.Tooltip("Top-left zone active during this phase.")]
    public bool q4Active;   // top-left

    // ── Duration ──────────────────────────────────────────────────────────────
    [UnityEngine.Range(1, 60)]
    [UnityEngine.Tooltip("How long this phase lasts in seconds before moving to the next one.")]
    public float duration;

    // ── Spawn interval ────────────────────────────────────────────────────────
    // Interval = seconds between each spawned object.
    // Starts at startSpawnInterval, shrinks toward minSpawnInterval over time.
    // Lower interval = more objects per second = harder.

    // ── Spawn rate ────────────────────────────────────────────────────────────
    // IMPORTANT — this is intentionally inverted from what you might expect:
    //   Low number (1)  = LOW spawn rate = objects appear SLOWLY (long gap between spawns)
    //   High number (10) = HIGH spawn rate = objects appear QUICKLY (short gap between spawns)
    //
    // Internally this maps to a seconds-between-spawns interval value:
    //   1  → 3.0s between spawns  (very slow, relaxed)
    //   5  → 1.5s between spawns  (moderate)
    //   10 → 0.0s between spawns  (maximum rate, back-to-back spawns)
    //
    // Note: minSpawnInterval acts as a floor so 10 never literally means
    // infinite spawns — set minSpawnInterval to your lowest comfortable value.
    //
    // Formula: Lerp(3.0, 0.0, (spawnRate - 1) / 9)
    [UnityEngine.Range(1f, 10f)]
    [UnityEngine.Tooltip("How fast objects spawn at the START of this phase.\n1.0 = slow (relaxed), 10.0 = fast (intense).\nNote: accelerationRate will push this higher over time.")]
    public float spawnRate;

    // Converts the 1-10 spawn rate to actual seconds between spawns.
    // Higher spawnRate = lower interval = more objects per second.
    public float StartSpawnInterval => UnityEngine.Mathf.Lerp(3.0f, 0.0f, (spawnRate - 1) / 9f);

    // ── Acceleration rate ─────────────────────────────────────────────────────
    // 1-10 slider. Controls how quickly the interval shrinks over time.
    //   1  = barely accelerates (gentle, constant feel)
    //   5  = moderate ramp
    //   10 = aggressive ramp (gets intense fast)
    [UnityEngine.Range(1f, 10f)]
    [UnityEngine.Tooltip("How aggressively spawning speeds up. 1.0 = gentle, 10.0 = aggressive.")]
    public float accelerationRate;

    // Converts the 1-10 rating to the actual seconds-per-second decrease value.
    // 1 → 0.03   5 → 0.15   10 → 0.30
    public float SpawnIntervalDecreasePerSecond => accelerationRate * 0.03f;

    // ── Helper ────────────────────────────────────────────────────────────────
    // Returns the internal geometry indices for whichever quadrants are checked.
    // Called once at the start of each phase; result is cached in ObstacleSpawner.
    //
    // Internal geometry indices (used by GetRandomPosInQuadrant):
    //   0 = top-right   1 = top-left   2 = bottom-left   3 = bottom-right
    public int[] GetActiveQuadrants()
    {
        var list = new System.Collections.Generic.List<int>(4);
        if (q1Active) list.Add(0);  // Q1 top-right
        if (q2Active) list.Add(3);  // Q2 bottom-right
        if (q3Active) list.Add(2);  // Q3 bottom-left
        if (q4Active) list.Add(1);  // Q4 top-left
        return list.ToArray();
    }
}
