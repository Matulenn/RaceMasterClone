using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

[Serializable]
public class RacerResultData
{
    public string racerId;
    public string racerName;

    // Nula znamená, že pretekár ešte nedokončil.
    public int finishPosition;

    public float finishTimeSeconds;
    public bool finished;
}

[Serializable]
public class RaceResultData
{
    public bool complete;

    public List<RacerResultData> racers =
        new List<RacerResultData>();
}

public class RaceResults : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RaceManager raceManager;
    [SerializeField] private RaceProgress[] racers;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text resultsText;

    [Header("Runtime Results")]
    [SerializeField] private RaceResultData resultData =
        new RaceResultData();

    private readonly Dictionary<RaceProgress, RacerResultData>
        resultsByRacer =
            new Dictionary<RaceProgress, RacerResultData>();

    private readonly List<RaceProgress> subscribedRacers =
        new List<RaceProgress>();

    private readonly StringBuilder textBuilder =
        new StringBuilder();

    private int finishedCount;

    // Vyvolá sa po dokončení všetkých priradených pretekárov.
    public event Action<RaceResultData> ResultsReady;

    public bool Complete => resultData.complete;

    private void Awake()
    {
        resultData = new RaceResultData();
        finishedCount = 0;

        if (raceManager == null)
        {
            Debug.LogError(
                "RaceResults: Priraď RaceManager.",
                this
            );

            enabled = false;
            return;
        }

        if (racers == null || racers.Length == 0)
        {
            Debug.LogError(
                "RaceResults: Priraď pretekárov.",
                this
            );

            enabled = false;
            return;
        }

        // Najprv over celý zoznam.
        HashSet<RaceProgress> uniqueRacers =
            new HashSet<RaceProgress>();

        foreach (RaceProgress racer in racers)
        {
            if (racer == null || !uniqueRacers.Add(racer))
            {
                Debug.LogError(
                    "RaceResults: Zoznam obsahuje prázdnu " +
                    "alebo duplicitnú položku.",
                    this
                );

                enabled = false;
                return;
            }
        }

        for (int i = 0; i < racers.Length; i++)
        {
            RaceProgress racer = racers[i];

            RacerResultData result = new RacerResultData
            {
                racerId = $"racer_{i + 1}",
                racerName = racer.name,
                finishPosition = 0,
                finishTimeSeconds = 0f,
                finished = false
            };

            resultData.racers.Add(result);
            resultsByRacer.Add(racer, result);

            racer.FinishedRace += HandleRacerFinished;
            subscribedRacers.Add(racer);
        }

        if (resultsText != null)
        {
            resultsText.text = "";
        }
    }

    private void OnDestroy()
    {
        foreach (RaceProgress racer in subscribedRacers)
        {
            if (racer != null)
            {
                racer.FinishedRace -= HandleRacerFinished;
            }
        }
    }

    private void HandleRacerFinished(RaceProgress racer)
    {
        if (!raceManager.RaceStarted)
            return;

        if (!resultsByRacer.TryGetValue(
                racer,
                out RacerResultData result))
        {
            return;
        }

        if (result.finished)
            return;

        finishedCount++;

        result.finished = true;
        result.finishPosition = finishedCount;

        result.finishTimeSeconds = Mathf.Max(
            0f,
            Time.time - raceManager.StartTime
        );

        // Dokončení sú zoradení podľa dojazdu,
        // nedokončení zostávajú na konci.
        resultData.racers.Sort(CompareResults);

        resultData.complete =
            finishedCount == resultsByRacer.Count;

        Debug.Log(
            $"{result.finishPosition}. " +
            $"{result.racerName} | " +
            $"{FormatTime(result.finishTimeSeconds)}",
            racer
        );

        UpdateResultsText();

        if (resultData.complete)
        {
            Debug.Log(
                "Výsledky všetkých pretekárov sú pripravené.",
                this
            );

            ResultsReady?.Invoke(GetResultsSnapshot());
        }
    }

    private static int CompareResults(
        RacerResultData a,
        RacerResultData b)
    {
        if (a.finished && b.finished)
        {
            return a.finishPosition.CompareTo(
                b.finishPosition
            );
        }

        if (a.finished)
            return -1;

        if (b.finished)
            return 1;

        return string.CompareOrdinal(a.racerId, b.racerId);
    }

    private void UpdateResultsText()
    {
        if (resultsText == null)
            return;

        textBuilder.Clear();

        textBuilder.AppendLine(
            resultData.complete
                ? "VYSLEDKY"
                : "PRIEBEZNE DOJAZDY"
        );

        foreach (RacerResultData result in resultData.racers)
        {
            if (result.finished)
            {
                textBuilder.Append(result.finishPosition);
                textBuilder.Append(". ");
                textBuilder.Append(result.racerName);
                textBuilder.Append(" | ");
                textBuilder.Append(
                    FormatTime(result.finishTimeSeconds)
                );
            }
            else
            {
                textBuilder.Append("- ");
                textBuilder.Append(result.racerName);
                textBuilder.Append(" | na trati");
            }

            textBuilder.AppendLine();
        }

        resultsText.text = textBuilder.ToString();
    }

    public RaceResultData GetResultsSnapshot()
    {
        RaceResultData snapshot = new RaceResultData
        {
            complete = resultData.complete
        };

        foreach (RacerResultData result in resultData.racers)
        {
            snapshot.racers.Add(new RacerResultData
            {
                racerId = result.racerId,
                racerName = result.racerName,
                finishPosition = result.finishPosition,
                finishTimeSeconds = result.finishTimeSeconds,
                finished = result.finished
            });
        }

        return snapshot;
    }

    public string GetResultsJson()
    {
        return JsonUtility.ToJson(
            GetResultsSnapshot(),
            true
        );
    }

    [ContextMenu("Print Results JSON")]
    private void PrintResultsJson()
    {
        Debug.Log(GetResultsJson(), this);
    }

    public static string FormatTime(float seconds)
    {
        int totalMilliseconds =
            Mathf.FloorToInt(Mathf.Max(0f, seconds) * 1000f);

        int minutes = totalMilliseconds / 60000;
        int remainingSeconds =
            (totalMilliseconds / 1000) % 60;
        int milliseconds = totalMilliseconds % 1000;

        return $"{minutes:00}:" +
               $"{remainingSeconds:00}." +
               $"{milliseconds:000}";
    }
}