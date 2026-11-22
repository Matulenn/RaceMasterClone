using UnityEngine;

public class CapsuleFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;

    [SerializeField] private Vector3 offset =
        new Vector3(0f, 12f, -9f);

    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float lookHeight = 0.5f;

    private Vector3 followVelocity;

    private void Start()
    {
        if (target == null)
            return;

        transform.position = target.position + offset;
        LookAtTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            smoothTime
        );

        LookAtTarget();
    }

    private void LookAtTarget()
    {
        Vector3 lookPosition =
            target.position + Vector3.up * lookHeight;

        transform.LookAt(lookPosition);
    }
}