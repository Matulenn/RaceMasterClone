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

    private void Awake()
    {
        if (checkpoints == null || checkpoints.Length < 2)
        {
            Debug.LogError(
                "RaceStandings: Priraď checkpointy v poradí.",
                this
            );

            enabled = false;
            return;
        }

        foreach (Transform checkpoint in checkpoints)
        {
            if (checkpoint == null)
            {
                Debug.LogError(
                    "RaceStandings: Niektorý checkpoint chýba.",
                    this
                );

                enabled = false;
                return;
            }
        }

        if (racers == null || racers.Length == 0)
        {
            Debug.LogError(
                "RaceStandings: Priraď pretekárov.",
                this
            );

            enabled = false;
            return;
        }

        foreach (RaceProgress racer in racers)
        {
            if (racer == null || orderedRacers.Contains(racer))
                continue;

            if (racer.CheckpointCount != checkpoints.Length)
            {
                Debug.LogError(
                    $"{racer.name}: Checkpoint Count sa nezhoduje " +
                    "s počtom checkpointov v RaceStandings.",
                    racer
                );

                enabled = false;
                return;
            }

            orderedRacers.Add(racer);
        }

        foreach (RaceProgress racer in orderedRacers)
        {
            racer.FinishedRace += HandleRacerFinished;
        }
    }

    private void OnDestroy()
    {
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

        // Stabilné poradie pri úplnej zhode.
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

    private float GetSegmentProgress(RaceProgress racer)
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

        float progress = Vector3.Dot(
            position - start,
            segment
        ) / segment.sqrMagnitude;

        return Mathf.Clamp01(progress);
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
}