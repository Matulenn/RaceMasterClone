using System;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

[Serializable]
public class PlayerCloudData
{
    public int coins;
    public int topSpeedLevel;
    public int accelerationLevel;
    public int handlingLevel;
}

[Serializable]
public class RaceResultCloudData
{
    public string levelName;
    public int place;
    public int totalRacers;
    public int score;
    public float timeSeconds;
    public int coinsEarned;
    public string timestamp;
}

public class FirebaseManager : MonoBehaviour
{
    private static FirebaseManager instance;
    private static bool isQuitting;

    public static FirebaseManager Instance
    {
        get
        {
            if (isQuitting) return null;
            if (instance == null)
            {
                GameObject obj = new GameObject("DatabaseManager");
                instance = obj.AddComponent<FirebaseManager>();
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

    private DatabaseReference dbReference;
    private bool isInitialized;
    private string playerId;

    // Udalosť pre zobrazenie stavu v UI: (text správy, jeToChyba)
    public event Action<string, bool> OnStatusChanged;
    public string LastStatusMessage { get; private set; } = "Connecting to Firebase...";
    public bool LastStatusIsError { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Jedinečné ID zariadenia/hráča uložené lokálne
        playerId = PlayerPrefs.GetString("FirebasePlayerId", "");
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = "player_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            PlayerPrefs.SetString("FirebasePlayerId", playerId);
            PlayerPrefs.Save();
        }
    }

    private void Start()
    {
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        SetStatus("Connecting to Firebase...", false);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                isInitialized = true;
                Debug.Log("Firebase database is ready.");
                LoadPlayerProgress();
            }
            else
            {
                SetStatus($"Firebase error: {task.Result}", true);
                Debug.LogError($"Could not resolve Firebase dependencies: {task.Result}");
            }
        });
    }

    // Načíta mince a zakúpené vylepšenia pri spustení hry
    public void LoadPlayerProgress()
    {
        if (!isInitialized) return;

        SetStatus("Loading cloud progress...", false);

        dbReference.Child("users").Child(playerId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                SetStatus("Failed to load cloud data (using local save).", true);
                return;
            }

            DataSnapshot snapshot = task.Result;
            if (snapshot.Exists && !string.IsNullOrEmpty(snapshot.GetRawJsonValue()))
            {
                PlayerCloudData cloudData = JsonUtility.FromJson<PlayerCloudData>(snapshot.GetRawJsonValue());
                if (PlayerDataManager.Instance != null)
                {
                    PlayerDataManager.Instance.ApplyCloudData(
                        cloudData.coins,
                        cloudData.topSpeedLevel,
                        cloudData.accelerationLevel,
                        cloudData.handlingLevel
                    );
                }
                SetStatus("Cloud progress loaded.", false);
            }
            else
            {
                // Prvé spustenie: nahráme počiatočné dáta do Firebase
                SavePlayerProgress();
            }
        });
    }

    // Uloží aktuálne mince a úrovne vylepšení z garáže do Firebase
    public void SavePlayerProgress()
    {
        if (!isInitialized)
        {
            SetStatus("Offline: Saved locally only.", true);
            return;
        }

        var data = PlayerDataManager.Instance;
        if (data == null) return;

        SetStatus("Saving progress to Firebase...", false);

        PlayerCloudData cloudData = new PlayerCloudData
        {
            coins = data.Coins,
            topSpeedLevel = data.TopSpeedLevel,
            accelerationLevel = data.AccelerationLevel,
            handlingLevel = data.HandlingLevel
        };

        string json = JsonUtility.ToJson(cloudData);

        dbReference.Child("users").Child(playerId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                SetStatus("Save failed! Check internet connection.", true);
                Debug.LogError("Firebase save error: " + task.Exception);
            }
            else
            {
                SetStatus("Saved to Firebase!", false);
            }
        });
    }

    // Uloží výsledok pretekov (vrátane levelu a skóre) a pripočíta získané mince
    public void SaveRaceResult(int finalPlace, int totalRacers, float raceTimeSeconds, int coinsEarned, string levelName = "Level 1")
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddCoins(coinsEarned);
        }

        if (!isInitialized)
        {
            SetStatus("Offline: Result saved locally only.", true);
            return;
        }

        SetStatus("Saving race result...", false);

        int calculatedScore = Mathf.Max(100, (totalRacers - finalPlace + 1) * 500 - Mathf.RoundToInt(raceTimeSeconds * 5f));

        RaceResultCloudData resultData = new RaceResultCloudData
        {
            levelName = levelName,
            place = finalPlace,
            totalRacers = totalRacers,
            score = calculatedScore,
            timeSeconds = Mathf.Round(raceTimeSeconds * 100f) / 100f,
            coinsEarned = coinsEarned,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        string json = JsonUtility.ToJson(resultData);

        dbReference.Child("results").Child(playerId).Push().SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                SetStatus("Failed to save race result!", true);
            }
            else
            {
                SavePlayerProgress();
            }
        });
    }

    private void SetStatus(string message, bool isError)
    {
        LastStatusMessage = message;
        LastStatusIsError = isError;
        OnStatusChanged?.Invoke(message, isError);
    }
}