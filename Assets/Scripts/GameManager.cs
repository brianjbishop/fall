// GameManager.cs
// ─────────────────────────────────────────────────────────────────────────────
// Singleton that owns all game state and drives scene transitions.
//
// It persists across every scene load (DontDestroyOnLoad) so other scripts can
// always reach it via GameManager.Instance no matter which scene is active.
//
// KEY RESPONSIBILITIES
//   • Track elapsed time, hit count, and high score
//   • Tell the rest of the game when play starts / ends (events)
//   • Own all scene-loading logic — nothing else should call SceneManager directly
//   • Hold a reference to the live Player (updated each scene via RegisterPlayer)
//
// PRIMARY INSPECTOR VARIABLES
//   maxHitCount       — how many hearts the player has (change this to adjust difficulty)
//   levelSceneIndices — build indices of your level scenes (add new levels here)
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ── GameResult ─────────────────────────────────────────────────────────────
    // Snapshot passed to listeners when the game ends.
    // Read-only so nothing can alter it after the fact.
    public readonly struct GameResult
    {
        public GameResult(float elapsedTime, int playerHitCount, int remainingHitCount, int highScore)
        {
            ElapsedTime       = elapsedTime;
            PlayerHitCount    = playerHitCount;
            RemainingHitCount = remainingHitCount;
            HighScore         = highScore;
        }

        public float ElapsedTime       { get; }
        public int   PlayerHitCount    { get; }
        public int   RemainingHitCount { get; }
        public int   HighScore         { get; }
    }

    // ── Singleton ──────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────────────────
    // Other scripts subscribe to these to react to state changes.
    //   GameStarted        — fires when gameplay begins in a level scene
    //   GameOverTriggered  — fires when the last heart is lost
    public event Action              GameStarted;
    public event Action<GameResult>  GameOverTriggered;

    // ── Public read-only state ─────────────────────────────────────────────────
    public float ElapsedTime       { get; private set; }   // seconds since last StartGame
    public int   PlayerHitCount    { get; private set; }   // hearts lost this session
    public int   RemainingHitCount => maxHitCount - PlayerHitCount;
    public int   HighScore         => currentHighScore;
    public int   MaxHitCount       => maxHitCount;          // total hearts (read by UI to spawn heart icons)
    public bool  IsGameOver        { get; private set; }
    public bool  IsGameStarted     { get; private set; }
    public bool  IsPaused          { get; private set; }

    // ── Player reference ──────────────────────────────────────────────────────
    // The Player script in the active scene calls RegisterPlayer(this) in its
    // Awake(), keeping this reference current as scenes load and unload.
    public Player PlayerRef { get; private set; }

    // ── Inspector variables ────────────────────────────────────────────────────
    [Header("Persistence")]
    [Tooltip("Keep this GameObject alive when loading a new scene. Leave ON for normal play.")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Hearts / Difficulty")]
    [Tooltip("How many times the player can be hit before game over. THIS IS YOUR HEART COUNT.")]
    [SerializeField] private int maxHitCount = 3;

    [Header("Level Scenes")]
    [Tooltip("Build indices of every level scene. CountdownScene picks from this list at random. " +
             "Add a new level: create its scene, note its build index, add the number here.")]
    [SerializeField] public int[] levelSceneIndices = { 2, 3 };

    [Header("Global Speed")]
    [Tooltip("Speed multiplier applied to all obstacles at the start of the first level.")]
    [SerializeField] private float speedMultiplierStart = 1f;
    [Tooltip("Added to the multiplier each time a level completes. 0.1 = 10% faster per level.")]
    [SerializeField] private float speedMultiplierPerLevel = 0.05f;

    // Readable by FlyingUpward and MoveUpward to scale their speed each frame.
    public float SpeedMultiplier { get; private set; } = 1f;

    // ── Private state ──────────────────────────────────────────────────────────
    private int currentHighScore;
    private readonly System.Collections.Generic.List<GameObject> persistentObjects = new();

    // ══════════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Singleton pattern — only one GameManager ever exists.
        // If a duplicate appears (e.g. accidentally placed in a level scene),
        // destroy it immediately so the original persists cleanly.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentHighScore = PlayerPrefs.GetInt("HighScore", 0);

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // Tick elapsed time only while actually playing.
        if (!IsGameStarted || IsGameOver) return;
        ElapsedTime += Time.deltaTime;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PLAYER REGISTRATION
    // ══════════════════════════════════════════════════════════════════════════

    // Called by Player.Awake() every time a level scene loads.
    // Keeps PlayerRef pointing at whichever Player is currently alive.
    public void RegisterPlayer(Player player)
    {
        PlayerRef = player;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PERSISTENT OBJECT CLEANUP
    // ══════════════════════════════════════════════════════════════════════════

    // Called by PersistObject.Awake() whenever a persistent object is created.
    public void RegisterPersistentObject(GameObject go)
    {
        if (go != null && !persistentObjects.Contains(go))
            persistentObjects.Add(go);
    }

    // Destroys every registered persistent object and clears the list.
    // GameManager is never registered here (it doesn't use PersistObject)
    // so it always survives. Call this on restart for a clean slate.
    public void DestroyAllPersistent()
    {
        foreach (GameObject go in persistentObjects)
        {
            if (go != null)
                Destroy(go);
        }
        persistentObjects.Clear();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GAME STATE
    // ══════════════════════════════════════════════════════════════════════════

    // Called by PlayerSpinController when the player is hit by an obstacle.
    // Removes one heart; if none left, triggers game over.
    public void RegisterPlayerHit()
    {
        if (IsGameOver) return;

        PlayerHitCount++;

        if (PlayerHitCount >= maxHitCount)
            TriggerGameOver();
    }

    // Resets state between sessions.
    //   fullReset = true  → also wipes hearts (used when restarting from GameOverScene)
    //   fullReset = false → keeps hearts intact (used when moving between levels in one session)
    public void ResetStats(bool fullReset = true)
    {
        ResumeGame();
        ElapsedTime   = 0f;
        IsGameOver    = false;
        IsGameStarted = false;

        if (fullReset)
        {
            PlayerHitCount  = 0;  // restore all hearts
            SpeedMultiplier = speedMultiplierStart;
        }
    }

    // Starts gameplay in the current scene and fires the GameStarted event.
    // Listeners: GameSettlementUI (spawns hearts, shows HUD), QuadrantSpawner (begins coroutine).
    //   resetStats = true  → full reset first (legacy path, still used if needed)
    //   resetStats = false → carry forward current hit count (used by GameSettlementUI in level scenes)
    public void StartGame(bool resetStats = true)
    {
        Debug.Log($"[GameManager] StartGame called — resetStats={resetStats}");

        if (resetStats)
            ResetStats(fullReset: true);
        else
        {
            ResumeGame();
            IsGameOver = false;
        }

        IsGameStarted = true;
        Debug.Log("[GameManager] IsGameStarted = true, firing GameStarted event");
        GameStarted?.Invoke();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PAUSE / RESUME
    // ══════════════════════════════════════════════════════════════════════════

    public void PauseGame()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        // Guard against redundant calls
        if (!IsPaused && Math.Abs(Time.timeScale - 1f) < 0.0001f) return;
        IsPaused = false;
        Time.timeScale = 1f;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SCENE TRANSITIONS
    // All scene-loading lives here. Other scripts call these methods;
    // they never call SceneManager.LoadScene directly.
    // ══════════════════════════════════════════════════════════════════════════

    // Called by CameraIntroDrop when the camera finishes dropping in StartScene.
    // Wipes all stats (full reset — new game) and goes to the countdown.
    public void OnIntroComplete()
    {
        ResetStats(fullReset: true);
        SceneManager.LoadScene(SceneIndex.Countdown);
    }

    // Called by QuadrantSpawner when all phases of a level are complete.
    // Hearts are NOT reset — the player keeps whatever they had.
    public void OnLevelComplete()
    {
        IsGameStarted   = false;
        SpeedMultiplier += speedMultiplierPerLevel;
        ResumeGame();
        SceneManager.LoadScene(SceneIndex.Countdown);
    }

    // Called by CountdownController after the "GO!" beat.
    // Picks a random level from the configured list and loads it.
    public void LoadRandomLevel()
    {
        if (levelSceneIndices == null || levelSceneIndices.Length == 0)
        {
            Debug.LogError("[GameManager] levelSceneIndices is empty — add level scene build indices in the Inspector.");
            return;
        }

        int idx = levelSceneIndices[UnityEngine.Random.Range(0, levelSceneIndices.Length)];
        Debug.Log($"[GameManager] Loading level at build index {idx}");
        SceneManager.LoadScene(idx);
    }

    // ── Game over ─────────────────────────────────────────────────────────────

    // Internal — called by RegisterPlayerHit when hearts run out.
    private void TriggerGameOver()
    {
        IsGameOver    = true;
        IsGameStarted = false;
        PauseGame();  // freeze the scene so nothing moves while we transition

        // Save high score if beaten
        int scoreToCheck = Mathf.RoundToInt(ElapsedTime);
        if (scoreToCheck > currentHighScore)
        {
            currentHighScore = scoreToCheck;
            PlayerPrefs.SetInt("HighScore", currentHighScore);
            PlayerPrefs.Save();
        }

        // Fire event so any listeners (audio, VFX) can react before the scene changes
        GameOverTriggered?.Invoke(new GameResult(ElapsedTime, PlayerHitCount, RemainingHitCount, currentHighScore));

        Debug.Log($"[GameManager] Game Over — score={ElapsedTime:F1}s  highScore={currentHighScore}");

        // Short delay so the hit reaction (spin, camera shake) finishes before loading
        StartCoroutine(LoadGameOverDelayed(0.1f));
    }

    // Waits a tiny moment (real time, ignores timeScale pause), then loads GameOverScene.
    private IEnumerator LoadGameOverDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Time.timeScale = 1f;  // un-pause before loading or the new scene inherits timeScale=0
        SceneManager.LoadScene(SceneIndex.GameOver);
    }
}
