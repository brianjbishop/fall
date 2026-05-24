using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerSpinController spinController;

    [Header("Animator Parameters")]
    [SerializeField] private string isHitParam = "isHit";

    private bool lastIsHit;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (spinController == null)
        {
            spinController = GetComponent<PlayerSpinController>();
        }
    }

    private void Update()
    {
        if (animator == null || spinController == null)
        {
            return;
        }

        bool isHit = !spinController.IsIdle;
        if (isHit == lastIsHit)
        {
            return;
        }

        lastIsHit = isHit;
        animator.SetBool(isHitParam, isHit);
    }
}
