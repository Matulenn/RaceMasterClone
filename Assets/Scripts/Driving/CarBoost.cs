using UnityEngine;
// Ak sa váš skript na ovládanie auta volá inak (napr. playerController),
// zmeòte názov v nasledujúcom riadku:
using Car = CarController;

// Doèasný boost auta: vyššia rýchlos, rýchlejšie zrých¾ovanie,
// lepšia pri¾navos a silnejšia asistencia držania cesty.
// Tento skript netreba pridáva ruène, BoostPickup ho pridá na auto sám.
public class CarBoost : MonoBehaviour
{
    // Pre UI (napr. ukazovate¾ boostu)
    public bool IsBoosting => timer > 0f;
    public float RemainingTime => Mathf.Max(timer, 0f);
    public float Duration { get; private set; }

    private Car car;
    private Rigidbody rb;
    private float timer;

    // Pôvodné hodnoty auta, ktoré sa po skonèení boostu vrátia
    private float originalSpeedMultiplier;
    private float originalAcceleration;
    private float originalSideGrip;
    private float originalAlignStrength;
    private float originalEdgeTurnStrength;

    void Awake()
    {
        car = GetComponent<Car>();
        rb = GetComponent<Rigidbody>();
    }

    public void Activate(float duration, float speedMultiplier, float accelerationMultiplier,
                         float gripMultiplier, float assistMultiplier, float instantPush)
    {
        if (car == null) return;

        // Pri prvom zobratí uložíme pôvodné hodnoty a zapneme boost.
        // Ïalší pickup poèas boostu len predåži jeho trvanie (boosty sa nenásobia).
        if (!IsBoosting)
        {
            originalSpeedMultiplier = car.speedMultiplier;
            originalAcceleration = car.acceleration;
            originalSideGrip = car.sideGrip;
            originalAlignStrength = car.alignStrength;
            originalEdgeTurnStrength = car.edgeTurnStrength;

            car.speedMultiplier *= speedMultiplier;
            car.acceleration *= accelerationMultiplier;
            car.sideGrip *= gripMultiplier;
            car.alignStrength *= assistMultiplier;
            car.edgeTurnStrength *= assistMultiplier;
        }

        timer = Mathf.Max(timer, duration);
        Duration = duration;

        // Okamžité „kopnutie“ dopredu
        if (instantPush > 0f && rb != null)
            rb.AddForce(transform.forward * instantPush, ForceMode.VelocityChange);
    }

    void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) Restore();
        }
    }

    private void Restore()
    {
        timer = 0f;
        car.speedMultiplier = originalSpeedMultiplier;
        car.acceleration = originalAcceleration;
        car.sideGrip = originalSideGrip;
        car.alignStrength = originalAlignStrength;
        car.edgeTurnStrength = originalEdgeTurnStrength;
        // Auto po booste spomalí plynulo pod¾a nastavenia Deceleration.
    }

    void OnDisable()
    {
        if (IsBoosting) Restore();
    }
}