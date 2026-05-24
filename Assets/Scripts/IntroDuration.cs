// IntroDuration.cs
// ─────────────────────────────────────────────────────────────────────────────
// Controls how long the start scene lingers after the camera finishes dropping
// before transitioning to CountdownScene.
//
// Attach this to the same GameObject as CameraIntroDrop (the camera).
// CameraIntroDrop reads the delay value from this component automatically.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

public class IntroDuration : MonoBehaviour
{
    [Tooltip("Seconds to stay on the start scene after the camera lands. " +
             "Use this time for instructions, atmosphere, or just vibe. Set to 0 for instant.")]
    public float postDropDelay = 3f;
}
