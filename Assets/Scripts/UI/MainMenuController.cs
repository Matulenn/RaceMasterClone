using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panely")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject garagePanel;

    [Header("Stav mincí (pravý horný roh)")]
    [SerializeField] private GameObject coinBadgeObject;
    [SerializeField] private TMP_Text coinsText;

    [Header("Karty tratí (Level Select)")]
    [SerializeField] private string track1SceneName = "Track";
    [SerializeField] private TMP_Text track1BestTimeText;
    [SerializeField] private string track2SceneName = "Track2";
    [SerializeField] private TMP_Text track2BestTimeText;

    [Header("Audio Zdroje")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Prvky nastavení")]
    [SerializeField] private Slider gameVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Animácie tlačidiel")]
    [SerializeField] private bool autoAttachButtonHover = true;

    private void Start()
    {
        Time.timeScale = 1f;

        if (autoAttachButtonHover)
        {
            AttachHoverEffectsToButtons();
        }

        // Načítanie uložených nastavení zvuku
        float gameVol = PlayerPrefs.GetFloat("GameVolume", 1f);
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

        AudioListener.volume = gameVol;
        if (musicSource != null) musicSource.volume = musicVol;
        if (sfxSource != null) sfxSource.volume = sfxVol;

        if (gameVolumeSlider != null) gameVolumeSlider.SetValueWithoutNotify(gameVol);
        if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(musicVol);
        if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(sfxVol);

        // Načítanie obrazovky a grafiky
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

        int savedQuality = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(savedQuality);
        if (qualityDropdown != null) qualityDropdown.SetValueWithoutNotify(savedQuality);

        RefreshCoinsDisplay();
        RefreshTrackRecords();
        ShowMainMenu();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool isInSubMenu = mainMenuPanel != null && !mainMenuPanel.activeSelf;
            if (isInSubMenu)
            {
                PlayClickSound();
                ShowMainMenu();
            }
        }

#if UNITY_EDITOR
        // F9 pridá +500 testovacích mincí na overenie zobrazenia v rohu
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            AddTestCoins(500);
        }
#endif
    }

    private void AttachHoverEffectsToButtons()
    {
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        AudioClip clickClip = sfxSource != null ? sfxSource.clip : null;

        foreach (Button btn in allButtons)
        {
            UIButtonHoverEffect hover = btn.GetComponent<UIButtonHoverEffect>();
            if (hover == null)
            {
                hover = btn.gameObject.AddComponent<UIButtonHoverEffect>();
            }
            hover.ConfigureAudio(sfxSource, clickClip);
        }
    }

    public void RefreshCoinsDisplay()
    {
        if (coinsText == null) return;

        int coins = GetCurrentCoins();
        coinsText.text = coins.ToString("N0");
    }

    private int GetCurrentCoins()
    {
        try
        {
            Type playerDataType = Type.GetType("PlayerDataManager, Assembly-CSharp") ?? Type.GetType("PlayerDataManager");
            if (playerDataType != null)
            {
                object instance = playerDataType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (instance != null)
                {
                    PropertyInfo coinsProp = playerDataType.GetProperty("Coins", BindingFlags.Public | BindingFlags.Instance);
                    if (coinsProp != null)
                        return Convert.ToInt32(coinsProp.GetValue(instance));

                    MethodInfo getCoinsMethod = playerDataType.GetMethod("GetCoins", BindingFlags.Public | BindingFlags.Instance);
                    if (getCoinsMethod != null)
                        return Convert.ToInt32(getCoinsMethod.Invoke(instance, null));

                    FieldInfo coinsField = playerDataType.GetField("coins", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (coinsField != null)
                        return Convert.ToInt32(coinsField.GetValue(instance));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainMenu] Načítanie mincí z PlayerDataManager preskočené: {ex.Message}");
        }

        return PlayerPrefs.GetInt("Coins", 0);
    }

#if UNITY_EDITOR
    private void AddTestCoins(int amount)
    {
        bool addedToManager = false;
        try
        {
            Type playerDataType = Type.GetType("PlayerDataManager, Assembly-CSharp") ?? Type.GetType("PlayerDataManager");
            if (playerDataType != null)
            {
                object instance = playerDataType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                MethodInfo addMethod = playerDataType.GetMethod("AddCoins", BindingFlags.Public | BindingFlags.Instance);
                if (instance != null && addMethod != null)
                {
                    addMethod.Invoke(instance, new object[] { amount });
                    addedToManager = true;
                }
            }
        }
        catch { }

        if (!addedToManager)
        {
            int current = PlayerPrefs.GetInt("Coins", 0) + amount;
            PlayerPrefs.SetInt("Coins", current);
            PlayerPrefs.Save();
        }

        RefreshCoinsDisplay();
    }
#endif

    public void RefreshTrackRecords()
    {
        UpdateSingleTrackRecord(track1SceneName, track1BestTimeText);
        UpdateSingleTrackRecord(track2SceneName, track2BestTimeText);
    }

    private void UpdateSingleTrackRecord(string sceneName, TMP_Text targetText)
    {
        if (targetText == null) return;

        string key = $"BestTime_{sceneName}";
        float bestTime = PlayerPrefs.GetFloat(key, -1f);

        if (bestTime > 0f && bestTime < float.MaxValue)
        {
            targetText.text = $"BEST TIME: {FormatTime(bestTime)}";
            targetText.color = new Color(1f, 0.82f, 0.15f, 1f);
        }
        else
        {
            targetText.text = "BEST TIME: --:--.--";
            targetText.color = new Color(0.65f, 0.7f, 0.8f, 1f);
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int hundredths = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        return $"{minutes:00}:{seconds:00}.{hundredths:00}";
    }

    public void ShowMainMenu()
    {
        RefreshCoinsDisplay();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (garagePanel != null) garagePanel.SetActive(false);
        if (coinBadgeObject != null) coinBadgeObject.SetActive(true);
    }

    public void ShowLevelSelect()
    {
        RefreshCoinsDisplay();
        RefreshTrackRecords();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (garagePanel != null) garagePanel.SetActive(false);
        if (coinBadgeObject != null) coinBadgeObject.SetActive(true);
    }

    public void ShowSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (garagePanel != null) garagePanel.SetActive(false);
        if (coinBadgeObject != null) coinBadgeObject.SetActive(false);
    }

    public void ShowGarage()
    {
        if (garagePanel == null) return;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        garagePanel.SetActive(true);
        if (coinBadgeObject != null) coinBadgeObject.SetActive(false);
    }

    public void SetGameVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("GameVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null) musicSource.volume = volume;
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null) sfxSource.volume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
    }

    public void PlayClickSound()
    {
        if (sfxSource != null && sfxSource.clip != null)
        {
            sfxSource.PlayOneShot(sfxSource.clip);
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
        PlayerPrefs.Save();
    }

    public void LoadLevel(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[MainMenu] Scéna '{sceneName}' ešte nie je pridaná v Build Profiles -> Scene List!");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void LoadTrack1() => LoadLevel(track1SceneName);
    public void LoadTrack2() => LoadLevel(track2SceneName);

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Hra bola ukončená.");
    }
}