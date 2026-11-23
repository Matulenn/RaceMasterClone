using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class RaceUIController : MonoBehaviour
{
    [Header("Panely")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject resultsPanel;

    [Header("HUD Prvky")]
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private float maxDisplaySpeed = 220f;
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text countdownText;

    [Header("Speed Gauge Farby")]
    [SerializeField] private Image speedFillImage;
    [SerializeField] private Color lowSpeedColor = new Color(0.18f, 0.85f, 0.35f, 1f);
    [SerializeField] private Color midSpeedColor = new Color(1f, 0.78f, 0.1f, 1f);
    [SerializeField] private Color highSpeedColor = new Color(0.95f, 0.2f, 0.2f, 1f);

    [Header("Výsledková obrazovka")]
    [SerializeField] private TMP_Text resultsTitleText;
    [SerializeField] private TMP_Text finalPositionText;
    [SerializeField] private TMP_Text finalTimeText;
    [SerializeField] private TMP_Text newBestTimeText;
    [SerializeField] private TMP_Text earnedCoinsText;
    [SerializeField] private TMP_Text cloudStatusText;

    [Header("Odmeny za umiestnenie (Mince)")]
    [SerializeField] private int firstPlaceCoins = 150;
    [SerializeField] private int secondPlaceCoins = 100;
    [SerializeField] private int thirdPlaceCoins = 60;
    [SerializeField] private int participationCoins = 30;

    [Header("Štartovacie odpočítavanie")]
    [SerializeField] private bool playCountdownOnStart = true;

    [Header("Zvukové efekty odpočítavania")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip countdownBeepSound;
    [SerializeField] private AudioClip countdownGoSound;

    [Header("Hudba vo výsledkoch (Finish)")]
    [SerializeField] private AudioSource finishMusicSource;
    [SerializeField] private AudioClip finishMusic;
    [SerializeField] private bool loopFinishMusic = true;
    [SerializeField] private AudioSource raceMusicToStop;

    private float raceTime;
    private bool isRaceStarted;
    private bool isRaceActive = true;
    private bool isPaused;

    private RaceManager raceManager;
    private RaceResults raceResults;
    private RaceStandings raceStandings;
    private TMP_Text externalStandingsText;
    private CarController playerCarController;
    private PlayerRaceController playerController;
    private RaceProgress playerProgress;
    private Rigidbody playerRigidbody;
    private int totalRacersCount = 4;
    private string lastSyncedCountdown = "";
    private Coroutine countdownAnimCoroutine;

    public bool CanRace => isRaceStarted && isRaceActive && !isPaused;

#if UNITY_EDITOR
    private float debugSpeed;
    private bool hasExternalSpeedInput;
#endif

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.volume = PlayerPrefs.GetFloat("GameVolume", 1f);

        if (raceMusicToStop != null)
        {
            raceMusicToStop.volume *= PlayerPrefs.GetFloat("MusicVolume", 1f);
        }

        if (hudPanel != null) hudPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (newBestTimeText != null) newBestTimeText.gameObject.SetActive(false);

        if (speedFillImage == null && speedSlider != null && speedSlider.fillRect != null)
        {
            speedFillImage = speedSlider.fillRect.GetComponent<Image>();
        }

        ApplySpeedVisuals(0f);
        UpdateTimerUI(0f);

        ConnectToRaceSystems();

        if (raceManager != null && raceManager.enabled)
        {
            isRaceStarted = raceManager.RaceStarted;
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(!isRaceStarted);
            }
        }
        else if (playCountdownOnStart && countdownText != null)
        {
            StartCoroutine(CountdownRoutine());
        }
        else
        {
            isRaceStarted = true;
            if (countdownText != null) countdownText.gameObject.SetActive(false);
        }
    }

    private void ConnectToRaceSystems()
    {
        raceManager = FindFirstObjectByType<RaceManager>();
        raceResults = FindFirstObjectByType<RaceResults>();
        raceStandings = FindFirstObjectByType<RaceStandings>();

        if (raceStandings != null)
        {
            FieldInfo standingsField = typeof(RaceStandings).GetField("standingsText", BindingFlags.NonPublic | BindingFlags.Instance);
            if (standingsField != null)
            {
                externalStandingsText = standingsField.GetValue(raceStandings) as TMP_Text;
            }
        }

        if (raceResults != null)
        {
            RaceResultData snapshot = raceResults.GetResultsSnapshot();
            if (snapshot != null && snapshot.racers != null && snapshot.racers.Count > 0)
            {
                totalRacersCount = snapshot.racers.Count;
            }
        }

        GameObject playerObj = null;

        CarController[] allCars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
        for (int i = 0; i < allCars.Length; i++)
        {
            if (!allCars[i].IsAI)
            {
                playerCarController = allCars[i];
                playerObj = allCars[i].gameObject;
                break;
            }
        }

        if (playerObj == null)
        {
            playerController = FindFirstObjectByType<PlayerRaceController>();
            if (playerController != null)
            {
                playerObj = playerController.gameObject;
            }
        }

        if (playerObj != null)
        {
            playerRigidbody = playerObj.GetComponent<Rigidbody>();
            playerProgress = playerObj.GetComponent<RaceProgress>();
            if (playerProgress != null)
            {
                playerProgress.FinishedRace += OnPlayerFinishedRace;
            }
        }
    }

    private void OnDestroy()
    {
        if (playerProgress != null)
        {
            playerProgress.FinishedRace -= OnPlayerFinishedRace;
        }
    }

    private void Update()
    {
        if (raceManager != null && raceManager.enabled && isRaceActive)
        {
            SyncWithRaceManager();
        }

        if (CanRace)
        {
            if (raceManager != null && raceManager.enabled)
            {
                raceTime = raceManager.ElapsedTime;
            }
            else
            {
                raceTime += Time.deltaTime;
            }

            UpdateTimerUI(raceTime);
            UpdatePlayerTelemetry();

#if UNITY_EDITOR
            HandleEditorPreviewControls();
#endif
        }

        if (isRaceActive && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                TogglePause();
            }
        }
    }

    private void LateUpdate()
    {
        HighlightPlayerRow(positionText, false);

        if (resultsPanel != null && resultsPanel.activeSelf)
        {
            HighlightPlayerRow(finalPositionText, true);
        }
    }

    private void HighlightPlayerRow(TMP_Text targetText, bool replaceHeader)
    {
        if (targetText == null || string.IsNullOrEmpty(targetText.text)) return;

        string content = targetText.text;

        if (replaceHeader)
        {
            content = content.Replace("PRIEBEZNE DOJAZDY", "PORADIE V CIELI")
                             .Replace("VYSLEDKY", "PORADIE V CIELI");
        }

        string playerName = playerProgress != null ? playerProgress.name : "Player";

        if (!content.Contains("<color=#FFD54F>"))
        {
            string[] lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(playerName))
                {
                    lines[i] = $"<color=#FFD54F><b>{lines[i]}</b></color>";
                }
            }
            content = string.Join("\n", lines);
        }

        if (targetText.text != content)
        {
            targetText.text = content;
        }
    }

    private void SyncWithRaceManager()
    {
        isRaceStarted = raceManager.RaceStarted;

        if (countdownText == null) return;

        string currentLabel = countdownText.text;
        if (currentLabel == lastSyncedCountdown) return;

        lastSyncedCountdown = currentLabel;

        if (string.IsNullOrEmpty(currentLabel))
        {
            countdownText.transform.localScale = Vector3.one;
            countdownText.gameObject.SetActive(false);
            return;
        }

        countdownText.gameObject.SetActive(true);
        bool isGo = currentLabel.Equals("GO!", StringComparison.OrdinalIgnoreCase);

        Color stepColor = new Color(0.2f, 1f, 0.45f, 1f);
        if (!isGo)
        {
            if (currentLabel == "3") stepColor = new Color(0.95f, 0.25f, 0.25f, 1f);
            else if (currentLabel == "2") stepColor = new Color(1f, 0.78f, 0.1f, 1f);
            else stepColor = new Color(0.25f, 0.88f, 0.4f, 1f);
        }

        PlayCountdownSound(isGo);

        if (countdownAnimCoroutine != null)
        {
            StopCoroutine(countdownAnimCoroutine);
        }
        countdownAnimCoroutine = StartCoroutine(AnimateCountdownStep(currentLabel, stepColor, isGo ? 0.65f : 0.8f));
    }

    private void UpdatePlayerTelemetry()
    {
        if (playerCarController != null)
        {
            float speedKmh = Mathf.Max(0f, playerCarController.CurrentSpeed) * 3.6f;
            UpdateSpeed(speedKmh);
        }
        else if (playerRigidbody != null)
        {
            float speedKmh = playerRigidbody.linearVelocity.magnitude * 3.6f;
            UpdateSpeed(speedKmh);
        }

        if (externalStandingsText != null && externalStandingsText != positionText && playerProgress != null)
        {
            string rawStandings = externalStandingsText.text;
            if (!string.IsNullOrEmpty(rawStandings))
            {
                string[] lines = rawStandings.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(playerProgress.name))
                    {
                        int dotIndex = lines[i].IndexOf('.');
                        if (dotIndex > 0 && int.TryParse(lines[i].Substring(0, dotIndex).Trim(), out int place))
                        {
                            UpdatePosition(place, totalRacersCount);
                        }
                        break;
                    }
                }
            }
        }
    }

    private void OnPlayerFinishedRace(RaceProgress racer)
    {
        if (!isRaceActive) return;

        int finalPlace = 1;
        float finalFinishTime = (raceManager != null && raceManager.enabled) ? raceManager.ElapsedTime : raceTime;

        if (raceResults != null)
        {
            RaceResultData snapshot = raceResults.GetResultsSnapshot();
            if (snapshot != null && snapshot.racers != null)
            {
                totalRacersCount = snapshot.racers.Count;
                for (int i = 0; i < snapshot.racers.Count; i++)
                {
                    if (snapshot.racers[i].racerName == racer.name && snapshot.racers[i].finished)
                    {
                        finalPlace = snapshot.racers[i].finishPosition;
                        finalFinishTime = snapshot.racers[i].finishTimeSeconds;
                        break;
                    }
                }
            }
        }

        raceTime = finalFinishTime;
        UpdateTimerUI(raceTime);

        int rewardCoins = GetCoinsForPlace(finalPlace);
        ShowResults(finalPlace, totalRacersCount, rewardCoins);
    }

    private int GetCoinsForPlace(int place)
    {
        switch (place)
        {
            case 1: return firstPlaceCoins;
            case 2: return secondPlaceCoins;
            case 3: return thirdPlaceCoins;
            default: return participationCoins;
        }
    }

    private IEnumerator CountdownRoutine()
    {
        isRaceStarted = false;
        countdownText.gameObject.SetActive(true);

        string[] steps = { "3", "2", "1" };
        Color[] stepColors =
        {
            new Color(0.95f, 0.25f, 0.25f, 1f),
            new Color(1f, 0.78f, 0.1f, 1f),
            new Color(0.25f, 0.88f, 0.4f, 1f)
        };

        for (int i = 0; i < steps.Length; i++)
        {
            while (isPaused) yield return null;
            PlayCountdownSound(false);
            yield return AnimateCountdownStep(steps[i], stepColors[i], 0.8f);
        }

        while (isPaused) yield return null;
        isRaceStarted = true;
        PlayCountdownSound(true);
        yield return AnimateCountdownStep("GO!", new Color(0.2f, 1f, 0.45f, 1f), 0.65f);

        if (sfxSource != null) sfxSource.pitch = 1f;
        countdownText.transform.localScale = Vector3.one;
        countdownText.gameObject.SetActive(false);
    }

    private void PlayCountdownSound(bool isGo)
    {
        if (countdownBeepSound == null && countdownGoSound == null) return;

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }

        if (isGo)
        {
            if (countdownGoSound != null)
            {
                sfxSource.pitch = 1f;
                sfxSource.PlayOneShot(countdownGoSound);
            }
            else if (countdownBeepSound != null)
            {
                sfxSource.pitch = 1.35f;
                sfxSource.PlayOneShot(countdownBeepSound);
            }
        }
        else if (countdownBeepSound != null)
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(countdownBeepSound);
        }
    }

    private IEnumerator AnimateCountdownStep(string label, Color textColor, float duration)
    {
        countdownText.text = label;
        countdownText.color = textColor;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!isPaused)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = Mathf.Lerp(1.65f, 1.0f, 1f - Mathf.Pow(1f - t, 3f));
                countdownText.transform.localScale = Vector3.one * scale;
            }
            yield return null;
        }
    }

