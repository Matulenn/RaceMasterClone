using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;
using Unity.Mathematics;
using Random = UnityEngine.Random;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    public enum DriverType { Player, AI }

    [Header("Vodič")]
    [Tooltip("Player = ovláda hráč klávesnicou. AI = ovláda počítač.")]
    public DriverType driver = DriverType.Player;

    [Header("Cesta")]
    [Tooltip("Objekt so Spline Container komponentom (stred cesty).")]
    public SplineContainer track;
    [Tooltip("Polovica šírky cesty v metroch.")]
    public float roadHalfWidth = 4f;

    [Header("Rýchlosť")]
    [Tooltip("Keď je vypnuté, auto zastaví (napr. pred štartom alebo po cieli).")]
    public bool isDriving = true;
    public float maxSpeed = 25f;
    [Tooltip("Zrýchlenie (m/s za sekundu).")]
    public float acceleration = 6f;
    [Tooltip("Spomalenie, keď je auto rýchlejšie ako má byť (m/s za sekundu).")]
    public float deceleration = 10f;
    [Tooltip("Násobok max. rýchlosti mimo cesty (0.5 = polovičná rýchlosť).")]
    [Range(0f, 1f)] public float offRoadSpeedFactor = 0.5f;
    [Tooltip("Násobok max. rýchlosti, ktorý môžu meniť iné skripty (boost, spomalenie...).")]
    public float speedMultiplier = 1f;

    [Header("Brzda")]
    [Tooltip("Ako rýchlo auto pri brzdení spomaľuje (m/s za sekundu).")]
    public float brakeDeceleration = 18f;
    [Tooltip("Najnižšia rýchlosť, na ktorú sa dá pribrzdiť (0 = až do zastavenia).")]
    public float minBrakeSpeed = 6f;

    [Header("Zatáčanie")]
    [Tooltip("Maximálna rýchlosť zatáčania od vodiča (stupne za sekundu).")]
    public float steerRate = 70f;
    [Tooltip("Ako rýchlo sa zatáčanie rozbehne a vráti na nulu.")]
    public float steerSmoothing = 6f;
    [Tooltip("Celkový limit otáčania auta (stupne za sekundu).")]
    public float maxTurnRate = 110f;
    [Tooltip("Ako rýchlo auto dosiahne požadované otáčanie.")]
    public float turnResponse = 8f;

    [Header("Asistencia držania cesty")]
    [Tooltip("Ako silno sa auto natáča v smere cesty, keď vodič nezatáča (0 = vypnuté).")]
    public float alignStrength = 2f;
    [Tooltip("Sila natáčania v smere cesty počas zatáčania (0–1 z alignStrength).")]
    [Range(0f, 1f)] public float alignWhileSteering = 0.25f;
    [Tooltip("Ako ďaleko dopredu asistencia sleduje smer cesty (m).")]
    public float assistLookAhead = 6f;
    [Tooltip("Od akej časti šírky cesty začne asistencia tlačiť auto späť (0.7 = 70 %).")]
    [Range(0f, 1f)] public float edgeAssistStart = 0.7f;
    [Tooltip("Ako silno sa auto pri okraji natočí späť (stupne za sekundu).")]
    public float edgeTurnStrength = 60f;
    [Tooltip("Ako silno je auto za okrajom cesty tlačené späť (m/s²).")]
    public float edgePushForce = 15f;

    [Header("Nárazy")]
    [Tooltip("Najmenšia sila nárazu, na ktorú auto reaguje (m/s).")]
    public float minImpactSpeed = 2f;
    [Tooltip("Sila odrazu od prekážky.")]
    public float bounceFactor = 0.4f;
    [Tooltip("Koľko percent rýchlosti auto stratí pri silnom náraze.")]
    [Range(0f, 1f)] public float impactSpeedLoss = 0.5f;
    [Tooltip("Ako dlho sa auto po silnom náraze spamätáva (s).")]
    public float recoveryTime = 1f;
    [Tooltip("Zrýchlenie počas spamätávania (násobok).")]
    [Range(0f, 1f)] public float recoveryAccelerationFactor = 0.3f;
    [Tooltip("Volá sa pri náraze, parameter je sila nárazu (napr. pre zvuk alebo otrasy kamery).")]
    public UnityEvent<float> onCrash;

    [Header("Fyzika")]
    [Tooltip("Priľnavosť pneumatík do strán. Nižšie = viac šmyku.")]
    public float sideGrip = 6f;
    [Tooltip("Výška ťažiska. Záporná = nižšie ťažisko, menej prevracania.")]
    public float centerOfMassHeight = -0.4f;
    [Tooltip("Ako silno sa auto snaží zostať na kolesách.")]
    public float uprightStrength = 5f;
    [Tooltip("Ako ďaleko pod autom sa hľadá zem.")]
    public float groundRayLength = 1.2f;
    public LayerMask groundLayers = ~0;

    [Header("Reset")]
    [Tooltip("Po koľkých sekundách v prevrátenej polohe sa auto vráti na cestu.")]
    public float flipResetDelay = 3f;
    [Tooltip("Po koľkých sekundách státia sa auto vráti na cestu (0 = vypnuté).")]
    public float stuckResetTime = 4f;
    public float resetHeight = 1f;

    [Header("AI – povaha vodiča (len pre AI)")]
    [Tooltip("Zručnosť: 1 = takmer bez chýb, 0 = veľa chýb.")]
    [Range(0f, 1f)] public float skill = 0.7f;
    [Tooltip("Náhodná odchýlka zručnosti pri štarte, aby každý súper jazdil inak.")]
    [Range(0f, 0.5f)] public float skillVariation = 0.15f;
    [Tooltip("Rozsah náhodnej max. rýchlosti (násobok), napr. 0.9–1.05.")]
    public Vector2 aiSpeedMultiplierRange = new Vector2(0.9f, 1.05f);

    [Header("AI – jazda v pruhu")]
    [Tooltip("Ako ďaleko od stredu cesty môže byť pruh (časť polovice šírky cesty).")]
    [Range(0f, 1f)] public float laneRange = 0.6f;
    [Tooltip("Ako často (s) AI zvažuje zmenu pruhu (min, max).")]
    public Vector2 laneChangeInterval = new Vector2(4f, 10f);
    [Tooltip("Ako ďaleko pred sebou AI mieri (m), plus časť podľa rýchlosti.")]
    public float aiLookAheadMin = 6f;
    public float aiLookAheadPerSpeed = 0.5f;
    [Tooltip("Uhol k cieľu, pri ktorom AI zatáča naplno (stupne).")]
    public float aiFullSteerAngle = 25f;

    [Header("AI – brzdenie pred zákrutami")]
    [Tooltip("Rýchlosť v najostrejšej zákrute (m/s).")]
    public float sharpCornerSpeed = 10f;
    [Tooltip("Uhol zákruty, ktorý sa považuje za najostrejší (stupne).")]
    public float sharpCornerAngle = 90f;

    [Header("AI – vyhýbanie")]
    [Tooltip("Ako ďaleko AI vidí prekážky (m), plus časť podľa rýchlosti.")]
    public float detectRange = 12f;
    public float detectPerSpeed = 0.8f;
    [Tooltip("Polomer „zraku“ (hrúbka lúča).")]
    public float detectRadius = 0.8f;
    [Tooltip("Výška lúčov nad stredom auta.")]
    public float detectHeight = 0.5f;
    [Tooltip("Vrstvy, ktoré AI vidí (prekážky, autá).")]
    public LayerMask obstacleLayers = ~0;
    [Tooltip("Ako silno sa AI vyhýba.")]
    public float avoidStrength = 1.5f;

    // ---------- Verejné hodnoty pre UI a iné skripty ----------
    public float CurrentSpeed { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsOffRoad { get; private set; }
    public bool IsRecovering => recoveryTimer > 0f;
    public bool IsBraking => brakeInput;
    public bool IsAI => driver == DriverType.AI;

    // ---------- Spoločné ----------
    private Rigidbody rb;
    private float steerInput;
    private bool brakeInput;
    private float steerSmoothed;
    private bool resetRequested;
    private float recoveryTimer;
    private float flippedTimer;
    private float stuckTimer;
    private float trackLength;
    private bool trackClosed;
    private Vector3 debugNearest;
    private Vector3 debugAhead;

    // ---------- AI ----------
    private float actualSkill;
    private float laneOffset;
    private float targetLaneOffset;
    private float nextLaneChange;
    private float noiseSeed;
    private float decisionTimer;
    private Collider ignoredObstacle;
    private float ignoreTimer;
    private Collider lastSeenObstacle;
    private float mistakeTimer;
    private float mistakeSteer;
    private float nextMistakeCheck;
    private Vector3 debugAITarget;

    // =====================================================================
    //  Unity
    // =====================================================================

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.centerOfMass = new Vector3(0f, centerOfMassHeight, 0f);
        ApplyFrictionlessMaterial();

        if (track != null)
        {
            trackLength = track.CalculateLength();
            trackClosed = track.Spline.Closed;
        }

        if (IsAI)
        {
            actualSkill = Mathf.Clamp01(skill + Random.Range(-skillVariation, skillVariation));
            speedMultiplier = Random.Range(aiSpeedMultiplierRange.x, aiSpeedMultiplierRange.y);
            noiseSeed = Random.Range(0f, 1000f);
        }
    }

    void Start()
    {
        if (track == null || trackLength <= 0f)
        {
            Debug.LogError("CarController: nie je priradená cesta (Track).", this);
            enabled = false;
            return;
        }

        if (IsAI)
        {
            laneOffset = targetLaneOffset = RandomLane();
            nextLaneChange = Time.time + Random.Range(laneChangeInterval.x, laneChangeInterval.y);
            nextMistakeCheck = Time.time + Random.Range(2f, 5f);
        }
        else
        {
            ApplyGarageUpgrades();
        }
    }

    private void ApplyGarageUpgrades()
    {
        PlayerDataManager data = PlayerDataManager.Instance;
        if (data == null) return;

        maxSpeed *= data.GetSpeedMultiplier();
        acceleration *= data.GetAccelerationMultiplier();
        brakeDeceleration *= data.GetAccelerationMultiplier();
        steerRate *= data.GetHandlingMultiplier();
        maxTurnRate *= data.GetHandlingMultiplier();
        sideGrip *= data.GetHandlingMultiplier();
    }

    void Update()
    {
        if (IsAI) UpdateAI(Time.deltaTime);
        else ReadPlayerInput();
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (resetRequested)
        {
            resetRequested = false;
            ResetToTrack();
            return;
        }

        if (recoveryTimer > 0f) recoveryTimer -= dt;
        steerSmoothed = Mathf.MoveTowards(steerSmoothed, steerInput, steerSmoothing * dt);

        Vector3 up = transform.up;
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        IsGrounded = Physics.Raycast(transform.position + up * 0.5f, -up,
            groundRayLength + 0.5f, groundLayers, QueryTriggerInteraction.Ignore);

        Vector3 velocity = GetVelocity();
        float forwardSpeed = Vector3.Dot(velocity, forward);
        CurrentSpeed = forwardSpeed;

        // --- Poloha voči ceste ---
        float t = GetNearestT(rb.position);
        EvaluateTrack(t, out Vector3 nearest, out Vector3 tangent, out Vector3 trackUp);
        Vector3 trackRight = Vector3.Cross(trackUp, tangent).normalized;
        float lateralOffset = Vector3.Dot(rb.position - nearest, trackRight);
        IsOffRoad = Mathf.Abs(lateralOffset) > roadHalfWidth;
        debugNearest = nearest;

        if (IsGrounded)
        {
            // --- Automatický pohyb dopredu ---
            float targetSpeed = isDriving ? maxSpeed * speedMultiplier : 0f;
            if (IsOffRoad) targetSpeed *= offRoadSpeedFactor;
            if (brakeInput) targetSpeed = Mathf.Min(targetSpeed, minBrakeSpeed);

            float speedDelta = targetSpeed - forwardSpeed;
            float rate = speedDelta > 0f
                ? acceleration * (IsRecovering ? recoveryAccelerationFactor : 1f)
                : (brakeInput ? brakeDeceleration : deceleration);
            float change = Mathf.Clamp(speedDelta, -rate * dt, rate * dt);
            rb.AddForce(forward * change, ForceMode.VelocityChange);

            // --- Priľnavosť pneumatík ---
            float sideSpeed = Vector3.Dot(velocity, right);
            rb.AddForce(-right * sideSpeed * Mathf.Clamp01(sideGrip * dt), ForceMode.VelocityChange);

            // --- Zatáčanie: vodič + asistencia ---
            float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 5f);
            float driverYaw = steerSmoothed * steerRate;
            float assistYaw = ComputeAssist(t, lateralOffset);

            float desiredYawRate = Mathf.Clamp(driverYaw + assistYaw, -maxTurnRate, maxTurnRate) * speedFactor;
            if (forwardSpeed < 0f) desiredYawRate = -desiredYawRate;

            float currentYawRate = Vector3.Dot(rb.angularVelocity, up) * Mathf.Rad2Deg;
            float yawChange = (desiredYawRate - currentYawRate) * Mathf.Clamp01(turnResponse * dt);
            rb.angularVelocity += up * (yawChange * Mathf.Deg2Rad);

            // --- Za okrajom cesty: jemné tlačenie späť ---
            float excess = Mathf.Abs(lateralOffset) - roadHalfWidth;
            if (excess > 0f)
                rb.AddForce(-trackRight * Mathf.Sign(lateralOffset) * edgePushForce * Mathf.Min(excess, 2f), ForceMode.Acceleration);
        }

        // --- Snaha zostať na kolesách ---
        rb.AddTorque(Vector3.Cross(up, Vector3.up) * uprightStrength, ForceMode.Acceleration);

        // --- Reset po prevrátení ---
        if (Vector3.Dot(up, Vector3.up) < 0.3f && velocity.magnitude < 2f)
        {
            flippedTimer += dt;
            if (flippedTimer >= flipResetDelay) ResetToTrack();
        }
        else
        {
            flippedTimer = 0f;
        }

        // --- Reset po uviaznutí ---
        if (stuckResetTime > 0f && isDriving && Mathf.Abs(forwardSpeed) < 1f)
        {
            stuckTimer += dt;
            if (stuckTimer >= stuckResetTime) ResetToTrack();
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    // =====================================================================
    //  Asistencia
    // =====================================================================

    private float ComputeAssist(float t, float lateralOffset)
    {
        EvaluateTrack(AdvanceT(t, assistLookAhead), out Vector3 aheadPoint, out Vector3 aheadTangent, out Vector3 _);
        debugAhead = aheadPoint;

        Vector3 flatTangent = Vector3.ProjectOnPlane(aheadTangent, transform.up);
        float headingError = flatTangent.sqrMagnitude > 0.0001f
            ? Vector3.SignedAngle(transform.forward, flatTangent, transform.up)
            : 0f;

        bool steering = Mathf.Abs(steerSmoothed) > 0.05f;
        float align = headingError * alignStrength * (steering ? alignWhileSteering : 1f);

        float edgeStart = roadHalfWidth * edgeAssistStart;
        float edge = 0f;
        float absOffset = Mathf.Abs(lateralOffset);
        if (absOffset > edgeStart)
        {
            float amount = Mathf.Clamp01((absOffset - edgeStart) / Mathf.Max(roadHalfWidth - edgeStart, 0.01f));
            edge = -Mathf.Sign(lateralOffset) * amount * edgeTurnStrength;
        }

        return align + edge;
    }

    // =====================================================================
    //  Nárazy
    // =====================================================================

    void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount == 0) return;

        Vector3 normal = collision.GetContact(0).normal;
        if (Vector3.Dot(normal, transform.up) > 0.6f) return;

        float impact = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, normal));
        if (impact < minImpactSpeed) return;

        Vector3 flatNormal = Vector3.ProjectOnPlane(normal, transform.up).normalized;
        rb.AddForce(flatNormal * impact * bounceFactor, ForceMode.VelocityChange);

        float strength = Mathf.Clamp01(impact / Mathf.Max(maxSpeed, 0.01f));
        SetVelocity(GetVelocity() * (1f - impactSpeedLoss * strength));
        recoveryTimer = Mathf.Max(recoveryTimer, recoveryTime * strength);

        onCrash?.Invoke(impact);
    }

    // =====================================================================
    //  Hráč
    // =====================================================================

    private void ReadPlayerInput()
    {
        steerInput = 0f;
        brakeInput = false;

#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steerInput += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steerInput -= 1f;
            brakeInput = kb.sKey.isPressed || kb.downArrowKey.isPressed || kb.spaceKey.isPressed;
            if (kb.rKey.wasPressedThisFrame) resetRequested = true;
        }
