// Attach this to any GameObject you want to survive scene loads.
//
// When this object wakes up it checks whether an older copy with the same
// name is already alive in the DontDestroyOnLoad scene (happens when
// StartScene reloads after a GameOver restart). If so, it destroys the
// OLD copy and replaces it with the fresh one from the new scene load.
// This guarantees there is always exactly one copy and it is always fresh.
using UnityEngine;

public class PersistObject : MonoBehaviour
{
    private void Awake()
    {
        // Find any existing PersistObject with the same GameObject name.
        // The old copy is the one already in DontDestroyOnLoad; destroy it
        // so the freshly loaded scene object takes over cleanly.
        foreach (var existing in FindObjectsByType<PersistObject>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (existing != this && existing.gameObject.name == gameObject.name)
            {
                Destroy(existing.gameObject);
                break;
            }
        }

        DontDestroyOnLoad(gameObject);
    }
}