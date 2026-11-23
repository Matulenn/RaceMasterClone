using UnityEngine;

public class CapsuleFollowCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera Position")]
    [SerializeField, Min(0.1f)]
    private float distance = 9f;

    [SerializeField, Min(0.1f)]
    private float height = 7f;

    [SerializeField, Min(0.01f)]
    private float positionSmoothTime = 0.2f;

    [Header("Camera Direction")]
    [SerializeField, Min(0.1f)]
    private float directionSharpness = 5f;

    [SerializeField, Min(0f)]
    private float lookAhead = 3f;

    [SerializeField]
    private float lookHeight = 0.5f;

    [Tooltip("Smer jazdy predtým, než sa pretekár začne hýbať.")]
    [SerializeField]
    private Vector3 initialDirection = Vector3.forward;

    private Rigidbody targetBody;
    private Transform cachedTarget;

    private Vector3 followDirection;
    private Vector3 positionVelocity;

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (cachedTarget != target)
        {
            InitializeTarget();
        }

        Vector3 movement = Vector3.zero;

        if (targetBody != null)
        {
#if UNITY_6000_0_OR_NEWER
            movement = targetBody.linearVelocity;
#else
            movement = targetBody.velocity;
#endif
        }

        movement.y = 0f;

        // Pri zastavení si kamera ponechá posledný smer.
        if (movement.sqrMagnitude > 0.04f)
        {
            float blend =
                1f - Mathf.Exp(
                    -directionSharpness * Time.deltaTime
                );

            followDirection = Vector3.Slerp(
                followDirection,
                movement.normalized,
                blend
            ).normalized;
        }

        Vector3 desiredPosition =
            target.position
            - followDirection * distance
            + Vector3.up * height;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref positionVelocity,
            positionSmoothTime
        );

        Vector3 lookPosition =
            target.position
            + followDirection * lookAhead
            + Vector3.up * lookHeight;

        transform.LookAt(lookPosition);
    }

    private void InitializeTarget()
    {
        cachedTarget = target;
        targetBody = target.GetComponent<Rigidbody>();

        followDirection = initialDirection;
        followDirection.y = 0f;

        if (followDirection.sqrMagnitude < 0.0001f)
        {
            followDirection = Vector3.forward;
        }

        followDirection.Normalize();
        positionVelocity = Vector3.zero;

        transform.position =
            target.position
            - followDirection * distance
            + Vector3.up * height;
    }
}