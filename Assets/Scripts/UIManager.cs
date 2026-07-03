using UnityEngine;
using UnityEngine.UI; // Using standard UI for MVP. Can upgrade to TMPro later if needed.

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Text statusText;
    public Text scoreText;
    public Text livesText;
    public Text timeText;
    public Image staminaFill;

    private PlayerController playerController;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponent<PlayerController>();
        }

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void Update()
    {
        UpdateUI();
    }

    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        if (newState == GameManager.GameState.Menu)
        {
            if (statusText) statusText.text = "Press Any Key to Start";
        }
        else if (newState == GameManager.GameState.GameOver)
        {
            if (statusText) statusText.text = "GAME OVER\nFinal Score: " + GameManager.Instance.PlantsDestroyed;
        }
    }

    private void UpdateUI()
    {
        if (GameManager.Instance == null) return;

        // Status Text (Countdown)
        if (GameManager.Instance.CurrentGameState == GameManager.GameState.Countdown)
        {
            int count = Mathf.CeilToInt(GameManager.Instance.GetCountdown());
            if (statusText) statusText.text = count.ToString();
        }
        else if (GameManager.Instance.CurrentGameState == GameManager.GameState.Playing)
        {
            if (statusText) statusText.text = ""; // Clear status text during gameplay
        }

        // Score
        if (scoreText) scoreText.text = "Deforested: " + GameManager.Instance.PlantsDestroyed;

        // Time
        if (timeText) 
        {
            float time = GameManager.Instance.GetTimeRemaining();
            int min = Mathf.FloorToInt(time / 60);
            int sec = Mathf.FloorToInt(time % 60);
            string cycle = GameManager.Instance.CurrentTimeState.ToString();
            timeText.text = $"{cycle} {min}:{sec:D2}";
        }

        // Player Stats
        if (playerController != null)
        {
            if (livesText) livesText.text = "Lives: " + playerController.CurrentLives;
            
            if (staminaFill) 
            {
                staminaFill.fillAmount = playerController.CurrentStamina / playerController.maxStamina;
            }
        }
    }
}
