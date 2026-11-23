using UnityEngine;

public class AILaneChanger : MonoBehaviour
{
    [Header("Racers")]
    [SerializeField] private RaceProgress[] racers;

    [Header("Behaviour")]
    [SerializeField] private bool allowOvertaking = true;

    [SerializeField] private float[] availableOffsets =
        { -1.5f, 0f, 1.5f };

    [Header("Detection")]
    [SerializeField, Min(0.1f)]
    private float detectionDistance = 10f;

    [SerializeField, Min(0.1f)]
    private float lateralClearance = 1.2f;

    [SerializeField, Min(0f)]
    private float speedAdvantage = 0.3f;

    [Header("Lane Safety")]
    [SerializeField, Min(0.1f)]
    private float frontClearance = 7f;

    [SerializeField, Min(0.1f)]
    private float rearClearance = 5f;

    [SerializeField, Min(0.1f)]
    private float crossingClearance = 3f;

    [SerializeField, Min(0.1f)]
    private float changeCooldown = 3f;

    private Rigidbody rb;
    private Rigidbody[] bodies;

    private float nextChangeTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        bodies = new Rigidbody[
            racers == null ? 0 : racers.Length
        ];

        for (int i = 0; i < bodies.Length; i++)
        {
            if (racers[i] != null)
            {
                bodies[i] =
                    racers[i].GetComponent<Rigidbody>();
            }
        }
    }

    public float ChooseOffset(
        Vector3 centerPoint,
        Vector3 trackForward,
        float currentOffset,
        float targetOffset,
        float requestedSpeed)
    {
        if (!allowOvertaking ||
            Time.time < nextChangeTime ||
            Mathf.Abs(currentOffset - targetOffset) > 0.15f)
        {
            return targetOffset;
        }

        trackForward.y = 0f;

        if (trackForward.sqrMagnitude < 0.0001f)
            return targetOffset;

        Vector3 forward = trackForward.normalized;
        Vector3 right =
            Vector3.Cross(Vector3.up, forward).normalized;

        if (!HasSlowerRacerAhead(
                forward,
                right,
                requestedSpeed))
        {
            return targetOffset;
        }

        float chosenOffset = targetOffset;
        float smallestChange = float.PositiveInfinity;

        foreach (float candidate in availableOffsets)
        {
            float difference =
                Mathf.Abs(candidate - currentOffset);

            if (difference < 0.2f ||
                difference >= smallestChange)
            {
                continue;
            }

            if (!IsLaneChangeClear(
                    centerPoint,
                    forward,
                    right,
                    candidate))
            {
                continue;
            }

            chosenOffset = candidate;
            smallestChange = difference;
        }

        if (smallestChange < float.PositiveInfinity)
        {
            nextChangeTime = Time.time + changeCooldown;
        }

        return chosenOffset;
    }

    private bool HasSlowerRacerAhead(
        Vector3 forward,
        Vector3 right,
        float requestedSpeed)
    {
        foreach (Rigidbody other in bodies)
        {
            if (!CanConsider(other))
                continue;

            Vector3 delta = other.position - rb.position;

            if (Mathf.Abs(delta.y) > 2f)
                continue;

            delta.y = 0f;

            float ahead = Vector3.Dot(delta, forward);
            float sideways =
                Mathf.Abs(Vector3.Dot(delta, right));

            if (ahead <= 0f ||
                ahead > detectionDistance ||
                sideways > lateralClearance)
            {
                continue;
            }

            float otherSpeed = Mathf.Max(
                0f,
                Vector3.Dot(GetVelocity(other), forward)
            );

            if (requestedSpeed > otherSpeed + speedAdvantage)
                return true;
        }

        return false;
    }

    private bool IsLaneChangeClear(
        Vector3 centerPoint,
        Vector3 forward,
        Vector3 right,
        float candidate)
    {
        float ownLateral = Vector3.Dot(
            rb.position - centerPoint,
            right
        );

        float sweepMin =
            Mathf.Min(ownLateral, candidate)
            - lateralClearance;

        float sweepMax =
            Mathf.Max(ownLateral, candidate)
            + lateralClearance;

        foreach (Rigidbody other in bodies)
        {
            if (!CanConsider(other))
                continue;

            Vector3 delta = other.position - rb.position;

            if (Mathf.Abs(delta.y) > 2f)
                continue;

            delta.y = 0f;

            float ahead = Vector3.Dot(delta, forward);

            float otherLateral = Vector3.Dot(
                other.position - centerPoint,
                right
            );

            bool inTargetLane =
                Mathf.Abs(otherLateral - candidate)
                < lateralClearance;

            if (inTargetLane &&
                ahead > -rearClearance &&
                ahead < frontClearance)
            {
                return false;
            }

            bool inCrossingCorridor =
                otherLateral >= sweepMin &&
                otherLateral <= sweepMax;

            if (inCrossingCorridor &&
                Mathf.Abs(ahead) < crossingClearance)
            {
                return false;
            }
        }

        return true;
    }

    private bool CanConsider(Rigidbody other)
    {
        return other != null &&
               other != rb &&
               other.gameObject.activeInHierarchy;
    }

    private static Vector3 GetVelocity(Rigidbody body)
    {
#if UNITY_6000_0_OR_NEWER
        return body.linearVelocity;
#else
        return body.velocity;
#endif
    }
}