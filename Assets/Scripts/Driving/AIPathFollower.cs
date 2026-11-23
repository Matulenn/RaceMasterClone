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
    [SerializeField] private float laneOffset;

    [SerializeField, Min(0.1f)]
    private float laneChangeSpeed = 1f;

    [SerializeField] private Color pathColor = Color.magenta;

    [Header("Braking")]
    [Tooltip("Brzdenie pred štartom a po dokončení pretekov.")]
    [SerializeField, Min(0.1f)]
    private float finishBraking = 10f;

    [Header("Catch Up")]
    [SerializeField] private RaceCatchUp catchUp;

    [Header("Runtime Status")]
    [SerializeField] private bool raceStarted;
    [SerializeField] private float currentLaneOffset;
    [SerializeField] private float targetLaneOffset;
    [SerializeField] private float requestedSpeed;
    [SerializeField] private float allowedSpeed;

    private Rigidbody rb;
    private RaceProgress raceProgress;
    private AITrafficAwareness trafficAwareness;
    private AILaneChanger laneChanger;
    private AICollisionReaction collisionReaction;

    public bool CanReactToImpact =>
        isActiveAndEnabled &&
        raceStarted &&
        (raceProgress == null || !raceProgress.Finished);

    public void SetRaceStarted(bool started)
    {
        raceStarted = started;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        raceProgress = GetComponent<RaceProgress>();
        trafficAwareness = GetComponent<AITrafficAwareness>();
        laneChanger = GetComponent<AILaneChanger>();
        collisionReaction = GetComponent<AICollisionReaction>();

        currentLaneOffset = laneOffset;
        targetLaneOffset = laneOffset;
    }

    private void Start()
    {
        if (path == null || path.PointCount < 2)
        {
            Debug.LogError(
                $"{name}: AI potrebuje priradenú trasu " +
                "aspoň s dvomi bodmi.",
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

        if (raceProgress == null)
        {
            Debug.LogWarning(
                $"{name}: Chýba RaceProgress. " +
                "AI nebude vedieť zastaviť po dokončení pretekov.",
                this
            );
        }

        if (collisionReaction == null)
        {
            Debug.LogWarning(
                $"{name}: Chýba AICollisionReaction. " +
                "AI bude jazdiť bez reakcie na náraz.",
                this
            );
        }
    }

    private void FixedUpdate()
    {
        if (!raceStarted ||
            (raceProgress != null && raceProgress.Finished))
        {
            requestedSpeed = 0f;
            allowedSpeed = 0f;

            BrakeAfterFinish();
            return;
        }

        if (path == null || path.PointCount < 2)
            return;

        currentPointIndex = Mathf.Clamp(
            currentPointIndex,
            0,
            path.PointCount - 1
        );

        float normalMultiplier =
            catchUp != null && raceProgress != null
                ? catchUp.GetMultiplier(raceProgress)
                : 1f;

        float impactMultiplier =
            collisionReaction != null
                ? collisionReaction.SpeedMultiplier
                : 1f;

        // Spomalenie alebo bonus po náraze má prednosť.
        // Dva bonusy sa medzi sebou nenásobia.
        float effectiveMultiplier =
            !Mathf.Approximately(impactMultiplier, 1f)
                ? impactMultiplier
                : normalMultiplier;

        requestedSpeed =
            moveSpeed * Mathf.Max(0f, effectiveMultiplier);

        UpdateDrivingLane(requestedSpeed);

        Vector3 toTarget = GetHorizontalTargetDirection();
        int checkedPoints = 0;

        while (toTarget.sqrMagnitude <=
                   reachDistance * reachDistance &&
               checkedPoints < path.PointCount)
        {
            currentPointIndex =
                (currentPointIndex + 1) % path.PointCount;

            toTarget = GetHorizontalTargetDirection();
            checkedPoints++;
        }

        if (toTarget.sqrMagnitude < 0.0001f)
        {
            allowedSpeed = 0f;
            BrakeAfterFinish();
            return;
        }

        Vector3 driveDirection = toTarget.normalized;

        allowedSpeed = requestedSpeed;

        if (trafficAwareness != null &&
            trafficAwareness.isActiveAndEnabled)
        {
            allowedSpeed = trafficAwareness.GetAllowedSpeed(
                requestedSpeed,
                driveDirection
            );
        }

        allowedSpeed = Mathf.Clamp(
            allowedSpeed,
            0f,
            requestedSpeed
        );

        Vector3 desiredVelocity =
            driveDirection * allowedSpeed;

        Vector3 velocity = GetBodyVelocity();

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

    private void UpdateDrivingLane(float desiredSpeed)
    {
        int count = path.PointCount;

        int previous =
            (currentPointIndex - 1 + count) % count;

        int next =
            (currentPointIndex + 1) % count;

        Vector3 forward =
            path.GetPoint(next) - path.GetPoint(previous);

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward =
                path.GetPoint(next) -
                path.GetPoint(currentPointIndex);

            forward.y = 0f;
        }

        if (laneChanger != null &&
            laneChanger.isActiveAndEnabled &&
            forward.sqrMagnitude >= 0.0001f)
        {
            targetLaneOffset = laneChanger.ChooseOffset(
                path.GetPoint(currentPointIndex),
                forward,
                currentLaneOffset,
                targetLaneOffset,
                desiredSpeed
            );
        }
        else
        {
            targetLaneOffset = laneOffset;
        }

        currentLaneOffset = Mathf.MoveTowards(
            currentLaneOffset,
            targetLaneOffset,
            laneChangeSpeed * Time.fixedDeltaTime
        );
    }

    private void BrakeAfterFinish()
    {
        Vector3 velocity = GetBodyVelocity();

        Vector3 horizontalVelocity =
            new Vector3(velocity.x, 0f, velocity.z);

        Vector3 velocityChange = Vector3.ClampMagnitude(
            -horizontalVelocity,
            finishBraking * Time.fixedDeltaTime
        );

        rb.AddForce(
            velocityChange,
            ForceMode.VelocityChange
        );
    }

    private Vector3 GetBodyVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
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

        float offset =
            Application.isPlaying
                ? currentLaneOffset
                : laneOffset;

        return current + right * offset;
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