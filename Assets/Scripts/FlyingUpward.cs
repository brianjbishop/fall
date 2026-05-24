using UnityEngine;

public class FlyingUpward : MonoBehaviour
{
    [SerializeField] private float startSpeed = 1f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float speedIncreasePerSecond = 0.5f;
    [SerializeField] private float timeScaleCorrection = 1f;
    [SerializeField] private float timeToSpeedExponent = 1f;
    [SerializeField] private float lifeTime = 10f;
    [Header("Proximity Sound")]
    [SerializeField] private float playerDistanceThreshold = 0.5f;
    [Header("Spin")]
    [SerializeField] private bool enableSpin = true;
    [SerializeField] private float randomSpinMinSpeed = 10f;
    [SerializeField] private float randomSpinMaxSpeed = 40f;

    private Vector3 actualSpinAxis;
    private float actualSpinSpeed;
    private Transform playerTransform;
    private Collider obstacleCollider;
    private Collider playerCollider;
    private AudioSource audioSource;
    private bool hasPlayedProximitySound;

    private void Start()
    {
        Destroy(gameObject, lifeTime);

        actualSpinAxis = Random.onUnitSphere;
        actualSpinSpeed = Random.Range(randomSpinMinSpeed, randomSpinMaxSpeed);
        obstacleCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        TryFindPlayer();
    }

    private void Update()
    {
        TryPlayProximitySound();

        float globalMult   = GameManager.Instance != null ? GameManager.Instance.SpeedMultiplier : 1f;
        float correctedTime = Mathf.Max(0f, Time.timeSinceLevelLoad * timeScaleCorrection);
        float timeFactor = Mathf.Pow(correctedTime, Mathf.Max(0f, timeToSpeedExponent));
        float currentSpeed = Mathf.Min(maxSpeed, startSpeed + timeFactor * speedIncreasePerSecond) * globalMult;
        transform.position += Vector3.up * currentSpeed * Time.deltaTime;

        if (enableSpin && (actualSpinAxis.sqrMagnitude > 0f) && actualSpinSpeed != 0f)
        {
            transform.Rotate(actualSpinAxis, actualSpinSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void TryFindPlayer()
    {
        if (playerTransform != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerCollider = playerObject.GetComponent<Collider>();
        }
    }

    private void TryPlayProximitySound()
    {
        TryFindPlayer();

        if (playerTransform == null)
        {
            hasPlayedProximitySound = false;
            return;
        }

            // compute both center distance and closest-point distance for debugging
            float centerDistance = Vector3.Distance(transform.position, playerTransform.position);

            Vector3 obstaclePoint = transform.position;
            if (obstacleCollider != null)
            {
                obstaclePoint = obstacleCollider.ClosestPoint(playerTransform.position);
            }

            Vector3 playerPoint = playerTransform.position;
            if (playerCollider != null)
            {
                playerPoint = playerCollider.ClosestPoint(obstaclePoint);
            }

            float closestDistance = Vector3.Distance(obstaclePoint, playerPoint);

            // Use centerDistance as the authoritative check (matches visual expectation),
            // but log both so we can see why ClosestPoint may report zero.
            Debug.Log($"[FlyingUpward] Check: centerDist={centerDistance:F3}, closestDist={closestDistance:F3}, threshold={playerDistanceThreshold:F3}, result={(centerDistance < playerDistanceThreshold)}");
            if (centerDistance < playerDistanceThreshold)
            {
                Debug.Log($"[FlyingUpward] Proximity triggered on: {gameObject.name}, centerDist={centerDistance:F3}, closestDist={closestDistance:F3}, threshold={playerDistanceThreshold:F3}");
                if (!hasPlayedProximitySound && audioSource != null)
                {
                    Debug.Log("Playing proximity sound from: " + gameObject.name);
                    audioSource.PlayOneShot(audioSource.clip);
                }

                hasPlayedProximitySound = true;
            }
            else
            {
                hasPlayedProximitySound = false;
            }
    }
}