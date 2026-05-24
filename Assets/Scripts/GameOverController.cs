// GameOverController.cs
// ─────────────────────────────────────────────────────────────────────────────
// Drives the GameOverScene: displays the final score and high score, waits for
// any movement key, then tears down all persistent objects in the correct order
// before returning to StartScene for a clean restart.
//
// DESTRUCTION ORDER (on key press)
//   1. Hide all text on this screen immediately
//   2. Destroy CloudSpawner (takes all child cloud objects with it)
//   3. Destroy the persistent Player
//   4. Destroy the persistent HUD canvas (GameSettlementUI)
//   5. Reset GameManager state (hearts, time, etc.)
//   6. Load StartScene — this unloads GameOverScene and destroys this script last
//
// SCENE SETUP
//   • Wire scoreText / scoreTextB, highScoreText / highScoreTextB,
//     restartPromptText / restartPromptTextB in the Inspector.
//   • The B slots are rotated 180° copies for the opposite side of the cabinet.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Score Display")]
    [Tooltip("Primary score text.")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("Secondary score text — rotated 180° for the opposite side of the cabinet.")]
    [SerializeField] private TMP_Text scoreTextB;

    [Header("High Score Display")]
    [Tooltip("Primary high score text.")]
    [SerializeField] private TMP_Text highScoreText;
    [Tooltip("Secondary high score text — rotated 180° for the opposite side.")]
    [SerializeField] private TMP_Text highScoreTextB;

    [Header("Restart Prompt")]
    [Tooltip("Primary restart prompt text.")]
    [SerializeField] private TMP_Text restartPromptText;
    [Tooltip("Secondary restart prompt text — rotated 180° for the opposite side.")]
    [SerializeField] private TMP_Text restartPromptTextB;

    [Tooltip("Seconds before the restart prompt appears and input is accepted.")]
    [SerializeField] private float inputDelayTime = 1.5f;

    // ── Private state ──────────────────────────────────────────────────────────
    private float arrivalTime;
    private bool  restarting = false;   // prevents double-trigger if two keys hit same frame

    // ══════════════════════════════════════════════════════════════════════════
    // STARTUP — display scores, hide prompt until delay elapses
    // ══════════════════════════════════════════════════════════════════════════

    private void Start()
    {
        Time.timeScale = 1f;
        arrivalTime    = Time.realtimeSinceStartup;

        // Prompt hidden until delay passes
        SetActive(restartPromptText,  false);
        SetActive(restartPromptTextB, false);

        StartCoroutine(ShowPromptAfterDelay());

        // Populate score fields from GameManager (persists from the level that just ended)
        if (GameManager.Instance != null)
        {
            string scoreStr = $"Score: {GameManager.Instance.ElapsedTime:F1} s";
            string bestStr  = $"Best:  {GameManager.Instance.HighScore} s";

            SetText(scoreText,      scoreStr);
            SetText(scoreTextB,     scoreStr);
            SetText(highScoreText,  bestStr);
            SetText(highScoreTextB, bestStr);
        }
        else
        {
            SetText(scoreText,      "Score: —");
            SetText(scoreTextB,     "Score: —");
            SetText(highScoreText,  "Best:  —");
            SetText(highScoreTextB, "Best:  —");
            Debug.LogWarning("[GameOverController] GameManager.Instance is null — score data unavailable.");
        }
    }

    private IEnumerator ShowPromptAfterDelay()
    {
        yield return new WaitForSecondsRealtime(inputDelayTime);
        SetActive(restartPromptText,  true);
        SetActive(restartPromptTextB, true);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // INPUT
    // ══════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        if (restarting) return;
        if (Time.realtimeSinceStartup - arrivalTime < inputDelayTime) return;

        // anyKeyDown catches every keyboard key and every controller button,
        // regardless of how the physical controllers are mapped.
        if (Input.anyKeyDown)
            StartCoroutine(Restart());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // RESTART — ordered teardown then scene load
    // ══════════════════════════════════════════════════════════════════════════

    private IEnumerator Restart()
    {
        restarting = true;

        // 1. Hide all text on this screen immediately so nothing lingers visually
        SetActive(scoreText,          false);
        SetActive(scoreTextB,         false);
        SetActive(highScoreText,      false);
        SetActive(highScoreTextB,     false);
        SetActive(restartPromptText,  false);
        SetActive(restartPromptTextB, false);

        yield return null;  // let the hide take effect before destroying anything

        // 2. Destroy every persistent object (player, canvas, clouds, etc.)
        //    except GameManager itself — one call handles everything cleanly.
        GameManager.Instance?.DestroyAllPersistent();

        // 3. Reset GameManager (hearts back to full, time to 0, game state cleared)
        GameManager.Instance?.ResetStats(fullReset: true);

        // 4. Wait one frame so all Destroy calls process before the scene loads
        yield return null;

        // 5. Load StartScene — unloads GameOverScene and destroys this script last
        SceneManager.LoadScene(SceneIndex.Start);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private static void SetText(TMP_Text target, string content)
    {
        if (target != null) target.text = content;
    }

    private static void SetActive(TMP_Text target, bool visible)
    {
        if (target != null) target.gameObject.SetActive(visible);
    }
}
