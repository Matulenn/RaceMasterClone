using UnityEngine;

public class RaceProgress : MonoBehaviour
{
    [Header("Race Settings")]
    [SerializeField, Min(2)]
    private int checkpointCount = 8;

    [SerializeField, Min(1)]
    private int totalLaps = 3;

    [Header("Runtime Status - do not edit during Play")]
    [SerializeField] private bool lapTrackingStarted;
    [SerializeField] private int nextCheckpointIndex;
    [SerializeField] private int completedLaps;
    [SerializeField] private bool finished;

    public int NextCheckpointIndex => nextCheckpointIndex;
    public int CompletedLaps => completedLaps;
    public int TotalLaps => totalLaps;
    public bool Finished => finished;

    private void Awake()
    {
        lapTrackingStarted = false;
        nextCheckpointIndex = 0;
        completedLaps = 0;
        finished = false;
    }

    public void PassCheckpoint(int checkpointIndex)
    {
        if (finished)
            return;

        if (checkpointIndex < 0 ||
            checkpointIndex >= checkpointCount)
        {
            return;
        }

        // Iný než očakávaný checkpoint sa nezapočíta.
        if (checkpointIndex != nextCheckpointIndex)
            return;

        // Prvý prejazd štartom ešte nie je dokončené kolo.
        if (!lapTrackingStarted)
        {
            lapTrackingStarted = true;
            nextCheckpointIndex = 1;

            Debug.Log(
                $"{name}: Začiatok merania kôl.",
                this
            );

            return;
        }

        if (checkpointIndex == 0)
        {
            completedLaps++;

            Debug.Log(
                $"{name}: Dokončené kolo " +
                $"{completedLaps}/{totalLaps}.",
                this
            );

            if (completedLaps >= totalLaps)
            {
                finished = true;

                Debug.Log(
                    $"{name}: Dokončil preteky!",
                    this
                );

                return;
            }
        }
        else
        {
            Debug.Log(
                $"{name}: Checkpoint {checkpointIndex}.",
                this
            );
        }

        nextCheckpointIndex =
            (checkpointIndex + 1) % checkpointCount;
    }
}