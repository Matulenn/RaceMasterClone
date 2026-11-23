using UnityEngine;

public class RaceProgress : MonoBehaviour
{
    [Header("Race Settings")]
    [SerializeField, Min(2)]
    private int checkpointCount = 8;

    [SerializeField, Min(1)]
    private int totalLaps = 3;

    [Header("Ranking")]
    [Tooltip("Transformy checkpointov v poradí 0..checkpointCount-1.")]
    [SerializeField] private Transform[] checkpoints;

    [Header("Runtime Status - do not edit during Play")]
    [SerializeField] private bool lapTrackingStarted;
    [SerializeField] private int nextCheckpointIndex;
    [SerializeField] private int completedLaps;
    [SerializeField] private int checkpointsPassed;
    [SerializeField] private float finishTime;
    [SerializeField] private bool finished;

    private bool warnedMissingCheckpoints;

    public int NextCheckpointIndex => nextCheckpointIndex;
    public int CompletedLaps => completedLaps;
    public int TotalLaps => totalLaps;
    public int CheckpointCount => checkpointCount;
    public int CheckpointsPassed => checkpointsPassed;
    public float FinishTime => finishTime;

    public bool LapTrackingStarted => lapTrackingStarted;
    public bool Finished => finished;

    // Vyšší ProgressScore = lepšie poradie.
    // Zoraď pretekárov zostupne podľa tejto hodnoty.
    public float ProgressScore
    {
        get
        {
            // Dokončení sú vždy pred nedokončenými
            // a medzi sebou podľa času dokončenia.
            if (finished)
                return 1_000_000_000f - finishTime;

            return checkpointsPassed * 100_000f
                   - DistanceToNextCheckpoint();
        }
    }

    public event System.Action<RaceProgress> FinishedRace;

    private void Awake()
    {
        lapTrackingStarted = false;
        nextCheckpointIndex = 0;
        completedLaps = 0;
        checkpointsPassed = 0;
        finishTime = -1f;
        finished = false;
        warnedMissingCheckpoints = false;
    }

    private float DistanceToNextCheckpoint()
    {
        if (checkpoints == null ||
            nextCheckpointIndex < 0 ||
            nextCheckpointIndex >= checkpoints.Length ||
            checkpoints[nextCheckpointIndex] == null)
        {
            if (!warnedMissingCheckpoints)
            {
                warnedMissingCheckpoints = true;

                Debug.LogWarning(
                    $"{name}: Priraď checkpointy do poľa " +
                    "Checkpoints, inak poradie nebude presné.",
                    this
                );
            }

            return 0f;
        }

        return Vector3.Distance(
            transform.position,
            checkpoints[nextCheckpointIndex].position
        );
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

        // Započíta iba ďalší očakávaný checkpoint.
        if (checkpointIndex != nextCheckpointIndex)
            return;

        // Každý platný prejazd zvyšuje celkový postup.
        checkpointsPassed++;

        // Prvý prejazd štartom začne meranie kôl.
        // Ešte sa nepočíta ako dokončené kolo.
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

        // Návrat na štart po všetkých checkpointoch.
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
                finishTime = Time.time;

                Debug.Log(
                    $"{name}: Dokončil preteky!",
                    this
                );

                FinishedRace?.Invoke(this);
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