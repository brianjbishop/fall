// CountdownController.cs
// ─────────────────────────────────────────────────────────────────────────────
// Drives the CountdownScene: displays "3 → 2 → 1 → GO!" then tells GameManager
// to load a random level scene.
//
// SCENE SETUP (CountdownScene)
//   1. Create an empty GameObject, attach this script.
//   2. Create a Canvas → add a TextMeshPro - Text (UI) element in the center.
//   3. Drag that TMP text into the "Countdown Text" slot in the Inspector.
//   4. Tune digitHoldTime / goHoldTime to feel right.
//   5. You can customise the labels array if you want different text (e.g. "GO!" → "RUN!").
//
// NOTE: This scene does NOT call GameManager.StartGame().
//       That happens inside GameSettlementUI.Start() once the level scene loads.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownController : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("UI")]
    [Tooltip("The TMP text element that displays each countdown label.")]
    [SerializeField] private TMP_Text countdownText;

    [Header("Timing")]
    [Tooltip("How long each number (3, 2, 1) stays on screen in seconds.")]
    [SerializeField] private float digitHoldTime = 0.85f;

    [Tooltip("How long the final label (GO!) stays on screen before the level loads.")]
    [SerializeField] private float goHoldTime = 0.6f;

    [Header("Labels")]
    [Tooltip("The sequence of strings shown on screen. The last entry uses goHoldTime; " +
             "all others use digitHoldTime.")]
    [SerializeField] private string[] labels = { "3", "2", "1", "GO!" };

    // ══════════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Start()
    {
        // The game-over scene pauses Time.timeScale to freeze the level.
        // Make sure it's running normally before counting down.
        Time.timeScale = 1f;

        StartCoroutine(RunCountdown());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // COUNTDOWN COROUTINE
    // ══════════════════════════════════════════════════════════════════════════

    private IEnumerator RunCountdown()
    {
        // Step through each label in the array.
        // "yield return new WaitForSeconds(N)" pauses here for N seconds,
        // lets Unity render the current label, then resumes.
        for (int i = 0; i < labels.Length; i++)
        {
            // Show this label
            if (countdownText != null)
                countdownText.text = labels[i];

            // Last label gets the shorter "GO!" hold time; others get the digit hold time
            bool isLastLabel = (i == labels.Length - 1);
            yield return new WaitForSeconds(isLastLabel ? goHoldTime : digitHoldTime);
        }

        // All labels shown — ask GameManager to load a random level.
        // GameManager picks randomly from its levelSceneIndices array.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadRandomLevel();
        }
        else
        {
            // Safety fallback: if GameManager somehow isn't alive, load index 2 directly.
            Debug.LogWarning("[CountdownController] GameManager.Instance is null — loading scene 2 as fallback.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(2);
        }
    }
}
