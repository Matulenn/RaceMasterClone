using System;
using UnityEngine;

public enum UpgradeType
{
    TopSpeed,
    Acceleration,
    Handling
}

public class PlayerDataManager : MonoBehaviour
{
    private static PlayerDataManager instance;
    private static bool isQuitting;
    public static PlayerDataManager Instance
    {
        get
        {
            if (isQuitting) return null;
            if (instance == null)
            {
                GameObject obj = new GameObject("PlayerDataManager");
                instance = obj.AddComponent<PlayerDataManager>();
            }
            return instance;
        }
    }
    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            isQuitting = true;
        }
    }

    public const int MaxUpgradeLevel = 5;

    public int Coins { get; private set; }
    public int TopSpeedLevel { get; private set; }
    public int AccelerationLevel { get; private set; }
    public int HandlingLevel { get; private set; }

    public event Action OnDataChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadLocalData();
    }

    public int GetLevel(UpgradeType type)
    {
        return type switch
        {
            UpgradeType.TopSpeed => TopSpeedLevel,
            UpgradeType.Acceleration => AccelerationLevel,
            UpgradeType.Handling => HandlingLevel,
            _ => 1
        };
    }

    public int GetUpgradeCost(UpgradeType type)
    {
        int currentLevel = GetLevel(type);
        if (currentLevel >= MaxUpgradeLevel) return 0;

        // Ceny: Lvl 1->2: 100, Lvl 2->3: 200, Lvl 3->4: 350, Lvl 4->5: 500
        return currentLevel switch
        {
            1 => 100,
            2 => 200,
            3 => 350,
            4 => 500,
            _ => 500
        };
    }

    public bool TryBuyUpgrade(UpgradeType type)
    {
        int currentLevel = GetLevel(type);
        if (currentLevel >= MaxUpgradeLevel) return false;

        int cost = GetUpgradeCost(type);
        if (Coins < cost) return false;

        Coins -= cost;

        switch (type)
        {
            case UpgradeType.TopSpeed:
                TopSpeedLevel++;
                break;
            case UpgradeType.Acceleration:
                AccelerationLevel++;
                break;
            case UpgradeType.Handling:
                HandlingLevel++;
                break;
        }

        SaveLocalData();
        FirebaseManager.Instance.SavePlayerProgress();
        OnDataChanged?.Invoke();
        return true;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        Coins += amount;
        SaveLocalData();
        OnDataChanged?.Invoke();
    }

    // Násobiče pre skript auta kolegov (napr. 1.0x až 1.4x)
    public float GetSpeedMultiplier() => 1f + (TopSpeedLevel - 1) * 0.1f;
    public float GetAccelerationMultiplier() => 1f + (AccelerationLevel - 1) * 0.12f;
    public float GetHandlingMultiplier() => 1f + (HandlingLevel - 1) * 0.1f;

    public void LoadLocalData()
    {
        // Nový hráč dostane 300 mincí na úvodné vyskúšanie garáže
        Coins = PlayerPrefs.GetInt("PlayerCoins", 300);
        TopSpeedLevel = PlayerPrefs.GetInt("Upgrade_TopSpeed", 1);
        AccelerationLevel = PlayerPrefs.GetInt("Upgrade_Acceleration", 1);
        HandlingLevel = PlayerPrefs.GetInt("Upgrade_Handling", 1);
        OnDataChanged?.Invoke();
    }

    public void SaveLocalData()
    {
        PlayerPrefs.SetInt("PlayerCoins", Coins);
        PlayerPrefs.SetInt("Upgrade_TopSpeed", TopSpeedLevel);
        PlayerPrefs.SetInt("Upgrade_Acceleration", AccelerationLevel);
        PlayerPrefs.SetInt("Upgrade_Handling", HandlingLevel);
        PlayerPrefs.Save();
    }

    // Metóda pripravená pre načítanie dát z Firebase
    public void ApplyCloudData(int coins, int speedLvl, int accelLvl, int handlingLvl)
    {
        Coins = Mathf.Max(0, coins);
        TopSpeedLevel = Mathf.Clamp(speedLvl, 1, MaxUpgradeLevel);
        AccelerationLevel = Mathf.Clamp(accelLvl, 1, MaxUpgradeLevel);
        HandlingLevel = Mathf.Clamp(handlingLvl, 1, MaxUpgradeLevel);

        SaveLocalData();
        OnDataChanged?.Invoke();
    }
}