using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AITrafficAwareness : MonoBehaviour
{
    [Header("Other racers - include player and all AI")]
    [SerializeField] private RaceProgress[] racers;

    [Header("Detection")]
    [SerializeField, Min(0.1f)]
    private float detectionDistance = 10f;

    [Tooltip("Maximálna bočná vzdialenosť stredov pretekárov.")]
    [SerializeField, Min(0.1f)]
    private float lateralTolerance = 1.2f;

    [Header("Following")]
    [Tooltip("Požadovaný minimálny odstup medzi stredmi objektov.")]
    [SerializeField, Min(0.1f)]
    private float minimumGap = 2f;

    [Tooltip("Dodatočný odstup podľa vlastnej rýchlosti.")]
    [SerializeField, Min(0f)]
    private float timeGap = 0.6f;

    [Tooltip("Ako silno rozdiel odstupu ovplyvní požadovanú rýchlosť.")]
    [SerializeField, Min(0.1f)]
    private float followingGain = 1.5f;

    [Header("Runtime Status")]
    [SerializeField] private string followingRacer = "";
    [SerializeField] private float recommendedSpeed;

    private Rigidbody rb;
    private Rigidbody[] racerBodies;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (racers == null)
        {
            racerBodies = new Rigidbody[0];
            return;
        }

        racerBodies = new Rigidbody[racers.Length];

        for (int i = 0; i < racers.Length; i++)
        {
            if (racers[i] != null)
            {
                racerBodies[i] =
                    racers[i].GetComponent<Rigidbody>();
            }
        }
    }

    public float GetAllowedSpeed(
        float requestedSpeed,
        Vector3 driveDirection)
    {
        recommendedSpeed = requestedSpeed;
        followingRacer = "";

        driveDirection.y = 0f;

        if (driveDirection.sqrMagnitude < 0.0001f)
            return requestedSpeed;

        Vector3 forward = driveDirection.normalized;
        Vector3 right =
            Vector3.Cross(Vector3.up, forward).normalized;

        float ownForwardSpeed = Mathf.Max(
            0f,
            Vector3.Dot(GetVelocity(rb), forward)
        );

        float desiredGap =
            minimumGap + ownForwardSpeed * timeGap;

        foreach (Rigidbody otherBody in racerBodies)
        {
            if (otherBody == null ||
                otherBody == rb ||
                !otherBody.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 toOther =
                otherBody.position - rb.position;

            // Nezohľadňuj vozidlo na výrazne inej výške.
            if (Mathf.Abs(toOther.y) > 2f)
                continue;

            toOther.y = 0f;

            float forwardDistance =
                Vector3.Dot(toOther, forward);

            if (forwardDistance <= 0f ||
                forwardDistance > detectionDistance)
            {
                continue;
            }

            float lateralDistance = Mathf.Abs(
                Vector3.Dot(toOther, right)
            );

            if (lateralDistance > lateralTolerance)
                continue;

            float leaderSpeed = Mathf.Max(
                0f,
                Vector3.Dot(
                    GetVelocity(otherBody),
                    forward
                )
            );

            float gapError =
                forwardDistance - desiredGap;

            float allowedSpeed = Mathf.Clamp(
                leaderSpeed + gapError * followingGain,
                0f,
                requestedSpeed
            );

            if (allowedSpeed < recommendedSpeed)
            {
                recommendedSpeed = allowedSpeed;
                followingRacer = otherBody.name;
            }
        }

        return recommendedSpeed;
    }

    private static Vector3 GetVelocity(Rigidbody body)
    {
#if UNITY_6000_0_OR_NEWER
        return body.linearVelocity;
#else
        return body.velocity;
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionDistance
        );
    }
}