using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AIPathFollower))]
public class AICollisionReaction : MonoBehaviour
{
    [Header("Impact Detection")]
    [SerializeField, Min(0.1f)]
    private float minimumImpactSpeed = 1.5f;

    [SerializeField, Min(0f)]
    private float impactCooldown = 1.5f;

    [SerializeField]
    private bool reactToMovingObstacles = true;

    [Header("Spin And Slowdown")]
    [SerializeField, Min(0.01f)]
    private float slowdownDuration = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float slowdownMultiplier = 0.2f;

    [Header("Recovery Boost")]
    [SerializeField, Min(1f)]
    private float recoveryMultiplier = 1.6f;

    [SerializeField, Min(0f)]
    private float recoveryDuration = 6f;

    [SerializeField, Min(0.01f)]
    private float boostBuildTime = 0.75f;

    [SerializeField, Min(0.01f)]
    private float boostFadeTime = 1f;

    [Header("Runtime Status")]
    [SerializeField] private bool reacting;
    [SerializeField] private float currentMultiplier = 1f;

    private Rigidbody rb;
    private AIPathFollower follower;

    private float impactTime;
    private float nextAllowedImpactTime;

    private bool spinning;
    private Quaternion rotationBeforeImpact;
    private RigidbodyConstraints constraintsBeforeImpact;

    public float SpeedMultiplier
    {
        get
        {
            if (!isActiveAndEnabled)
                return 1f;

            return EvaluateMultiplier();
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        follower = GetComponent<AIPathFollower>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isActiveAndEnabled ||
            !follower.CanReactToImpact ||
            Time.time < nextAllowedImpactTime)
        {
            return;
        }

        bool hitPlayer =
            collision.collider
                .GetComponentInParent<PlayerRaceController>() != null;

        bool hitObstacle =
            reactToMovingObstacles &&
            collision.collider
                .GetComponentInParent<MovingObstacle>() != null;

        if (!hitPlayer && !hitObstacle)
            return;

        if (collision.relativeVelocity.sqrMagnitude <
            minimumImpactSpeed * minimumImpactSpeed)
        {
            return;
        }

        BeginReaction();
    }

    private void BeginReaction()
    {
        // Pri opakovanom náraze najprv obnov pôvodný stav.
        StopSpin();

        rotationBeforeImpact = rb.rotation;
        constraintsBeforeImpact = rb.constraints;

        // Zachovaj zamknuté pozície, uvoľni Y rotáciu
        // a počas otočky zabráň prevráteniu cez X a Z.
        rb.constraints =
            (constraintsBeforeImpact &
             ~RigidbodyConstraints.FreezeRotationY) |
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        rb.angularVelocity = Vector3.zero;

        impactTime = Time.time;
        nextAllowedImpactTime =
            impactTime +
            Mathf.Max(impactCooldown, slowdownDuration);

        reacting = true;
        spinning = true;

        Vector3 velocity = GetVelocity();
        velocity.x *= slowdownMultiplier;
        velocity.z *= slowdownMultiplier;
        SetVelocity(velocity);
    }

    private void FixedUpdate()
    {
        if (!reacting)
            return;

        if (!follower.CanReactToImpact)
        {
            ResetReaction();
            return;
        }

        float elapsed = Time.time - impactTime;

        if (spinning)
        {
            float progress = Mathf.Clamp01(
                elapsed / slowdownDuration
            );

            rb.angularVelocity = Vector3.zero;

            rb.MoveRotation(
                Quaternion.AngleAxis(
                    360f * progress,
                    Vector3.up
                ) * rotationBeforeImpact
            );

            if (progress >= 1f)
            {
                StopSpin();
            }
        }

        currentMultiplier = EvaluateMultiplier();

        if (elapsed >= slowdownDuration + recoveryDuration)
        {
            ResetReaction();
        }
    }

    private float EvaluateMultiplier()
    {
        if (!reacting ||
            follower == null ||
            !follower.CanReactToImpact)
        {
            return 1f;
        }

        float elapsed = Time.time - impactTime;

        if (elapsed < slowdownDuration)
            return slowdownMultiplier;

        float recoveryElapsed = elapsed - slowdownDuration;

        if (recoveryDuration <= 0f ||
            recoveryElapsed >= recoveryDuration)
        {
            return 1f;
        }

        float build = Mathf.Clamp01(
            recoveryElapsed / boostBuildTime
        );

        float fade = Mathf.Clamp01(
            (recoveryDuration - recoveryElapsed) /
            boostFadeTime
        );

        return Mathf.Lerp(
            1f,
            recoveryMultiplier,
            Mathf.Min(build, fade)
        );
    }

    private void StopSpin()
    {
        if (!spinning || rb == null)
            return;

        rb.angularVelocity = Vector3.zero;
        rb.rotation = rotationBeforeImpact;
        rb.constraints = constraintsBeforeImpact;

        spinning = false;
    }

    private void ResetReaction()
    {
        StopSpin();

        reacting = false;
        currentMultiplier = 1f;
    }

    private void OnDisable()
    {
        ResetReaction();
    }

    private Vector3 GetVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    private void SetVelocity(Vector3 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }
}