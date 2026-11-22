using System.Collections;
using TMPro;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    [Header("Racers")]
    [SerializeField] private AIPathFollower[] aiRacers;

    [Header("UI")]
    [SerializeField] private TMP_Text countdownText;

    [Header("Countdown")]
    [SerializeField, Min(1)]
    private int countdownFrom = 3;

    [SerializeField, Min(0f)]
    private float goDisplayDuration = 1f;

    [Header("Runtime Status")]
    [SerializeField] private bool raceStarted;
    [SerializeField] private bool raceFinished;
    [SerializeField] private float elapsedTime;

    private RaceProgress[] racerProgress;
    private float startTime;

    public bool RaceStarted => raceStarted;
    public bool RaceFinished => raceFinished;
    public float ElapsedTime => elapsedTime;
    public float StartTime => startTime;

    private void Awake()
    {
        raceStarted = false;
        raceFinished = false;
        elapsedTime = 0f;

        if (aiRacers == null || aiRacers.Length == 0)
        {
            Debug.LogError(
                "RaceManager: Priraď AI pretekárov.",
                this
            );

            enabled = false;
            return;
        }

        racerProgress = new RaceProgress[aiRacers.Length];

        for (int i = 0; i < aiRacers.Length; i++)
        {
            if (aiRacers[i] == null)
            {
                Debug.LogError(
                    $"RaceManager: Chýba pretekár na indexe {i}.",
                    this
                );

                enabled = false;
                return;
            }

            racerProgress[i] =
                aiRacers[i].GetComponent<RaceProgress>();

            if (racerProgress[i] == null)
            {
                Debug.LogError(
                    $"{aiRacers[i].name}: Chýba RaceProgress.",
                    aiRacers[i]
                );

                enabled = false;
                return;
            }

            aiRacers[i].SetRaceStarted(false);
        }

        if (countdownText != null)
        {
            countdownText.text = "";
        }
    }

    private IEnumerator Start()
    {
        for (int number = countdownFrom; number > 0; number--)
        {
            ShowCountdown(number.ToString());
            yield return new WaitForSeconds(1f);
        }

        startTime = Time.time;
        raceStarted = true;

        foreach (AIPathFollower racer in aiRacers)
        {
            racer.SetRaceStarted(true);
        }

        ShowCountdown("GO!");
        Debug.Log("Preteky odštartovali!", this);

        yield return new WaitForSeconds(goDisplayDuration);

        ShowCountdown("");
    }

    private void Update()
    {
        if (!raceStarted || raceFinished)
            return;

        elapsedTime = Time.time - startTime;

        bool allFinished = true;

        foreach (RaceProgress progress in racerProgress)
        {
            if (!progress.Finished)
            {
                allFinished = false;
                break;
            }
        }

        if (allFinished)
        {
            raceFinished = true;

            Debug.Log(
                $"Všetci dokončili preteky. " +
                $"Celkové trvanie: {elapsedTime:F2} s.",
                this
            );
        }
    }

    private void ShowCountdown(string message)
    {
        if (countdownText != null)
        {
            countdownText.text = message;
        }
    }
}