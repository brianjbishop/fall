using System.Collections;
using UnityEngine;

public class CameraIntroDrop : MonoBehaviour
{
    [Header("Drop")]
    [SerializeField] private Vector3 startPosition = new Vector3(0f, 330f, 0f);
    [SerializeField] private Vector3 endPosition = new Vector3(0f, 4f, 0f);
    [SerializeField] private float dropDuration = 2f;
    [SerializeField] private float dropDurationMultiplier = 1.5f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Game Start")]
    [SerializeField] private bool resetGameManagerStatsOnLanding = true;
    [SerializeField] private Player playerInputReference;

    private Coroutine dropRoutine;
    private bool hasStarted;

    private void Awake()
    {
        endPosition = new Vector3(0f, 4f, 0f);
        transform.position = startPosition;
    }

    private void Start()
    {
        hasStarted = false;

        if (playerInputReference == null)
        {
            playerInputReference = FindFirstObjectByType<Player>();
        }

        Debug.Log("[CameraIntroDrop] Start: Waiting for player input to start game...");
    }

    private void Update()
    {
        // Drop not yet started; waiting for player input
        if (!hasStarted)
        {
            if (IsStartInputPressed())
            {
                // Hide start prompts the instant the key is pressed
                GameSettlementUI ui = FindFirstObjectByType<GameSettlementUI>();
                if (ui != null) ui.HideStartPrompts();

                // Start music exactly when the player presses a key — not before
                CameraAudioController audio = FindFirstObjectByType<CameraAudioController>();
                if (audio != null) audio.PlayMusic();

                dropRoutine = StartCoroutine(DropAndStartGame());
                hasStarted = true;
            }
        }
    }


    private IEnumerator DropAndStartGame()
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.0001f, dropDuration * Mathf.Max(0.01f, dropDurationMultiplier));

        while (elapsed < safeDuration)
        {
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float easedT = easing != null ? easing.Evaluate(t) : t;
            transform.position = Vector3.LerpUnclamped(startPosition, endPosition, easedT);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.position = endPosition;

        // Read the delay from IntroDuration on this same GameObject.
        // If the component isn't there, default to 0 (instant transition).
        IntroDuration introDuration = GetComponent<IntroDuration>();
        float postDropDelay = introDuration != null ? introDuration.postDropDelay : 0f;

        if (postDropDelay > 0f)
            yield return new WaitForSecondsRealtime(postDropDelay);

        if (GameManager.Instance != null)
        {
            // OnIntroComplete resets all stats (full reset — new session) and
            // loads CountdownScene. It replaces the old StartGame() call so the
            // game no longer tries to start inside the StartScene itself.
            GameManager.Instance.OnIntroComplete();
        }

        dropRoutine = null;
    }

    private bool IsStartInputPressed()
    {
        if (playerInputReference != null)
        {
            return playerInputReference.IsAnyMovementKeyDown();
        }

        return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
               Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) ||
               Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow) ||
               Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow);
    }
}