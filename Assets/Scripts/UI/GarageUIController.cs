using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GarageUIController : MonoBehaviour
{
    [Header("Hlavné texty")]
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text statusText;

    [Header("Top Speed UI")]
    [SerializeField] private TMP_Text speedLevelText;
    [SerializeField] private Button buySpeedButton;
    [SerializeField] private TMP_Text speedButtonText;

    [Header("Acceleration UI")]
    [SerializeField] private TMP_Text accelLevelText;
    [SerializeField] private Button buyAccelButton;
    [SerializeField] private TMP_Text accelButtonText;

    [Header("Handling UI")]
    [SerializeField] private TMP_Text handlingLevelText;
    [SerializeField] private Button buyHandlingButton;
    [SerializeField] private TMP_Text handlingButtonText;

    [Header("Navigácia")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Zvukové efekty")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip upgradeSound;

    [Header("Vizuálne dieliky úrovní (5 Image na každú vlastnosť)")]
    [SerializeField] private Image[] speedBars;
    [SerializeField] private Image[] accelerationBars;
    [SerializeField] private Image[] handlingBars;

    [Header("Farby dielikov")]
    [SerializeField] private Color activeBarColor = new Color(0.2f, 0.85f, 0.35f, 1f);
    [SerializeField] private Color inactiveBarColor = new Color(1f, 1f, 1f, 0.18f);
    [SerializeField] private Color maxLevelColor = new Color(1f, 0.8f, 0.15f, 1f);

    private void OnEnable()
    {
        if (PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.OnDataChanged += RefreshUI;

        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.OnStatusChanged += UpdateStatusText;
            UpdateStatusText(FirebaseManager.Instance.LastStatusMessage, FirebaseManager.Instance.LastStatusIsError);
        }

        RefreshUI();
    }

    private void OnDisable()
    {
        if (PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.OnDataChanged -= RefreshUI;

        if (FirebaseManager.Instance != null)
            FirebaseManager.Instance.OnStatusChanged -= UpdateStatusText;
    }

    private void Update()
    {
        if (IsDemoAddCoinsPressed())
        {
            AddDemoCoins(200);
        }
    }

    private bool IsDemoAddCoinsPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F9);
#endif
    }

    public void BuySpeed() => HandleUpgradePurchase(UpgradeType.TopSpeed);
    public void BuyAcceleration() => HandleUpgradePurchase(UpgradeType.Acceleration);
    public void BuyHandling() => HandleUpgradePurchase(UpgradeType.Handling);

    private void HandleUpgradePurchase(UpgradeType type)
    {
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.TryBuyUpgrade(type))
        {
            PlayUpgradeSound();
        }
    }

    private void PlayUpgradeSound()
    {
        if (upgradeSound == null) return;

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }

        sfxSource.PlayOneShot(upgradeSound);
    }

    public void AddDemoCoins(int amount)
    {
        if (PlayerDataManager.Instance == null) return;
        PlayerDataManager.Instance.AddCoins(amount);
        FirebaseManager.Instance?.SavePlayerProgress();
        UpdateStatusText($"Demo: +{amount} Coins added!", false);
    }

    public void CloseGarage()
    {
        gameObject.SetActive(false);
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void RefreshUI()
    {
        var data = PlayerDataManager.Instance;
        if (data == null) return;

        if (coinsText != null)
            coinsText.text = $"Coins: {data.Coins}";

        UpdateStatRow(
            data.TopSpeedLevel,
            data.Coins,
            data.GetUpgradeCost(UpgradeType.TopSpeed),
            speedLevelText,
            buySpeedButton,
            speedButtonText,
            speedBars
        );

        UpdateStatRow(
            data.AccelerationLevel,
            data.Coins,
            data.GetUpgradeCost(UpgradeType.Acceleration),
            accelLevelText,
            buyAccelButton,
            accelButtonText,
            accelerationBars
        );

        UpdateStatRow(
            data.HandlingLevel,
            data.Coins,
            data.GetUpgradeCost(UpgradeType.Handling),
            handlingLevelText,
            buyHandlingButton,
            handlingButtonText,
            handlingBars
        );
    }

    private void UpdateStatRow(int level, int currentCoins, int cost,
        TMP_Text levelText, Button buyButton, TMP_Text buttonText, Image[] bars)
    {
        int maxLevel = PlayerDataManager.MaxUpgradeLevel;
        bool isMax = level >= maxLevel;

        if (levelText != null)
            levelText.text = isMax ? "MAX LEVEL" : $"LVL {level}/{maxLevel}";

        if (buttonText != null)
        {
            buttonText.text = isMax ? "MAX" : $"{cost} Coins";
            buttonText.color = isMax ? maxLevelColor : new Color(0.15f, 0.15f, 0.15f, 1f);
        }

        if (buyButton != null)
            buyButton.interactable = !isMax && currentCoins >= cost;

        if (bars != null)
        {
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i] == null) continue;
                if (isMax)
                    bars[i].color = maxLevelColor;
                else
                    bars[i].color = (i < level) ? activeBarColor : inactiveBarColor;
            }
        }
    }

    private void UpdateStatusText(string message, bool isError)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.color = isError ? new Color(1f, 0.35f, 0.35f) : new Color(0.35f, 1f, 0.5f);
    }
}