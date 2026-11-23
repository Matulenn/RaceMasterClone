using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RaceCheckpoint : MonoBehaviour
{
    [SerializeField, Min(0)]
    private int checkpointIndex;

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnValidate()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody racerBody = other.attachedRigidbody;

        if (racerBody == null)
            return;

        RaceProgress progress =
            racerBody.GetComponent<RaceProgress>();

        if (progress == null)
            return;

        progress.PassCheckpoint(checkpointIndex);
    }
}