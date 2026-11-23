using UnityEngine;

// Otáča kolesá auta podľa jeho skutočnej rýchlosti.
// Skript pridajte na hlavný objekt auta (rodiča kolies).
public class WheelSpin : MonoBehaviour
{
    [Tooltip("Sem pretiahnite všetky 4 kolesá (objekty kolies).")]
    public Transform[] wheels;

    [Tooltip("Polomer kolesa v metroch (pol výšky kolesa).")]
    public float wheelRadius = 0.35f;

    [Tooltip("Os, okolo ktorej sa koleso točí (v lokálnych súradniciach kolesa). Zvyčajne X.")]
    public Vector3 spinAxis = Vector3.right;

    [Tooltip("Zapnite, ak sa kolesá točia opačným smerom.")]
    public bool invertDirection = false;

    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void LateUpdate()
    {
        if (Time.deltaTime <= 0f) return;

        // Rýchlosť auta dopredu (záporná pri cúvaní).
        // Počíta sa z posunu pozície, takže funguje s Rigidbody aj bez neho.
        Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
        float forwardSpeed = Vector3.Dot(velocity, transform.forward);

        // Uhlová rýchlosť kolesa v stupňoch za sekundu.
        float degreesPerSecond = forwardSpeed / wheelRadius * Mathf.Rad2Deg;
        if (invertDirection) degreesPerSecond = -degreesPerSecond;

        float angle = degreesPerSecond * Time.deltaTime;

        foreach (Transform wheel in wheels)
        {
            if (wheel != null)
                wheel.Rotate(spinAxis, angle, Space.Self);
        }
    }
}
