using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveForce = 2f;
    [SerializeField] private float yellowKeyForceCorrection = 1f;  // forward
    [SerializeField] private float greenKeyForceCorrection  = 1f;  // backward
    [SerializeField] private float redKeyForceCorrection    = 1f;  // left
    [SerializeField] private float blueKeyForceCorrection   = 1f;  // right
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;
    [SerializeField] private float minZ = -10f;
    [SerializeField] private float maxZ = 10f;
    [SerializeField] private float inputBoundaryBuffer = 0.05f;
    [SerializeField] private float boundaryDamping = 25f;
    [SerializeField] private float boundaryReturnAcceleration = 40f;

    // Each direction accepts any number of keys — add as many as you need.
    // The alternate keys section is removed; just expand these arrays instead.
    [Header("Yellow — Forward")]
    [SerializeField] private KeyCode[] yellowKeys = { KeyCode.W, KeyCode.UpArrow };

    [Header("Green — Backward")]
    [SerializeField] private KeyCode[] greenKeys = { KeyCode.S, KeyCode.DownArrow };

    [Header("Red — Left")]
    [SerializeField] private KeyCode[] redKeys = { KeyCode.A, KeyCode.LeftArrow };

    [Header("Blue — Right")]
    [SerializeField] private KeyCode[] blueKeys = { KeyCode.D, KeyCode.RightArrow };

    [Header("Key Press Counter")]
    [SerializeField] private int keyPressThreshold = 16;
    [SerializeField] private int yellowKeyPressCount = 0;  // forward press accumulator
    [SerializeField] private int greenKeyPressCount  = 0;  // backward press accumulator

    // ── Public helpers used by other scripts (e.g. GameSettlementUI, GameOverController) ──

    public bool IsYellowKeyDown() => IsAnyKeyDown(yellowKeys);
    public bool IsGreenKeyDown()  => IsAnyKeyDown(greenKeys);
    public bool IsRedKeyDown()    => IsAnyKeyDown(redKeys);
    public bool IsBlueKeyDown()   => IsAnyKeyDown(blueKeys);

    // Legacy names kept so existing scripts that call these don't break
    public bool IsForwardKeyDown()  => IsYellowKeyDown();
    public bool IsBackwardKeyDown() => IsGreenKeyDown();
    public bool IsLeftKeyDown()     => IsRedKeyDown();
    public bool IsRightKeyDown()    => IsBlueKeyDown();

    public bool IsAnyMovementKeyDown() =>
        IsYellowKeyDown() || IsGreenKeyDown() || IsRedKeyDown() || IsBlueKeyDown();

    public event System.Action<Vector3> OnForceApplied;

    private Rigidbody playerRigidbody;

    // Awake runs before any Start() in the scene.
    // Registering here ensures GameManager.PlayerRef is set before other
    // scripts (e.g. GameSettlementUI) call StartGame() in their Start().
    private void Awake()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterPlayer(this);
    }

    // Called when this scene unloads — clears the stale reference from GameManager.
    private void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.PlayerRef == this)
            GameManager.Instance.RegisterPlayer(null);
    }

    void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();

        if (playerRigidbody == null)
            Debug.LogError("Player must be attached to a GameObject with a Rigidbody.");
    }

    void Update()
    {
        if (playerRigidbody == null) return;

        // Yellow — forward (requires threshold presses before applying force)
        if (IsYellowKeyDown())
        {
            yellowKeyPressCount++;
            if (yellowKeyPressCount >= keyPressThreshold && CanMoveTo(Vector3.forward))
            {
                playerRigidbody.AddForce(Vector3.forward * moveForce * yellowKeyForceCorrection, ForceMode.Impulse);
                OnForceApplied?.Invoke(Vector3.forward);
                yellowKeyPressCount = 0;
            }
        }

        // Green — backward (requires threshold presses before applying force)
        if (IsGreenKeyDown())
        {
            greenKeyPressCount++;
            if (greenKeyPressCount >= keyPressThreshold && CanMoveTo(Vector3.back))
            {
                playerRigidbody.AddForce(Vector3.back * moveForce * greenKeyForceCorrection, ForceMode.Impulse);
                OnForceApplied?.Invoke(Vector3.back);
                greenKeyPressCount = 0;
            }
        }

        // Red — left (instant, no threshold)
        if (IsRedKeyDown() && CanMoveTo(Vector3.left))
        {
            playerRigidbody.AddForce(Vector3.left * moveForce * redKeyForceCorrection, ForceMode.Impulse);
            OnForceApplied?.Invoke(Vector3.left);
        }

        // Blue — right (instant, no threshold)
        if (IsBlueKeyDown() && CanMoveTo(Vector3.right))
        {
            playerRigidbody.AddForce(Vector3.right * moveForce * blueKeyForceCorrection, ForceMode.Impulse);
            OnForceApplied?.Invoke(Vector3.right);
        }
    }

    void FixedUpdate()
    {
        if (playerRigidbody == null) return;

        Vector3 currentPosition = playerRigidbody.position;
        Vector3 currentVelocity = playerRigidbody.linearVelocity;
        Vector3 returnAcceleration = Vector3.zero;

        if (currentPosition.x < minX)
        {
            returnAcceleration.x = (minX - currentPosition.x) * boundaryReturnAcceleration;
            if (currentVelocity.x < 0f)
                currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, 0f, boundaryDamping * Time.fixedDeltaTime);
        }
        else if (currentPosition.x > maxX)
        {
            returnAcceleration.x = (maxX - currentPosition.x) * boundaryReturnAcceleration;
            if (currentVelocity.x > 0f)
                currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, 0f, boundaryDamping * Time.fixedDeltaTime);
        }

        if (currentPosition.z < minZ)
        {
            returnAcceleration.z = (minZ - currentPosition.z) * boundaryReturnAcceleration;
            if (currentVelocity.z < 0f)
                currentVelocity.z = Mathf.MoveTowards(currentVelocity.z, 0f, boundaryDamping * Time.fixedDeltaTime);
        }
        else if (currentPosition.z > maxZ)
        {
            returnAcceleration.z = (maxZ - currentPosition.z) * boundaryReturnAcceleration;
            if (currentVelocity.z > 0f)
                currentVelocity.z = Mathf.MoveTowards(currentVelocity.z, 0f, boundaryDamping * Time.fixedDeltaTime);
        }

        playerRigidbody.linearVelocity = currentVelocity;

        if (returnAcceleration != Vector3.zero)
            playerRigidbody.AddForce(returnAcceleration, ForceMode.Acceleration);
    }

    private bool CanMoveTo(Vector3 direction)
    {
        Vector3 pos = playerRigidbody.position;

        if (direction.x < 0f && pos.x <= minX + inputBoundaryBuffer) return false;
        if (direction.x > 0f && pos.x >= maxX - inputBoundaryBuffer) return false;
        if (direction.z < 0f && pos.z <= minZ + inputBoundaryBuffer) return false;
        if (direction.z > 0f && pos.z >= maxZ - inputBoundaryBuffer) return false;

        return true;
    }

    // Returns true if any key in the array is pressed this frame.
    private static bool IsAnyKeyDown(KeyCode[] keys)
    {
        if (keys == null) return false;
        foreach (var key in keys)
            if (Input.GetKeyDown(key)) return true;
        return false;
    }
}
