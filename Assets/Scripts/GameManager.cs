using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Countdown, Playing, GameOver }
    public GameState CurrentGameState { get; private set; } = GameState.Menu;

    public enum TimeState { Day, Night }
    public TimeState CurrentTimeState { get; private set; } = TimeState.Day;

    [Header("Cycle Settings")]
    public float dayDuration = 90f;
    public float nightDuration = 90f;
    private float cycleTimer = 0f;
    public int CycleCount { get; private set; } = 0; // Increments when Night falls

    [Header("Spawn Settings")]
    public float spawnRadius = 15f;
    public GameObject enemyPrefab; // We'll assign this via code or Inspector
    
    [Header("Stats")]
    public int PlantsDestroyed { get; private set; } = 0;

    public event Action<TimeState> OnTimeStateChanged;
    public event Action<GameState> OnGameStateChanged;

    private float countdownTimer = 3f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        SetGameState(GameState.Menu);
    }

    private void Update()
    {
        switch (CurrentGameState)
        {
            case GameState.Menu:
                if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame || 
                    (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
                {
                    SetGameState(GameState.Countdown);
                    countdownTimer = 3f;
                }
                break;

            case GameState.Countdown:
                countdownTimer -= Time.deltaTime;
                if (countdownTimer <= 0)
                {
                    StartGame();
                }
                break;

            case GameState.Playing:
                cycleTimer -= Time.deltaTime;
                if (cycleTimer <= 0f)
                {
                    SwitchTimeState();
                }
                break;
        }
    }

    private void SetGameState(GameState newState)
    {
        CurrentGameState = newState;
        OnGameStateChanged?.Invoke(CurrentGameState);
    }

    private void StartGame()
    {
        SetGameState(GameState.Playing);
        cycleTimer = dayDuration;
        CurrentTimeState = TimeState.Day;
        CycleCount = 0;
        PlantsDestroyed = 0;
        
        OnTimeStateChanged?.Invoke(CurrentTimeState);

        // Initial spawn of 7 plants
        for (int i = 0; i < 7; i++)
        {
            SpawnPlant();
        }
    }

    private void SwitchTimeState()
    {
        if (CurrentTimeState == TimeState.Day)
        {
            CurrentTimeState = TimeState.Night;
            cycleTimer = nightDuration;
            CycleCount++; // Cycle 1 is the first night
            Debug.Log($"Night {CycleCount} has fallen!");
        }
        else
        {
            CurrentTimeState = TimeState.Day;
            cycleTimer = dayDuration;
            Debug.Log("Day has broken!");
        }

        OnTimeStateChanged?.Invoke(CurrentTimeState);
    }

    public void OnPlantDestroyed()
    {
        PlantsDestroyed++;
        
        if (CurrentGameState != GameState.Playing) return;

        // Progressive Spawn Algorithm based on CycleCount
        float baseProb = 0f;
        if (CycleCount == 1) baseProb = 0.25f;
        else if (CycleCount == 2) baseProb = 0.50f;
        else if (CycleCount == 3) baseProb = 0.75f;
        else if (CycleCount >= 4) baseProb = 1.0f + ((CycleCount - 4) * 0.25f);

        // First plant spawn chance
        if (UnityEngine.Random.value <= baseProb)
        {
            SpawnPlant();
        }

        // Second plant spawn chance (excess probability)
        if (baseProb > 1.0f)
        {
            float extraProb = baseProb - 1.0f;
            if (UnityEngine.Random.value <= extraProb)
            {
                SpawnPlant();
            }
        }
    }

    private void SpawnPlant()
    {
        if (enemyPrefab == null)
        {
            // Fallback for MVP: Find an existing enemy to clone, or we will instantiate a primitive
            GameObject existingEnemy = GameObject.FindGameObjectWithTag("Enemy");
            if (existingEnemy != null) enemyPrefab = existingEnemy;
            else return;
        }

        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = new Vector3(randomCircle.x, 0.5f, randomCircle.y);
        
        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }

    public void TriggerGameOver()
    {
        SetGameState(GameState.GameOver);
        Debug.Log($"Game Over! Plants destroyed: {PlantsDestroyed}");
    }

    public float GetTimeRemaining() => cycleTimer;
    public float GetCountdown() => countdownTimer;
}
