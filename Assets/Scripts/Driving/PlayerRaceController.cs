using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(RaceProgress))]
public class PlayerRaceController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.1f)]
    private float moveSpeed = 5f;

    [SerializeField, Min(0.1f)]
    private float acceleration = 15f;

    [SerializeField, Min(0.1f)]
    private float braking = 12f;

    [SerializeField, Min(1f)]
    private float turnSpeed = 100f;

    [Header("Starting Direction")]
    [Tooltip("0 = +Z, 90 = +X, 180 = -Z, 270 = -X.")]
    [SerializeField] private float startingYaw = 0f;

    private Rigidbody rb;
    private RaceProgress raceProgress;

    private bool raceStarted;
    private float throttleInput;
    private float steeringInput;
    private float headingYaw;

    public void SetRaceStarted(bool started)
    {
        raceStarted = started;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        raceProgress = GetComponent<RaceProgress>();

        headingYaw = startingYaw;
    }

    private void Update()
    {
        throttleInput = 0f;
        steeringInput = 0f;

        if (!raceStarted || raceProgress.Finished)
            return;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.wKey.isPressed ||
            keyboard.upArrowKey.isPressed)
        {
            throttleInput += 1f;
        }

        if (keyboard.sKey.isPressed ||
            keyboard.downArrowKey.isPressed)
        {
            throttleInput -= 1f;
        }

        if (keyboard.dKey.isPressed ||
            keyboard.rightArrowKey.isPressed)
        {
            steeringInput += 1f;
        }

        if (keyboard.aKey.isPressed ||
            keyboard.leftArrowKey.isPressed)
        {
            steeringInput -= 1f;
        }
#else
        if (Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.UpArrow))
        {
            throttleInput += 1f;
        }

        if (Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.DownArrow))
        {
            throttleInput -= 1f;
        }

        if (Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.RightArrow))
        {
            steeringInput += 1f;
        }

        if (Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.LeftArrow))
        {
            steeringInput -= 1f;
        }
#endif
    }

    private void FixedUpdate()
    {
        if (!raceStarted || raceProgress.Finished)
        {
            ApplyHorizontalVelocity(Vector3.zero, braking);
            return;
        }

        // Jednoduché testovacie riadenie:
        // smer možno meniť aj počas státia.
        headingYaw +=
            steeringInput * turnSpeed * Time.fixedDeltaTime;

        Vector3 forward =
            Quaternion.Euler(0f, headingYaw, 0f)
            * Vector3.forward;

        Vector3 desiredVelocity =
            forward * throttleInput * moveSpeed;

        float changeRate =
            Mathf.Abs(throttleInput) > 0.01f
                ? acceleration
                : braking;

        ApplyHorizontalVelocity(
            desiredVelocity,
            changeRate
        );
    }

    private void ApplyHorizontalVelocity(
        Vector3 desiredVelocity,
        float changeRate)
    {
#if UNITY_6000_0_OR_NEWER
        Vector3 velocity = rb.linearVelocity;
#else
        Vector3 velocity = rb.velocity;
#endif

        Vector3 horizontalVelocity =
            new Vector3(velocity.x, 0f, velocity.z);

        Vector3 velocityChange = Vector3.ClampMagnitude(
            desiredVelocity - horizontalVelocity,
            changeRate * Time.fixedDeltaTime
        );

        rb.AddForce(
            velocityChange,
            ForceMode.VelocityChange
        );
    }
}