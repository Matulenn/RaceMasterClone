using System.Collections;
using TMPro;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    [Header("All Racers - player and AI")]
    [SerializeField] private RaceProgress[] racers;

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

    private AIPathFollower[] aiControllers;
    private PlayerRaceController[] playerControllers;
    private CarController[] carControllers;

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

        if (racers == null || racers.Length == 0)
        {
            DisableWithError("Priraď pretekárov.");
            return;
        }

        aiControllers = new AIPathFollower[racers.Length];
        playerControllers = new PlayerRaceController[racers.Length];
        carControllers = new CarController[racers.Length];

        for (int i = 0; i < racers.Length; i++)
        {
            RaceProgress racer = racers[i];

            if (racer == null)
            {
                DisableWithError($"Chýba pretekár na indexe {i}.");
                return;
            }

            for (int j = 0; j < i; j++)
            {
                if (racers[j] == racer)
                {
                    DisableWithError($"{racer.name} je v zozname viackrát.");
                    return;
                }
            }

            aiControllers[i] = racer.GetComponent<AIPathFollower>();
            playerControllers[i] = racer.GetComponent<PlayerRaceController>();
            carControllers[i] = racer.GetComponent<CarController>();

            bool hasCarController = carControllers[i] != null;
            bool hasAI = aiControllers[i] != null;
            bool hasPlayer = playerControllers[i] != null;

            if (!hasCarController && hasAI == hasPlayer)
            {
                DisableWithError(
                    $"{racer.name} musí mať CarController, AIPathFollower alebo PlayerRaceController."
                );
                return;
            }
        }

        SetControllersStarted(false);

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

        SetControllersStarted(true);

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

        foreach (RaceProgress racer in racers)
        {
            if (!racer.Finished)
                return;
        }

        raceFinished = true;

        Debug.Log(
            $"Všetci dokončili preteky. Celkové trvanie: {elapsedTime:F2} s.",
            this
        );
    }

    private void SetControllersStarted(bool started)
    {
        for (int i = 0; i < racers.Length; i++)
        {
            if (carControllers[i] != null)
            {
                carControllers[i].SetRaceStarted(started);
            }

            if (aiControllers[i] != null)
            {
                aiControllers[i].SetRaceStarted(started);
            }

            if (playerControllers[i] != null)
            {
                playerControllers[i].SetRaceStarted(started);
            }
        }
    }

    private void ShowCountdown(string message)
    {
        if (countdownText != null)
        {
            countdownText.text = message;
        }
    }

    private void DisableWithError(string message)
    {
        Debug.LogError($"RaceManager: {message}", this);
        enabled = false;
    }
}