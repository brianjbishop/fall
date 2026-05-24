using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Tooltip("Default shake duration (seconds)")]
    public float defaultDuration = 0.5f;
    [Tooltip("Default shake magnitude (world unit offset)")]
    public float defaultMagnitude = 0.5f;
    [Tooltip("Default shake rotation magnitude (degrees)")]
    public float defaultRotationMagnitude = 3f;

    private Coroutine running;

    public void Shake()
    {
        Shake(defaultDuration, defaultMagnitude);
    }

    public void Shake(float duration, float magnitude)
    {
        if (running != null)
        {
            StopCoroutine(running);
        }
        Debug.Log("CameraShake: Shake called (duration=" + duration + ", magnitude=" + magnitude + ")");
        running = StartCoroutine(DoShake(duration, magnitude, defaultRotationMagnitude));
    }

    public void Shake(float duration, float magnitude, float rotationMagnitude)
    {
        if (running != null)
        {
            StopCoroutine(running);
        }
        Debug.Log("CameraShake: Shake called (duration=" + duration + ", magnitude=" + magnitude + ", rot=" + rotationMagnitude + ")");
        running = StartCoroutine(DoShake(duration, magnitude, rotationMagnitude));
    }

    [ContextMenu("Test Shake")]
    private void TestShake()
    {
        Shake();
    }

    private IEnumerator DoShake(float duration, float magnitude, float rotationMagnitude)
    {
        Vector3 originalPos = transform.localPosition;
        Vector3 originalEuler = transform.localEulerAngles;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float damper = 1f - Mathf.Clamp01(elapsed / duration);
            Vector3 offset = Random.insideUnitSphere * magnitude * damper;
            Vector3 rotOffset = Random.insideUnitSphere * rotationMagnitude * damper;
            transform.localPosition = originalPos + offset;
            transform.localEulerAngles = originalEuler + rotOffset;
            elapsed += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        transform.localPosition = originalPos;
        transform.localEulerAngles = originalEuler;
        running = null;
    }
}
