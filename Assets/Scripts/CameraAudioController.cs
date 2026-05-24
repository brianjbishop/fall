using System.Collections;
using UnityEngine;

public class CameraAudioController : MonoBehaviour
{
    [Header("NewArcade Source")]
    [SerializeField] private AudioSource newArcadeSource;
    [SerializeField] private bool autoBindFromThisObject = true;
    [SerializeField] private int newArcadeSourceIndex = 0;
    [SerializeField] private bool useUnscaledTimeForTiming = true;

    [Header("Start Timing")]
    [SerializeField] private float delayAfterGameStarted = 0.2f;

    [Header("NewArcade Fade In")]
    [SerializeField] private float newArcadeStartVolume = 0.35f;
    [SerializeField] private float newArcadeTargetVolume = 1f;
    [SerializeField] private float newArcadeFadeDuration = 0.8f;
    [SerializeField] private bool enableAudioDebugLogs = true;

    private Coroutine playRoutine;
    private Coroutine fadeCoroutine;
    private GameManager cachedGameManager;
    private bool hasSubscribed;

    private void Start()
    {
        AutoBindAudioSourcesIfNeeded();
        PrepareNewArcadeSource();
        TryBindGameManager();

        if (cachedGameManager != null && cachedGameManager.IsGameStarted)
        {
            OnGameStarted();
        }
    }

    private void Update()
    {
        if (!hasSubscribed)
        {
            TryBindGameManager();
        }
    }

    private void OnDisable()
    {
        UnsubscribeGameManager();

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    private void OnGameOverTriggered(GameManager.GameResult result)
    {
        StopNewArcadeAudio();
    }

    private void PrepareNewArcadeSource()
    {
        if (newArcadeSource == null)
        {
            return;
        }

        newArcadeSource.playOnAwake = false;
        if (newArcadeSource.isPlaying)
        {
            newArcadeSource.Stop();
        }
    }

    private void AutoBindAudioSourcesIfNeeded()
    {
        if (!autoBindFromThisObject)
        {
            return;
        }

        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources == null || sources.Length == 0)
        {
            return;
        }

        if (newArcadeSource == null)
        {
            newArcadeSource = GetSourceByIndex(sources, newArcadeSourceIndex);
        }

        if (newArcadeSource == null && sources.Length > 0)
        {
            newArcadeSource = sources[0];
        }
    }

    private AudioSource GetSourceByIndex(AudioSource[] sources, int index)
    {
        if (index < 0 || index >= sources.Length)
        {
            return null;
        }

        return sources[index];
    }

    private void TryBindGameManager()
    {
        if (hasSubscribed)
        {
            return;
        }

        cachedGameManager = GameManager.Instance;
        if (cachedGameManager == null)
        {
            return;
        }

        cachedGameManager.GameStarted += OnGameStarted;
        cachedGameManager.GameOverTriggered += OnGameOverTriggered;
        hasSubscribed = true;
    }

    private void UnsubscribeGameManager()
    {
        if (!hasSubscribed || cachedGameManager == null)
        {
            return;
        }

        cachedGameManager.GameStarted -= OnGameStarted;
        cachedGameManager.GameOverTriggered -= OnGameOverTriggered;
        hasSubscribed = false;
    }

    // Called by CameraIntroDrop the moment the player presses a key in StartScene.
    // This is the only place music should start in StartScene — not from OnGameStarted.
    public void PlayMusic()
    {
        if (newArcadeSource != null && newArcadeSource.isPlaying) return;
        if (playRoutine != null) { StopCoroutine(playRoutine); playRoutine = null; }
        playRoutine = StartCoroutine(PlayAfterDelay());
    }

    private void OnGameStarted()
    {
        // In StartScene, music is triggered by CameraIntroDrop (player key press),
        // not by GameStarted. Skipping here prevents music from auto-starting the
        // moment StartScene loads after a GameOver restart.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == SceneIndex.Start)
            return;

        // If music is already playing (e.g. carried over from a previous level),
        // don't restart it — just let it keep running.
        if (newArcadeSource != null && newArcadeSource.isPlaying)
        {
            if (enableAudioDebugLogs)
                Debug.Log("[CameraAudioController] GameStarted received but music already playing — skipping restart.");
            return;
        }

        if (enableAudioDebugLogs)
            Debug.Log("[CameraAudioController] GameStarted received, will play NewArcade after delay=" + delayAfterGameStarted);

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        playRoutine = StartCoroutine(PlayAfterDelay());
    }

    private IEnumerator PlayAfterDelay()
    {
        float delay = Mathf.Max(0f, delayAfterGameStarted);
        if (delay > 0f)
        {
            if (useUnscaledTimeForTiming)
            {
                yield return new WaitForSecondsRealtime(delay);
            }
            else
            {
                yield return new WaitForSeconds(delay);
            }
        }

        StartNewArcadeWithFadeIn();
        playRoutine = null;
    }

    private void StartNewArcadeWithFadeIn()
    {
        if (newArcadeSource == null)
        {
            if (enableAudioDebugLogs)
            {
                Debug.LogWarning("[CameraAudioController] NewArcadeSource is null, cannot play background music.");
            }
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        fadeCoroutine = StartCoroutine(FadeInNewArcade());
    }

    private void StopNewArcadeAudio()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (newArcadeSource == null)
        {
            return;
        }

        if (enableAudioDebugLogs)
        {
            Debug.Log("[CameraAudioController] Stopping NewArcade on game settlement.");
        }

        newArcadeSource.Stop();
    }

    private IEnumerator FadeInNewArcade()
    {
        float clampedTarget = Mathf.Clamp01(newArcadeTargetVolume);
        float clampedStart = Mathf.Clamp01(newArcadeStartVolume);
        float duration = Mathf.Max(0f, newArcadeFadeDuration);

        newArcadeSource.volume = clampedStart;

        if (!newArcadeSource.isPlaying)
        {
            if (enableAudioDebugLogs)
            {
                Debug.Log("[CameraAudioController] Playing NewArcade with fade in on " + gameObject.name);
            }
            newArcadeSource.Play();
        }

        if (duration <= 0f)
        {
            newArcadeSource.volume = clampedTarget;
            fadeCoroutine = null;
            yield break;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += useUnscaledTimeForTiming ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            newArcadeSource.volume = Mathf.Lerp(clampedStart, clampedTarget, t);
            yield return null;
        }

        newArcadeSource.volume = clampedTarget;
        fadeCoroutine = null;
    }
}
