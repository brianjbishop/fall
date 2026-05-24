using UnityEngine;

public class PlayerFaceOrigin : MonoBehaviour
{
    [Tooltip("Rotation smooth speed (higher is faster); 0 means instant snap")]
    public float rotateSpeed = 10f;

    [Tooltip("Whether to only rotate on the Y axis (stay upright)")]
    public bool onlyYAxis = true;

    [Tooltip("Whether to apply in FixedUpdate (recommended when player uses Rigidbody)")]
    public bool useFixedUpdate = true;

    [Tooltip("Target point to face; defaults to world origin (0,0,0)")]
    public Vector3 targetPoint = Vector3.zero;

    [Tooltip("Extra local Z-axis rotation angle for visual feedback")]
    public float extraPitchAngle = 0f;

    private Rigidbody rb;
    private bool isFacingEnabled = true;
    private Vector3 previousPlanarDirection;
    private bool hasPreviousPlanarDirection;
    private int orbitDirection;

    public void SetFacingEnabled(bool enabled)
    {
        isFacingEnabled = enabled;
    }

    public bool IsFacingEnabled => isFacingEnabled;

    public int OrbitDirection => orbitDirection;

    public void SetExtraPitchAngle(float angle)
    {
        extraPitchAngle = angle;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.Log("PlayerFaceOrigin: No Rigidbody found; will use Transform rotation instead.");
        }

        Vector3 initialDirection = targetPoint - transform.position;
        if (onlyYAxis)
        {
            initialDirection.y = 0f;
        }

        if (initialDirection.sqrMagnitude > 1e-6f)
        {
            previousPlanarDirection = initialDirection.normalized;
            hasPreviousPlanarDirection = true;
        }
    }

    void FixedUpdate()
    {
        if (useFixedUpdate)
            ApplyFacing(Time.fixedDeltaTime);
    }

    void Update()
    {
        if (!useFixedUpdate)
            ApplyFacing(Time.deltaTime);
    }

    private void ApplyFacing(float deltaTime)
    {
        if (!isFacingEnabled)
            return;

        Vector3 dir = targetPoint - transform.position;
        if (onlyYAxis)
        {
            dir.y = 0f; // project to XZ plane
        }

        if (dir.sqrMagnitude < 1e-6f)
            return;

        Vector3 currentPlanarDirection = dir.normalized;
        UpdateOrbitDirection(currentPlanarDirection, deltaTime);

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        if (rotateSpeed <= 0f)
        {
            if (rb != null)
                rb.MoveRotation(targetRot * Quaternion.Euler(0f, 0f, extraPitchAngle));
            else
                transform.rotation = targetRot * Quaternion.Euler(0f, 0f, extraPitchAngle);
            return;
        }

        float t = Mathf.Clamp01(rotateSpeed * deltaTime);
        Quaternion newRot = Quaternion.Slerp(transform.rotation, targetRot, t);
        newRot *= Quaternion.Euler(0f, 0f, extraPitchAngle);

        if (rb != null)
            rb.MoveRotation(newRot);
        else
            transform.rotation = newRot;
    }

    private void UpdateOrbitDirection(Vector3 currentPlanarDirection, float deltaTime)
    {
        if (!hasPreviousPlanarDirection || deltaTime <= 0f)
        {
            previousPlanarDirection = currentPlanarDirection;
            hasPreviousPlanarDirection = true;
            orbitDirection = 0;
            return;
        }

        float signedAngle = Vector3.SignedAngle(previousPlanarDirection, currentPlanarDirection, Vector3.up);
        if (signedAngle > 0.1f)
        {
            orbitDirection = 1;
        }
        else if (signedAngle < -0.1f)
        {
            orbitDirection = -1;
        }
        else
        {
            orbitDirection = 0;
        }

        previousPlanarDirection = currentPlanarDirection;
    }
}