#else
        steerInput = Input.GetAxisRaw("Horizontal");
        brakeInput = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.Space);
        if (Input.GetKeyDown(KeyCode.R)) resetRequested = true;
#endif
    }

    // =====================================================================
    //  AI
    // =====================================================================

    private void UpdateAI(float dt)
    {
        UpdateLane(dt);
        UpdateMistakes(dt);

        decisionTimer -= dt;
        if (decisionTimer <= 0f)
        {
            decisionTimer = Mathf.Lerp(0.3f, 0.08f, actualSkill) * Random.Range(0.8f, 1.2f);
            DecideAI(CurrentSpeed);
        }
    }

    private void DecideAI(float speed)
    {
        float t = GetNearestT(transform.position);
        float positiveSpeed = Mathf.Max(speed, 0f);

        float lookAhead = aiLookAheadMin + positiveSpeed * aiLookAheadPerSpeed;
        EvaluateTrack(AdvanceT(t, lookAhead), out Vector3 point, out Vector3 tangent, out Vector3 up);
        Vector3 right = Vector3.Cross(up, tangent).normalized;
        debugAITarget = point + right * laneOffset;

        Vector3 toTarget = Vector3.ProjectOnPlane(debugAITarget - transform.position, transform.up);
        float angle = Vector3.SignedAngle(transform.forward, toTarget, transform.up);
        float steer = Mathf.Clamp(angle / aiFullSteerAngle, -1f, 1f);

        steer += ComputeAvoidance(positiveSpeed, out bool emergency);

        float wobble = (Mathf.PerlinNoise(noiseSeed, Time.time * 0.7f) - 0.5f) * 2f;
        steer += wobble * (1f - actualSkill) * 0.35f;
        if (mistakeTimer > 0f) steer += mistakeSteer;
        steerInput = Mathf.Clamp(steer, -1f, 1f);

        float brakeLook = (10f + positiveSpeed * 1.5f) * Mathf.Lerp(0.5f, 1f, actualSkill);
        EvaluateTrack(t, out Vector3 _, out Vector3 tangentNow, out Vector3 _);
        EvaluateTrack(AdvanceT(t, brakeLook), out Vector3 _, out Vector3 tangentLater, out Vector3 _);
        float cornerAngle = Vector3.Angle(tangentNow, tangentLater);

        float cornerSpeed = Mathf.Lerp(maxSpeed, sharpCornerSpeed, Mathf.Clamp01(cornerAngle / sharpCornerAngle));
        cornerSpeed *= 1f + (1f - actualSkill) * 0.3f;

        brakeInput = speed > cornerSpeed + 1f || emergency;
    }

    private float ComputeAvoidance(float speed, out bool emergency)
    {
        emergency = false;
        float range = detectRange + speed * detectPerSpeed;
        Vector3 origin = transform.position + transform.up * detectHeight;

        float center = CastDistance(origin, transform.forward, range, out Collider hitCollider);
        if (hitCollider == null) return 0f;

        if (hitCollider != lastSeenObstacle)
        {
            lastSeenObstacle = hitCollider;
            if (Random.value > Mathf.Lerp(0.6f, 0.98f, actualSkill))
            {
                ignoredObstacle = hitCollider;
                ignoreTimer = Random.Range(0.5f, 1.5f);
            }
        }
        if (hitCollider == ignoredObstacle && ignoreTimer > 0f) return 0f;

        Vector3 leftDir = Quaternion.AngleAxis(-25f, transform.up) * transform.forward;
        Vector3 rightDir = Quaternion.AngleAxis(25f, transform.up) * transform.forward;
        float leftFree = CastDistance(origin, leftDir, range, out _);
        float rightFree = CastDistance(origin, rightDir, range, out _);

        float side = rightFree >= leftFree ? 1f : -1f;
        float currentOffset = CurrentLateralOffset();
        float edgeLimit = roadHalfWidth * 0.8f;
        if (currentOffset > edgeLimit) side = -1f;
        else if (currentOffset < -edgeLimit) side = 1f;

        float urgency = 1f - center / range;
        emergency = urgency > 0.75f && Mathf.Max(leftFree, rightFree) < range * 0.5f;

        float maxLane = roadHalfWidth * laneRange;
        targetLaneOffset = Mathf.Clamp(currentOffset + side * 2.5f, -maxLane, maxLane);

        return side * urgency * avoidStrength;
    }

    private float CastDistance(Vector3 origin, Vector3 direction, float range, out Collider hitCollider)
    {
        hitCollider = null;
        float closest = range;

        RaycastHit[] hits = Physics.SphereCastAll(origin, detectRadius, direction, range,
            obstacleLayers, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (Vector3.Dot(hit.normal, transform.up) > 0.6f) continue;
            if (hit.distance <= 0f) continue;
            if (hit.distance < closest)
            {
                closest = hit.distance;
                hitCollider = hit.collider;
            }
        }
        return closest;
    }

    private void UpdateLane(float dt)
    {
        if (Time.time >= nextLaneChange)
        {
            nextLaneChange = Time.time + Random.Range(laneChangeInterval.x, laneChangeInterval.y);
            if (Random.value < 0.6f) targetLaneOffset = RandomLane();
        }
        laneOffset = Mathf.MoveTowards(laneOffset, targetLaneOffset, 1.5f * dt);
    }

    private void UpdateMistakes(float dt)
    {
        if (ignoreTimer > 0f) ignoreTimer -= dt;
        if (mistakeTimer > 0f) mistakeTimer -= dt;

        if (Time.time >= nextMistakeCheck)
        {
            nextMistakeCheck = Time.time + Random.Range(3f, 8f);
            if (Random.value < (1f - actualSkill) * 0.5f)
            {
                mistakeTimer = Random.Range(0.3f, 0.8f);
                mistakeSteer = Random.Range(0.4f, 0.9f) * (Random.value < 0.5f ? -1f : 1f);
            }
        }
    }

    private float CurrentLateralOffset()
    {
        float t = GetNearestT(transform.position);
        EvaluateTrack(t, out Vector3 point, out Vector3 tangent, out Vector3 up);
        Vector3 right = Vector3.Cross(up, tangent).normalized;
        return Vector3.Dot(transform.position - point, right);
    }

    private float RandomLane()
    {
        float max = roadHalfWidth * laneRange;
        return Random.Range(-max, max);
    }

    // =====================================================================
    //  Verejné funkcie
    // =====================================================================

    public void SetRaceStarted(bool started)
    {
        isDriving = started;
    }

    public void ResetToTrack()
    {
        float t = GetNearestT(rb.position);
        EvaluateTrack(t, out Vector3 point, out Vector3 tangent, out Vector3 trackUp);

        Vector3 position = point + trackUp * resetHeight;
        Quaternion rotation = Quaternion.LookRotation(tangent, trackUp);

        SetVelocity(Vector3.zero);
        rb.angularVelocity = Vector3.zero;
        rb.position = position;
        rb.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);

        steerSmoothed = 0f;
        flippedTimer = 0f;
        stuckTimer = 0f;
        recoveryTimer = 0f;
        laneOffset = targetLaneOffset = 0f;
    }

    // =====================================================================
    //  Pomocné funkcie
    // =====================================================================

    private float GetNearestT(Vector3 worldPosition)
    {
        float3 local = track.transform.InverseTransformPoint(worldPosition);
        SplineUtility.GetNearestPoint(track.Spline, local, out float3 _, out float t);
        return t;
    }

    private void EvaluateTrack(float t, out Vector3 point, out Vector3 tangent, out Vector3 up)
    {
        track.Evaluate(t, out float3 p, out float3 tan, out float3 u);
        point = p;
        tangent = ((Vector3)tan).normalized;
        up = ((Vector3)u).normalized;
        if (tangent.sqrMagnitude < 0.0001f) tangent = transform.forward;
        if (up.sqrMagnitude < 0.0001f) up = Vector3.up;
    }

    private float AdvanceT(float t, float distance)
    {
        float result = t + distance / trackLength;
        return trackClosed ? Mathf.Repeat(result, 1f) : Mathf.Min(result, 1f);
    }

    private void ApplyFrictionlessMaterial()
    {
        var material = new PhysicsMaterial("CarNoFriction")
        {
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum,
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f
        };

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (!col.isTrigger) col.sharedMaterial = material;
        }
    }

    private Vector3 GetVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    private void SetVelocity(Vector3 value)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = value;
#else
        rb.velocity = value;
#endif
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(debugNearest, 0.25f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(debugAhead, 0.3f);

        if (!IsAI) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(debugAITarget, 0.3f);

        float range = detectRange + Mathf.Max(CurrentSpeed, 0f) * detectPerSpeed;
        Vector3 origin = transform.position + transform.up * detectHeight;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, transform.forward * range);
        Gizmos.DrawRay(origin, Quaternion.AngleAxis(-25f, transform.up) * transform.forward * range);
        Gizmos.DrawRay(origin, Quaternion.AngleAxis(25f, transform.up) * transform.forward * range);
    }
}