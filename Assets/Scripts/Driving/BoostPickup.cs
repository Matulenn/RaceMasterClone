using UnityEngine;
// Ak sa váš skript na ovládanie auta volá inak (napr. playerController),
// zmeòte názov v nasledujúcom riadku:
using Car = CarController;

// Pickup, ktorı autu (hráèovi aj súperovi) dá doèasnı boost:
// vyššiu rıchlos a lepšie dranie cesty.
// Skript pridajte na objekt pickupu, ktorı má Collider.
[RequireComponent(typeof(Collider))]
public class BoostPickup : MonoBehaviour
{
    [Header("Efekt boostu")]
    [Tooltip("Ako dlho boost trvá (s).")]
    public float duration = 3f;
    [Tooltip("Násobok max. rıchlosti (1.4 = o 40 % rıchlejšie).")]
    public float speedMultiplier = 1.4f;
    [Tooltip("Násobok zrıchlenia, aby auto vyššiu rıchlos dosiahlo rıchlo.")]
    public float accelerationMultiplier = 2f;
    [Tooltip("Násobok pri¾navosti pneumatík (menej šmyku).")]
    public float gripMultiplier = 1.5f;
    [Tooltip("Násobok asistencie drania cesty.")]
    public float assistMultiplier = 1.5f;
    [Tooltip("Okamité „kopnutie“ dopredu (m/s). 0 = vypnuté.")]
    public float instantPush = 4f;

    [Header("Opätovné objavenie")]
    [Tooltip("Za ko¾ko sekúnd sa pickup znova objaví. 0 = u nikdy.")]
    public float respawnTime = 5f;

    [Header("Vzh¾ad")]
    [Tooltip("Èas, ktorá sa toèí a vznáša (napr. model). Prázdne = celı objekt.")]
    public Transform visual;
    public float rotateSpeed = 90f;
    public float bobHeight = 0.25f;
    public float bobSpeed = 2f;
    [Tooltip("Efekt pri zobratí (napr. Particle System prefab). Nepovinné.")]
    public GameObject pickupEffect;
    [Tooltip("Zvuk pri zobratí. Nepovinné.")]
    public AudioClip pickupSound;

    private Collider triggerCollider;
    private Renderer[] renderers;
    private bool available = true;
    private float respawnAt;
    private Vector3 visualStartPosition;

    // Pri pridaní skriptu v editore sa collider rovno prepne na trigger.
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
        renderers = GetComponentsInChildren<Renderer>();
        if (visual == null) visual = transform;
        visualStartPosition = visual.localPosition;
    }

    void Update()
    {
        // Otáèanie a vznášanie
        visual.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        Vector3 position = visualStartPosition;
        position.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        visual.localPosition = position;

        // Opätovné objavenie
        if (!available && respawnTime > 0f && Time.time >= respawnAt)
            SetAvailable(true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!available) return;

        Car car = other.GetComponentInParent<Car>();
        if (car == null) return;   // nie je to auto

        CarBoost boost = car.GetComponent<CarBoost>();
        if (boost == null) boost = car.gameObject.AddComponent<CarBoost>();

        boost.Activate(duration, speedMultiplier, accelerationMultiplier,
                       gripMultiplier, assistMultiplier, instantPush);

        if (pickupEffect != null)
            Destroy(Instantiate(pickupEffect, transform.position, Quaternion.identity), 3f);
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        SetAvailable(false);
        respawnAt = Time.time + respawnTime;
    }

    private void SetAvailable(bool value)
    {
        available = value;
        triggerCollider.enabled = value;
        foreach (Renderer r in renderers) r.enabled = value;
    }
}