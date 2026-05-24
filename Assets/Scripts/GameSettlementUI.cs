// GameSettlementUI.cs
// ─────────────────────────────────────────────────────────────────────────────
// Manages the in-game HUD shown during level play:
//   • Elapsed time display
//   • Remaining hit count display
//   • Heart icons (spawned from a prefab, removed one-by-one as the player is hit)
//
// This script also acts as the "level starter":
//   When the level scene loads, GameSettlementUI.Start() fires GameManager.StartGame()
//   after all Awake()s have run — guaranteeing QuadrantSpawner's coroutine and
//   this UI are both subscribed before the event fires.
//
// WHAT WAS REMOVED (now lives in GameOverController.cs / GameOverScene):
//   • Game Over statement panel
//   • Restart prompt panel
//   • Restart keyboard input handling
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameSettlementUI : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Start Prompts (optional — unused in level scenes)")]
    [Tooltip("GameObjects shown before the game begins. Leave empty for level scenes.")]
    [SerializeField] private GameObject[] startPromptElements;

    [Header("Gameplay HUD")]
    [Tooltip("GameObjects that make up the HUD — shown once gameplay starts.")]
    [SerializeField] private GameObject[] gameplayHudElements;

    [Tooltip("Legacy Text fields for elapsed time. Use the TMP versions if possible.")]
    [SerializeField] private Text[]     gameplayElapsedTimeTexts;
    [SerializeField] private Text[]     gameplayRemainingHitCountTexts;

    [Tooltip("TextMeshPro fields for elapsed time and hit count.")]
    [SerializeField] private TMP_Text[] gameplayElapsedTimeTMPs;
    [SerializeField] private TMP_Text[] gameplayRemainingHitCountTMPs;

    [Header("Heart Display")]
    [Tooltip("Prefab instantiated once per heart. Drag your heart UI prefab here.")]
    [SerializeField] private GameObject heartPrefab;

    [Tooltip("Parent Transform where heart prefabs are instantiated (a Horizontal Layout Group works well).")]
    [SerializeField] private Transform heartsContainer;

    [Header("Player Reference")]
    [Tooltip("The Player in this scene. Leave blank to auto-find on Start.")]
    [SerializeField] private Player playerInputReference;

    // ── Private state ──────────────────────────────────────────────────────────
    private readonly List<GameObject> activeHearts    = new();
    private int                       lastRemainingHitCount = -1;

    // ══════════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Show start prompts by default — they're meant to be visible in StartScene.
        // They'll disappear when the scene transitions (or if the array is empty
        // in level scenes, nothing happens).
        ShowStartPrompts(true);
        SetActive(gameplayHudElements, false);
    }

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[GameSettlementUI] GameManager.Instance is null — make sure GameManager exists in StartScene.");
            return;
        }

        // Auto-find player if not assigned in Inspector
        if (playerInputReference == null)
            playerInputReference = FindFirstObjectByType<Player>();

        // Subscribe to GameManager events.
        // These subscriptions happen in Start(), which runs AFTER all Awake()s,
        // so QuadrantSpawner's coroutine is already waiting by this point.
        GameManager.Instance.GameStarted      += HandleGameStarted;
        GameManager.Instance.GameOverTriggered += HandleGameOver;

        // Start the game in this scene.
        // resetStats: false → carry forward the hit count (hearts) from previous levels.
        if (!GameManager.Instance.IsGameStarted && !GameManager.Instance.IsGameOver)
        {
            GameManager.Instance.StartGame(resetStats: false);
        }
    }

    private void OnDestroy()
    {
        // Always unsubscribe to avoid callbacks on destroyed objects
        if (GameManager.Instance == null) return;
        GameManager.Instance.GameStarted       -= HandleGameStarted;
        GameManager.Instance.GameOverTriggered -= HandleGameOver;
    }

    private void Update()
    {
        // Refresh the time and remaining-hits text every frame while playing
        if (GameManager.Instance != null &&
            GameManager.Instance.IsGameStarted &&
            !GameManager.Instance.IsGameOver)
        {
            RefreshGameplayHud();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ══════════════════════════════════════════════════════════════════════════

    // Called when GameManager fires GameStarted.
    // Shows the HUD and populates the heart row.
    private void HandleGameStarted()
    {
        Debug.Log("[GameSettlementUI] HandleGameStarted — showing HUD.");
        gameObject.SetActive(true);
        SetActive(gameplayHudElements, true);

        // Only spawn hearts at the very start of a fresh session (no hearts exist yet).
        // Between levels hearts persist as-is — just sync the display to the current
        // remaining count without rebuilding the row from scratch.
        if (activeHearts.Count == 0)
            SpawnHearts(GameManager.Instance.MaxHitCount);
        else
            UpdateHearts(GameManager.Instance.RemainingHitCount);

        RefreshGameplayHud();
    }

    // Called when GameManager fires GameOverTriggered.
    // The scene transition is handled by GameManager itself; we just clean up the HUD.
    private void HandleGameOver(GameManager.GameResult result)
    {
        // Hide the entire canvas so nothing bleeds into GameOverScene or StartScene.
        // This is more reliable than hiding individual elements, which depends on
        // the gameplayHudElements array being fully wired up in the Inspector.
        gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HUD REFRESH
    // ══════════════════════════════════════════════════════════════════════════

    private void RefreshGameplayHud()
    {
        if (GameManager.Instance == null) return;

        // Update time display
        string timeStr = $"Time: {GameManager.Instance.ElapsedTime:F1} s";
        SetTexts(gameplayElapsedTimeTexts, timeStr);
        SetTexts(gameplayElapsedTimeTMPs,  timeStr);

        // Update hit-count display
        string hitsStr = $"Remaining Hits: {GameManager.Instance.RemainingHitCount}";
        SetTexts(gameplayRemainingHitCountTexts, hitsStr);
        SetTexts(gameplayRemainingHitCountTMPs,  hitsStr);

        // Sync heart icons with current remaining count
        int remaining = GameManager.Instance.RemainingHitCount;
        if (remaining != lastRemainingHitCount)
        {
            UpdateHearts(remaining);
            lastRemainingHitCount = remaining;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HEART SYSTEM
    // ══════════════════════════════════════════════════════════════════════════

    // Instantiate 'count' heart icons as children of heartsContainer.
    private void SpawnHearts(int count)
    {
        ClearHearts();
        if (heartPrefab == null || heartsContainer == null) return;

        for (int i = 0; i < count; i++)
            activeHearts.Add(Instantiate(heartPrefab, heartsContainer));

        lastRemainingHitCount = count;
    }

    // Remove heart icons from the right until the count matches 'remaining'.
    private void UpdateHearts(int remaining)
    {
        while (activeHearts.Count > remaining)
        {
            int last = activeHearts.Count - 1;
            Destroy(activeHearts[last]);
            activeHearts.RemoveAt(last);
        }
    }

    // Destroy all heart icons (used when clearing the HUD).
    private void ClearHearts()
    {
        foreach (var heart in activeHearts)
            if (heart != null) Destroy(heart);
        activeHearts.Clear();
        lastRemainingHitCount = -1;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private void ClearGameplayHud()
    {
        SetTexts(gameplayElapsedTimeTexts,        "");
        SetTexts(gameplayElapsedTimeTMPs,          "");
        SetTexts(gameplayRemainingHitCountTexts,  "");
        SetTexts(gameplayRemainingHitCountTMPs,   "");
        ClearHearts();
    }

    public void HideStartPrompts() => SetActive(startPromptElements, false);

    private void ShowStartPrompts(bool visible)
    {
        SetActive(startPromptElements, visible);
    }

    private static void SetActive(GameObject[] targets, bool visible)
    {
        if (targets == null || targets.Length == 0) return;
        foreach (var go in targets)
            if (go != null) go.SetActive(visible);
    }

    private static void SetTexts(Text[] targets, string content)
    {
        if (targets == null) return;
        foreach (var t in targets)
            if (t != null) t.text = content;
    }

    private static void SetTexts(TMP_Text[] targets, string content)
    {
        if (targets == null) return;
        foreach (var t in targets)
            if (t != null) t.text = content;
    }
}
