using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class RaceStandings : MonoBehaviour
{
    [Header("Racers")]
    [SerializeField] private RaceProgress[] racers;

    [Header("Checkpoints in race order")]
    [Tooltip("Index 0 = štart/cieľ, potom 1, 2, 3...")]
    [SerializeField] private Transform[] checkpoints;

    [Header("UI")]
    [SerializeField] private TMP_Text standingsText;

    private readonly List<RaceProgress> orderedRacers =
        new List<RaceProgress>();

    private readonly Dictionary<RaceProgress, int> finishPlaces =
        new Dictionary<RaceProgress, int>();

    private readonly StringBuilder textBuilder =
        new StringBuilder();

    private bool initialized;

    private void Awake()
    {
        initialized = false;

        if (checkpoints == null || checkpoints.Length < 2)
        {
            DisableWithError(
                "Priraď checkpointy v poradí."
            );
            return;
        }

        foreach (Transform checkpoint in checkpoints)
        {
            if (checkpoint == null)
            {
                DisableWithError(
                    "Niektorý checkpoint chýba."
                );
                return;
            }
        }

        if (racers == null || racers.Length == 0)
        {
            DisableWithError(
                "Priraď pretekárov."
            );
            return;
        }

        // Najprv overíme všetkých pretekárov.
        foreach (RaceProgress racer in racers)
        {
            if (racer == null)
            {
                DisableWithError(
                    "Zoznam pretekárov obsahuje prázdnu položku."
                );
                return;
            }

            if (orderedRacers.Contains(racer))
            {
                DisableWithError(
                    $"{racer.name} je v zozname viackrát."
                );
                return;
            }

            if (racer.CheckpointCount != checkpoints.Length)
            {
                DisableWithError(
                    $"{racer.name}: Checkpoint Count sa nezhoduje " +
                    "s počtom checkpointov v RaceStandings."
                );
                return;
            }

            orderedRacers.Add(racer);
        }

        // Udalosti pripájame až po úspešnom overení.
        foreach (RaceProgress racer in orderedRacers)
        {
            racer.FinishedRace += HandleRacerFinished;
        }

        initialized = true;
    }

    private void OnDestroy()
    {
        if (!initialized)
            return;

        foreach (RaceProgress racer in orderedRacers)
        {
            if (racer != null)
            {
                racer.FinishedRace -= HandleRacerFinished;
            }
        }
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        orderedRacers.Sort(CompareRacers);
        UpdateText();
    }

    private void HandleRacerFinished(RaceProgress racer)
    {
        if (finishPlaces.ContainsKey(racer))
            return;

        int place = finishPlaces.Count + 1;
        finishPlaces.Add(racer, place);

        Debug.Log(
            $"{racer.name}: Dojazd na {place}. mieste.",
            racer
        );
    }

    private int CompareRacers(RaceProgress a, RaceProgress b)
    {
        bool aFinished = finishPlaces.TryGetValue(
            a,
            out int aPlace
        );

        bool bFinished = finishPlaces.TryGetValue(
            b,
            out int bPlace
        );

        if (aFinished && bFinished)
            return aPlace.CompareTo(bPlace);

        if (aFinished)
            return -1;

        if (bFinished)
            return 1;

        int comparison =
            b.CompletedLaps.CompareTo(a.CompletedLaps);

        if (comparison != 0)
            return comparison;

        comparison =
            b.LapTrackingStarted.CompareTo(a.LapTrackingStarted);

        if (comparison != 0)
            return comparison;

        comparison =
            GetPassedCheckpointCount(b).CompareTo(
                GetPassedCheckpointCount(a)
            );

        if (comparison != 0)
            return comparison;

        comparison =
            GetSegmentProgress(b).CompareTo(
                GetSegmentProgress(a)
            );

        if (comparison != 0)
            return comparison;

        // Pri úplnej zhode rozhoduje pôvodné poradie v poli.
        return GetConfiguredIndex(a).CompareTo(
            GetConfiguredIndex(b)
        );
    }

    private int GetPassedCheckpointCount(RaceProgress racer)
    {
        if (!racer.LapTrackingStarted)
            return 0;

        int next = racer.NextCheckpointIndex;

        return next == 0
            ? checkpoints.Length - 1
            : next - 1;
    }

    private float GetRawSegmentProgress(RaceProgress racer)
    {
        int next = racer.NextCheckpointIndex;

        int previous =
            (next - 1 + checkpoints.Length) % checkpoints.Length;

        Vector3 start = checkpoints[previous].position;
        Vector3 end = checkpoints[next].position;
        Vector3 position = racer.transform.position;

        start.y = 0f;
        end.y = 0f;
        position.y = 0f;

        Vector3 segment = end - start;

        if (segment.sqrMagnitude < 0.0001f)
            return 0f;

        return Vector3.Dot(
            position - start,
            segment
        ) / segment.sqrMagnitude;
    }

    private float GetSegmentProgress(RaceProgress racer)
    {
        return GetRawSegmentProgress(racer);
    }

    private int GetConfiguredIndex(RaceProgress racer)
    {
        return System.Array.IndexOf(racers, racer);
    }

    private void UpdateText()
    {
        if (standingsText == null)
            return;

        textBuilder.Clear();

        for (int i = 0; i < orderedRacers.Count; i++)
        {
            RaceProgress racer = orderedRacers[i];

            textBuilder.Append(i + 1);
            textBuilder.Append(". ");
            textBuilder.Append(racer.name);

            if (finishPlaces.ContainsKey(racer))
            {
                textBuilder.Append(" | CIEL");
            }
            else if (!racer.LapTrackingStarted)
            {
                textBuilder.Append(" | START");
            }
            else
            {
                int currentLap = Mathf.Min(
                    racer.CompletedLaps + 1,
                    racer.TotalLaps
                );

                textBuilder.Append(" | Kolo ");
                textBuilder.Append(currentLap);
                textBuilder.Append("/");
                textBuilder.Append(racer.TotalLaps);
            }

            textBuilder.AppendLine();
        }

        standingsText.text = textBuilder.ToString();
    }

    [ContextMenu("Debug Standings")]
    private void DebugStandings()
    {
        if (!Application.isPlaying || !initialized)
        {
            Debug.LogWarning(
                "RaceStandings: Diagnostiku spusti počas Play " +
                "po úspešnej inicializácii.",
                this
            );
            return;
        }

        // Zoradíme aj pri ručnom spustení počas pauzy.
        orderedRacers.Sort(CompareRacers);

        StringBuilder debugBuilder = new StringBuilder();

        debugBuilder.AppendLine(
            "========== DEBUG STANDINGS =========="
        );

        for (int i = 0; i < orderedRacers.Count; i++)
        {
            RaceProgress racer = orderedRacers[i];

            int next = racer.NextCheckpointIndex;

            int previous =
                (next - 1 + checkpoints.Length) % checkpoints.Length;

            float rawProgress = GetRawSegmentProgress(racer);
            float clampedProgress = Mathf.Clamp01(rawProgress);

            string finishPlace =
                finishPlaces.TryGetValue(racer, out int place)
                    ? place.ToString()
                    : "-";

            debugBuilder.AppendLine(
                $"{i + 1}. {racer.name} | " +
                $"ConfiguredIndex={GetConfiguredIndex(racer)} | " +
                $"Laps={racer.CompletedLaps} | " +
                $"Started={racer.LapTrackingStarted} | " +
                $"PreviousCP={previous} | " +
                $"NextCP={next} | " +
                $"PassedInLap={GetPassedCheckpointCount(racer)} | " +
                $"RawProgress={rawProgress:F6} | " +
                $"ClampedProgress={clampedProgress:F6} | " +
                $"Finished={racer.Finished} | " +
                $"FinishPlace={finishPlace}"
            );

            debugBuilder.AppendLine(
                $"    Position={racer.transform.position.ToString("F3")} | " +
                $"Segment={checkpoints[previous].name}" +
                $" -> {checkpoints[next].name}"
            );
        }

        debugBuilder.AppendLine(
            "====================================="
        );

        Debug.Log(debugBuilder.ToString(), this);
    }

    private void DisableWithError(string message)
    {
        Debug.LogError(
            $"RaceStandings: {message}",
            this
        );

        enabled = false;
    }
}