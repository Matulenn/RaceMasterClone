using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AIPathFollower : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private AIPath path;

    [SerializeField, Min(0)]
    private int currentPointIndex;

    [Header("Movement")]
    [SerializeField, Min(0.1f)]
    private float moveSpeed = 4f;

    [SerializeField, Min(0.1f)]
    private float acceleration = 15f;

    [SerializeField, Min(0.1f)]
    private float reachDistance = 0.8f;

    [Header("Driving Line")]
    [Tooltip("Záporné = vľavo, kladné = vpravo v smere jazdy.")]
    [SerializeField] private float laneOffset = 0f;

    [SerializeField] private Color pathColor = Color.magenta;

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
        int checkedPoints = 0;

        while (toTarget.magnitude <= reachDistance &&
               checkedPoints < path.PointCount)
        {
            currentPointIndex =
                (currentPointIndex + 1) % path.PointCount;

            toTarget = GetHorizontalTargetDirection();
            checkedPoints++;
        }

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

        rb.AddForce(
            velocityChange,
            ForceMode.VelocityChange
        );
    }

    private Vector3 GetOffsetPoint(int index)
    {
        int count = path.PointCount;

        Vector3 current = path.GetPoint(index);

        int previousIndex = (index - 1 + count) % count;
        int nextIndex = (index + 1) % count;

        Vector3 forward =
            path.GetPoint(nextIndex) -
            path.GetPoint(previousIndex);

        forward.y = 0f;

        // Záloha pri zhodných susedných bodoch.
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = path.GetPoint(nextIndex) - current;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.0001f)
            return current;

        Vector3 right = Vector3.Cross(
            Vector3.up,
            forward.normalized
        ).normalized;

        return current + right * laneOffset;
    }

    private Vector3 GetHorizontalTargetDirection()
    {
        Vector3 direction =
            GetOffsetPoint(currentPointIndex) - rb.position;

        direction.y = 0f;

        return direction;
    }

    private void OnDrawGizmosSelected()
    {
        if (path == null || path.PointCount < 2)
            return;

        Gizmos.color = pathColor;

        for (int i = 0; i < path.PointCount; i++)
        {
            Vector3 current = GetOffsetPoint(i);

            Vector3 next = GetOffsetPoint(
                (i + 1) % path.PointCount
            );

            Gizmos.DrawLine(current, next);
            Gizmos.DrawSphere(current, 0.12f);
        }

        int targetIndex = Mathf.Clamp(
            currentPointIndex,
            0,
            path.PointCount - 1
        );

        Gizmos.color = Color.red;

        Gizmos.DrawLine(
            transform.position,
            GetOffsetPoint(targetIndex)
        );
    }
}