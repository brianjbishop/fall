using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class AudioSourceScenePlaylistRandomizer : MonoBehaviour
{
    [Header("Target Audio Source")]
    [SerializeField] private AudioSource targetAudioSource;
    [SerializeField] private bool autoBindFromThisObject = true;
    [SerializeField] private int autoBindSourceIndex = 0;

    [Header("Scene Load Playlist")]
    [SerializeField] private AudioClip[] playlist;
    [SerializeField] private bool enableDebugLogs = false;

    private void Awake()
    {
        BindAudioSourceIfNeeded();
        ApplyRandomClipOnSceneLoad();
    }

    private void BindAudioSourceIfNeeded()
    {
        if (targetAudioSource != null)
        {
            return;
        }

        if (!autoBindFromThisObject)
        {
            return;
        }

        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources == null || sources.Length == 0)
        {
            return;
        }

        int index = Mathf.Clamp(autoBindSourceIndex, 0, sources.Length - 1);
        targetAudioSource = sources[index];

        if (enableDebugLogs)
        {
            Debug.Log("[AudioSourceScenePlaylistRandomizer] Auto-bound AudioSource index=" + index + " on " + gameObject.name);
        }
    }

    private void ApplyRandomClipOnSceneLoad()
    {
        if (targetAudioSource == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[AudioSourceScenePlaylistRandomizer] Target AudioSource is null.");
            }
            return;
        }

        if (playlist == null || playlist.Length == 0)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[AudioSourceScenePlaylistRandomizer] Playlist is empty.");
            }
            return;
        }

        int[] validIndices = new int[playlist.Length];
        int count = 0;

        for (int i = 0; i < playlist.Length; i++)
        {
            if (playlist[i] != null)
            {
                validIndices[count] = i;
                count++;
            }
        }

        if (count == 0)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[AudioSourceScenePlaylistRandomizer] Playlist has no valid clips.");
            }
            return;
        }

        int selectedIndex = validIndices[Random.Range(0, count)];
        AudioClip selectedClip = playlist[selectedIndex];
        targetAudioSource.clip = selectedClip;

        if (enableDebugLogs)
        {
            Debug.Log("[AudioSourceScenePlaylistRandomizer] Selected clip: " + selectedClip.name);
        }
    }
}
