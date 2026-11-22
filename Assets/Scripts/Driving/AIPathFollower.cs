using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AIPathFollower : MonoBehaviour
{
    [SerializeField] private AIPath path;

    [SerializeField, Min(0.1f)]
    private float moveSpeed = 4f;

    [SerializeField, Min(0.1f)]
    private float acceleration = 15f;

    [SerializeField, Min(0.1f)]
    private float reachDistance = 0.8f;

    [SerializeField, Min(0)]
    private int currentPointIndex;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (path == null || path.PointCount < 2)
        {
            Debug.LogError(
                "AI potrebuje priradenú trasu aspoň s dvomi bodmi.",
                this
            );

            enabled = false;
            return;
        }

        currentPointIndex = Mathf.Clamp(
            currentPointIndex,
            0,
            path.PointCount - 1
        );
    }

    private void FixedUpdate()
    {
        if (path == null || path.PointCount < 2)
            return;

        currentPointIndex %= path.PointCount;

        Vector3 toTarget = GetHorizontalTargetDirection();

        // Preskočí dosiahnuté alebo tesne susediace body.
        // Limit zabraňuje nekonečnému cyklu.
        int checkedPoints = 0;

        while (toTarget.magnitude <= reachDistance &&
               checkedPoints < path.PointCount)
        {
            currentPointIndex =
                (currentPointIndex + 1) % path.PointCount;

            toTarget = GetHorizontalTargetDirection();
            checkedPoints++;
        }

        // Požadovaná rýchlosť už neklesá pri približovaní k bodu.
        Vector3 desiredVelocity =
            toTarget.normalized * moveSpeed;

#if UNITY_6000_0_OR_NEWER
        Vector3 velocity = rb.linearVelocity;
#else
        Vector3 velocity = rb.velocity;
#endif

        Vector3 horizontalVelocity =
            new Vector3(velocity.x, 0f, velocity.z);

        Vector3 velocityChange =
            desiredVelocity - horizontalVelocity;

        velocityChange = Vector3.ClampMagnitude(
            velocityChange,
            acceleration * Time.fixedDeltaTime
        );

        // Zvislý pohyb zostáva na gravitácii a kolíziách.
        rb.AddForce(
            velocityChange,
            ForceMode.VelocityChange
        );
    }

    private Vector3 GetHorizontalTargetDirection()
    {
        Vector3 direction =
            path.GetPoint(currentPointIndex) - rb.position;

        direction.y = 0f;

        return direction;
    }

    private void OnDrawGizmosSelected()
    {
        if (path == null || path.PointCount < 2)
            return;

        int index = Mathf.Clamp(
            currentPointIndex,
            0,
            path.PointCount - 1
        );

        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            transform.position,
            path.GetPoint(index)
        );
    }
}