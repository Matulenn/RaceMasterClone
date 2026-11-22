using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private void OnEnable()
    {
        PlayerDataManager.Instance.OnDataChanged += RefreshUI;
        RefreshUI();
        FirebaseManager.Instance.OnStatusChanged += UpdateFirebaseStatus;
UpdateFirebaseStatus(FirebaseManager.Instance.LastStatusMessage, FirebaseManager.Instance.LastStatusIsError);
    }

    private void OnDisable()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnDataChanged -= RefreshUI;
        }
        if (FirebaseManager.Instance != null)
    FirebaseManager.Instance.OnStatusChanged -= UpdateFirebaseStatus;
    }

    public void RefreshUI()
    {
        var data = PlayerDataManager.Instance;

        if (coinsText != null)
            coinsText.text = $"Coins: {data.Coins}";

        UpdateUpgradeRow(UpgradeType.TopSpeed, speedLevelText, buySpeedButton, speedButtonText);
        UpdateUpgradeRow(UpgradeType.Acceleration, accelLevelText, buyAccelButton, accelButtonText);
        UpdateUpgradeRow(UpgradeType.Handling, handlingLevelText, buyHandlingButton, handlingButtonText);
    }

    private void UpdateUpgradeRow(UpgradeType type, TMP_Text levelText, Button buyButton, TMP_Text buttonText)
    {
        var data = PlayerDataManager.Instance;
        int currentLevel = data.GetLevel(type);
        int maxLevel = PlayerDataManager.MaxUpgradeLevel;

        if (levelText != null)
            levelText.text = $"LVL {currentLevel} / {maxLevel}";

        if (currentLevel >= maxLevel)
        {
            if (buttonText != null) buttonText.text = "MAX";
            if (buyButton != null) buyButton.interactable = false;
        }
        else
        {
            int cost = data.GetUpgradeCost(type);
            if (buttonText != null) buttonText.text = $"{cost} Coins";
            if (buyButton != null) buyButton.interactable = data.Coins >= cost;
        }
    }

    public void BuyTopSpeed() => HandlePurchase(UpgradeType.TopSpeed, "Top Speed");
    public void BuyAcceleration() => HandlePurchase(UpgradeType.Acceleration, "Acceleration");
    public void BuyHandling() => HandlePurchase(UpgradeType.Handling, "Handling");

    private void HandlePurchase(UpgradeType type, string upgradeName)
    {
        bool success = PlayerDataManager.Instance.TryBuyUpgrade(type);
        if (statusText != null)
        {
            statusText.text = success
                ? $"{upgradeName} upgraded to LVL {PlayerDataManager.Instance.GetLevel(type)}!"
                : "Not enough coins!";
        }
    }

    public void AddTestCoins()
    {
        PlayerDataManager.Instance.AddCoins(200);
        if (statusText != null) statusText.text = "+200 test coins added.";
    }

    [Header("Navigácia")]
    [SerializeField] private GameObject mainMenuPanel;

    public void CloseGarage()
    {
        gameObject.SetActive(false);
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }
    private void UpdateFirebaseStatus(string message, bool isError)
{
    if (statusText == null) return;
    statusText.text = message;
    statusText.color = isError ? new Color(1f, 0.35f, 0.35f) : new Color(0.55f, 0.95f, 0.65f);
}
}