#if UNITY_EDITOR
    private void HandleEditorPreviewControls()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f8Key.wasPressedThisFrame)
        {
            ShowResults(1, totalRacersCount, firstPlaceCoins);
            return;
        }

        if (!hasExternalSpeedInput)
        {
            if (Keyboard.current.upArrowKey.isPressed)
            {
                debugSpeed = Mathf.MoveTowards(debugSpeed, maxDisplaySpeed, 110f * Time.deltaTime);
                ApplySpeedVisuals(debugSpeed);
            }
            else if (debugSpeed > 0f)
            {
                float brakeRate = Keyboard.current.downArrowKey.isPressed ? 180f : 75f;
                debugSpeed = Mathf.MoveTowards(debugSpeed, 0f, brakeRate * Time.deltaTime);
                ApplySpeedVisuals(debugSpeed);
            }
        }
    }
#endif

    public void UpdateSpeed(float speedKmh)
    {
#if UNITY_EDITOR
        hasExternalSpeedInput = true;
#endif
        ApplySpeedVisuals(speedKmh);
    }

    private void ApplySpeedVisuals(float speedKmh)
    {
        float clampedSpeed = Mathf.Max(0f, speedKmh);
        float normalized = Mathf.Clamp01(clampedSpeed / Mathf.Max(1f, maxDisplaySpeed));

        Color gaugeColor = normalized < 0.5f
            ? Color.Lerp(lowSpeedColor, midSpeedColor, normalized * 2f)
            : Color.Lerp(midSpeedColor, highSpeedColor, (normalized - 0.5f) * 2f);

        if (speedSlider != null)
            speedSlider.value = normalized;

        if (speedFillImage != null)
            speedFillImage.color = gaugeColor;

        if (speedText != null)
        {
            int roundedSpeed = Mathf.RoundToInt(clampedSpeed);
            speedText.text = $"{roundedSpeed} km/h";
            speedText.color = normalized >= 0.85f
                ? Color.Lerp(Color.white, highSpeedColor, (normalized - 0.85f) / 0.15f)
                : Color.white;
        }
    }

    public void UpdatePosition(int currentPlace, int totalRacers)
    {
        if (positionText != null) positionText.text = $"{currentPlace} / {totalRacers}";
    }

    private void UpdateTimerUI(float timeInSeconds)
    {
        if (timerText == null) return;
        timerText.text = FormatTime(timeInSeconds);
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int hundredths = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        return $"{minutes:00}:{seconds:00}.{hundredths:00}";
    }

    public void TogglePause()
    {
        if (!isRaceActive) return;

        isPaused = !isPaused;
        if (pausePanel != null) pausePanel.SetActive(isPaused);
        if (!isPaused && settingsPanel != null) settingsPanel.SetActive(false);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OpenSettings()
    {
        if (settingsPanel == null) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void RestartLevel()
    {
        if (finishMusicSource != null) finishMusicSource.Stop();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        if (finishMusicSource != null) finishMusicSource.Stop();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void ShowResults(int finalPlace, int totalRacers, int coinsEarned)
    {
        isRaceActive = false;
        isRaceStarted = false;
        isPaused = false;
        Time.timeScale = 1f;

        if (hudPanel != null) hudPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(true);

        PlayFinishMusic();

        if (resultsTitleText != null)
        {
            if (finalPlace == 1)
            {
                resultsTitleText.text = "1ST PLACE — VICTORY!";
                resultsTitleText.color = new Color(1f, 0.82f, 0.15f, 1f);
            }
            else if (finalPlace <= 3)
            {
                resultsTitleText.text = "PODIUM FINISH!";
                resultsTitleText.color = new Color(0.35f, 0.85f, 1f, 1f);
            }
            else
            {
                resultsTitleText.text = "RACE FINISHED";
                resultsTitleText.color = Color.white;
            }
        }

        string formattedTime = FormatTime(raceTime);
        if (finalTimeText != null)
            finalTimeText.text = $"Time: {formattedTime}";

        CheckAndSaveBestTime();
        StartCoroutine(AnimateEarnedCoinsRoutine(coinsEarned));
        SyncRewardWithManagers(finalPlace, raceTime, coinsEarned);
    }

    private void PlayFinishMusic()
    {
        if (raceMusicToStop != null && raceMusicToStop.isPlaying)
        {
            raceMusicToStop.Stop();
        }

        if (finishMusic == null) return;

        if (finishMusicSource == null)
        {
            finishMusicSource = gameObject.AddComponent<AudioSource>();
            finishMusicSource.playOnAwake = false;
        }

        finishMusicSource.pitch = 1f;
        finishMusicSource.loop = loopFinishMusic;
        finishMusicSource.clip = finishMusic;
        finishMusicSource.Play();
    }

    private void CheckAndSaveBestTime()
    {
        if (newBestTimeText == null) return;

        if (raceTime <= 0.1f)
        {
            newBestTimeText.gameObject.SetActive(false);
            return;
        }

        string key = $"BestTime_{SceneManager.GetActiveScene().name}";
        float previousBest = PlayerPrefs.GetFloat(key, float.MaxValue);

        if (raceTime < previousBest)
        {
            PlayerPrefs.SetFloat(key, raceTime);
            PlayerPrefs.Save();
            newBestTimeText.text = "NEW BEST TIME!";
            newBestTimeText.gameObject.SetActive(true);
        }
        else
        {
            newBestTimeText.gameObject.SetActive(false);
        }
    }

    private IEnumerator AnimateEarnedCoinsRoutine(int targetCoins)
    {
        if (earnedCoinsText == null) yield break;

        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            int current = Mathf.RoundToInt(Mathf.Lerp(0f, targetCoins, 1f - Mathf.Pow(1f - t, 2f)));
            earnedCoinsText.text = $"+{current} Coins";
            yield return null;
        }

        earnedCoinsText.text = $"+{targetCoins} Coins";
    }

    private void SyncRewardWithManagers(int finalPlace, float finalTime, int coinsEarned)
    {
        bool syncedToCloud = false;

        try
        {
            Type playerDataType = Type.GetType("PlayerDataManager, Assembly-CSharp") ?? Type.GetType("PlayerDataManager");
            if (playerDataType != null)
            {
                object instance = playerDataType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (instance != null)
                {
                    playerDataType.GetMethod("AddCoins")?.Invoke(instance, new object[] { coinsEarned });
                }
            }

            Type firebaseType = Type.GetType("FirebaseManager, Assembly-CSharp") ?? Type.GetType("FirebaseManager");
            if (firebaseType != null)
            {
                object fbInstance = firebaseType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (fbInstance != null)
                {
                    firebaseType.GetMethod("SavePlayerProgress")?.Invoke(fbInstance, null);
                    MethodInfo saveRaceMethod = firebaseType.GetMethod("SaveRaceResult");
                    if (saveRaceMethod != null)
                    {
                        saveRaceMethod.Invoke(fbInstance, new object[] { finalPlace, finalTime, coinsEarned });
                    }
                    syncedToCloud = true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[RaceUI] Cloud sync skipped: {ex.Message}");
        }

        if (cloudStatusText != null)
        {
            cloudStatusText.text = syncedToCloud ? "Saved to Cloud!" : "Reward collected!";
            cloudStatusText.color = new Color(0.35f, 1f, 0.5f, 1f);
        }
    }
}