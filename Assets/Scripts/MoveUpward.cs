using UnityEngine;

public class MoveUpward : MonoBehaviour
{
    [Tooltip("Upward movement speed (units/second)")]
    public float speed = 3f;

    void Update()
    {
        float globalMult = GameManager.Instance != null ? GameManager.Instance.SpeedMultiplier : 1f;
        transform.Translate(Vector3.up * speed * globalMult * Time.deltaTime, Space.World);
    }
}
