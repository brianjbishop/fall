using System.Collections;
using UnityEngine;

public class PlayerSpinController : MonoBehaviour
{
    [Header("Collision Detection")]
    [Tooltip("Minimum collision impact threshold; above this value counts as a hit")]
    public float collisionThreshold = 1f;

    [Header("Hit Audio")]
    [SerializeField] private AudioSource hitAudioSource;
    [SerializeField] private bool enableHitAudioDebugLog = true;

    [Header("Spin / Restore Timing")]
    [Tooltip("Duration to stay in spin state after collision (seconds), then begin restoring")]
    public float spinStateDuration = 2f;
    [Tooltip("Duration to smoothly restore rotation from current facing to (0,0,0) (seconds)")]
    public float restoreDuration = 1.5f;

    [Header("Animator Integration (optional)")]
    public Animator animator;
    public string animatorSpinParam = "isSpinning";
    public string animatorRestoringParam = "isRestoring";

    [Header("Parachute Tilt")]
    [Tooltip("正常移动时额外的 Z 轴旋转角度")]
    public float parachuteTiltAngle = 8f;
    [Tooltip("倾斜角度变化速度")]
    public float parachuteTiltLerpSpeed = 8f;

    private Rigidbody rb;
    private PlayerFaceOrigin faceOrigin;
    private float currentTiltAngle;

    private enum State { Idle, Spinning, Restoring }
    private State state = State.Idle;
    private float stateTimer = 0f;

    public bool IsIdle => state == State.Idle;

    // values recorded when starting restore
    private Quaternion startRotation;
    private Vector3 startAngularVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerSpinController requires a Rigidbody on the same GameObject.");
        }

        faceOrigin = GetComponent<PlayerFaceOrigin>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact < collisionThreshold) return;

        if (hitAudioSource != null)
        {
            if (enableHitAudioDebugLog)
            {
                Debug.Log("[PlayerSpinController] Playing hit audio on " + gameObject.name + ", impact=" + impact);
            }
            hitAudioSource.Play();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayerHit();
            
            // Check if the game is over; skip camera shake if so
            if (GameManager.Instance.IsGameOver)
            {
                return;
            }
        }

        // Attempt to trigger camera shake (if main camera has a CameraShake component)
        var cam = Camera.main;
        if (cam != null)
        {
            var shake = cam.GetComponent<CameraShake>();
            if (shake != null)
            {
                float shakeMag = Mathf.Clamp(impact * 0.1f, 0.1f, 1.5f);
                float shakeDur = 0.25f;
                shake.Shake(shakeDur, shakeMag);
            }
        }

        EnterSpinningState();
    }

    private void EnterSpinningState()
    {
        state = State.Spinning;
        stateTimer = 0f;
        currentTiltAngle = 0f;
        if (faceOrigin != null)
        {
            faceOrigin.SetFacingEnabled(false);
            faceOrigin.SetExtraPitchAngle(0f);
        }
        if (animator != null)
        {
            animator.SetBool(animatorSpinParam, true);
            animator.SetBool(animatorRestoringParam, false);
        }
    }

    private void StartRestore()
    {
        state = State.Restoring;
        stateTimer = 0f;
        currentTiltAngle = 0f;
        startRotation = transform.rotation;
        startAngularVelocity = rb != null ? rb.angularVelocity : Vector3.zero;

        if (animator != null)
        {
            animator.SetBool(animatorSpinParam, false);
            animator.SetBool(animatorRestoringParam, true);
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        UpdateParachuteTilt(Time.fixedDeltaTime);

        if (state == State.Spinning)
        {
            stateTimer += Time.fixedDeltaTime;
            if (stateTimer >= spinStateDuration)
            {
                StartRestore();
            }
        }
        else if (state == State.Restoring)
        {
            stateTimer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(stateTimer / Mathf.Max(0.0001f, restoreDuration));

            // Smoothly reduce angular velocity
            rb.angularVelocity = Vector3.Lerp(startAngularVelocity, Vector3.zero, t);

            // Smoothly interpolate rotation towards world (0,0,0)
            Quaternion target = Quaternion.identity;
            Quaternion newRot = Quaternion.Slerp(startRotation, target, t);
            rb.MoveRotation(newRot);

            if (t >= 1f - 1e-6f)
            {
                rb.angularVelocity = Vector3.zero;
                rb.MoveRotation(target);
                state = State.Idle;
                if (faceOrigin != null)
                {
                    faceOrigin.SetFacingEnabled(true);
                }
                if (animator != null)
                {
                    animator.SetBool(animatorRestoringParam, false);
                }
            }
        }
    }

    private void UpdateParachuteTilt(float deltaTime)
    {
        if (faceOrigin == null || !faceOrigin.IsFacingEnabled || state != State.Idle)
        {
            currentTiltAngle = Mathf.Lerp(currentTiltAngle, 0f, deltaTime * parachuteTiltLerpSpeed);
            faceOrigin?.SetExtraPitchAngle(currentTiltAngle);
            return;
        }

        float targetTilt = 0f;
        if (faceOrigin.OrbitDirection > 0)
        {
            targetTilt = parachuteTiltAngle;
        }
        else if (faceOrigin.OrbitDirection < 0)
        {
            targetTilt = -parachuteTiltAngle;
        }

        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetTilt, deltaTime * parachuteTiltLerpSpeed);
        faceOrigin.SetExtraPitchAngle(currentTiltAngle);
    }
}
