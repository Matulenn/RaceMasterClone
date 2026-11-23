using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUIController : MonoBehaviour
{
    [Header("Prvky nastavení")]
    [SerializeField] private Slider gameVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Voliteľné audio zdroje")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (musicSource == null)
        {
            GameObject musicObj = GameObject.Find("MenuMusic");
            if (musicObj != null) musicSource = musicObj.GetComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            GameObject sfxObj = GameObject.Find("menuSFX");
            if (sfxObj != null) sfxSource = sfxObj.GetComponent<AudioSource>();
        }
    }

    private void OnEnable()
    {
        float gameVol = PlayerPrefs.GetFloat("GameVolume", 1f);
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

        AudioListener.volume = gameVol;
        if (musicSource != null) musicSource.volume = musicVol;
        if (sfxSource != null) sfxSource.volume = sfxVol;

        if (gameVolumeSlider != null) gameVolumeSlider.SetValueWithoutNotify(gameVol);
        if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(musicVol);
        if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(sfxVol);

        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

        int savedQuality = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        if (qualityDropdown != null) qualityDropdown.SetValueWithoutNotify(savedQuality);
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
}