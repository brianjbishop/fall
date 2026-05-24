using UnityEngine;

public class ProjectileMover : MonoBehaviour
{
    [Tooltip("Flight speed (units/second)")]
    public float speed = 8f;

    private Vector3 _direction;
    private bool    _ready;

    // Called by CannonSpawner immediately after Instantiate
    public void Init(Vector3 direction)
    {
        _direction = direction;
        _ready     = true;
    }

    private void Update()
    {
        if (!_ready) return;
        transform.Translate(_direction * speed * Time.deltaTime, Space.World);
    }
}
