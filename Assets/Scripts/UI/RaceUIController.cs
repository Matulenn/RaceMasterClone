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
    [SerializeField] private GameObject resultsPanel;

    [Header("HUD Prvky")]
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private float maxDisplaySpeed = 220f;
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private TMP_Text timerText;

    [Header("Výsledková obrazovka")]
    [SerializeField] private TMP_Text finalPositionText;
    [SerializeField] private TMP_Text finalTimeText;
    [SerializeField] private TMP_Text earnedCoinsText;

    private float raceTime;
    private bool isRaceActive = true;
    private bool isPaused;

    private void Start()
    {
        Time.timeScale = 1f;
        if (hudPanel != null) hudPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
    }

    private void Update()
    {
        if (isRaceActive && !isPaused)
        {
            raceTime += Time.deltaTime;
            UpdateTimerUI(raceTime);
        }

        if (isRaceActive && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    // Verejná metóda pre skript auta
    public void UpdateSpeed(float speedKmh)
    {
        int roundedSpeed = Mathf.RoundToInt(Mathf.Max(0f, speedKmh));
        if (speedText != null) speedText.text = $"{roundedSpeed} km/h";
        if (speedSlider != null) speedSlider.value = Mathf.Clamp01(speedKmh / maxDisplaySpeed);
    }

    // Verejná metóda pre skript poradia na trati
    public void UpdatePosition(int currentPlace, int totalRacers)
    {
        if (positionText != null) positionText.text = $"{currentPlace} / {totalRacers}";
    }

    private void UpdateTimerUI(float timeInSeconds)
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int hundredths = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        timerText.text = $"{minutes:00}:{seconds:00}.{hundredths:00}";
    }

    public void TogglePause()
    {
        if (!isRaceActive) return;

        isPaused = !isPaused;
        if (pausePanel != null) pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // Zavolá sa po prejazde cieľovou čiarou
    public void ShowResults(int finalPlace, int totalRacers, int coinsEarned)
    {
        isRaceActive = false;
        isPaused = false;
        Time.timeScale = 1f;

        if (hudPanel != null) hudPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(true);

        if (finalPositionText != null) finalPositionText.text = $"Position: {finalPlace} / {totalRacers}";
        if (finalTimeText != null) finalTimeText.text = $"Time: {timerText.text}";
        if (earnedCoinsText != null) earnedCoinsText.text = $"+{coinsEarned} Coins";
    }
}