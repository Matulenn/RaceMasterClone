using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovingObstacle : MonoBehaviour
{
    [Header("Movement Points")]
    [SerializeField] private Transform pointLeft;
    [SerializeField] private Transform pointRight;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 2f;
    [SerializeField] private bool startOnLeft = true;

    private Rigidbody rb;
    private bool movingRight;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (pointLeft == null || pointRight == null)
        {
            Debug.LogError(
                "MovingObstacle: Priraď oba krajné body.",
                this
            );

            enabled = false;
            return;
        }

        rb.useGravity = false;
        rb.isKinematic = true;

        rb.position = startOnLeft
            ? pointLeft.position
            : pointRight.position;

        movingRight = startOnLeft;
    }

    private void FixedUpdate()
    {
        Vector3 targetPosition = movingRight
            ? pointRight.position
            : pointLeft.position;

        Vector3 nextPosition = Vector3.MoveTowards(
            rb.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(nextPosition);

        if ((nextPosition - targetPosition).sqrMagnitude < 0.000001f)
        {
            movingRight = !movingRight;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (pointLeft == null || pointRight == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pointLeft.position, pointRight.position);

        Gizmos.DrawWireSphere(pointLeft.position, 0.2f);
        Gizmos.DrawWireSphere(pointRight.position, 0.2f);
    }
}