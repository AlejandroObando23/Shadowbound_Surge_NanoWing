using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public enum PlayerState { Physical, GhostForm, FullPower, WeakNight }
    
    [Header("Movement")]
    public float moveSpeed = 5f;
    public LayerMask groundLayer;
    
    [Header("Stats")]
    public int lives = 3;
    public float maxStamina = 100f;
    public float staminaRegenRate = 10f; // per sec in sun
    public float ghostStaminaDrainRate = 15f; // per sec in ghost form
    
    public int CurrentLives { get; private set; }
    public float CurrentStamina { get; private set; }
    
    [Header("Combat (Ghost Form)")]
    public float ghostDashCost = 15f;
    public float ghostSpinCost = 10f;
    public int ghostDamage = 1;
    public float ghostAttackRadius = 4f;

    [Header("Combat (Full Power)")]
    public float powerDashCost = 30f;
    public float powerSpinCost = 20f;
    public int powerDashDamage = 4;
    public int powerSpinDamage = 5;
    public float powerAttackRadius = 6f;

    [Header("Cooldowns")]
    public float dashCooldown = 1f;
    public float spinCooldown = 1f;
    private float dashTimer = 0f;
    private float spinTimer = 0f;

    [Header("State")]
    public PlayerState currentState = PlayerState.Physical;
    private bool isInCloudShadow = false;

    private Rigidbody rb;
    private Camera mainCamera;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        
        CurrentLives = lives;
        CurrentStamina = 0f;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeStateChanged += HandleTimeStateChange;
            GameManager.Instance.OnGameStateChanged += HandleGameStateChange;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeStateChanged -= HandleTimeStateChange;
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChange;
        }
    }

    private void HandleGameStateChange(GameManager.GameState newState)
    {
        if (newState == GameManager.GameState.Playing)
        {
            CurrentLives = lives;
            CurrentStamina = 0f;
            TransitionToState(PlayerState.Physical);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != GameManager.GameState.Playing)
            return;

        HandleStateAndStamina();
        UpdateCooldowns();
        HandleInputs();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != GameManager.GameState.Playing)
            return;

        HandleContinuousMovement();
    }

    private void HandleTimeStateChange(GameManager.TimeState newTimeState)
    {
        if (newTimeState == GameManager.TimeState.Night)
        {
            if (CurrentStamina >= maxStamina * 0.99f) 
            {
                TransitionToState(PlayerState.FullPower);
            }
            else
            {
                TransitionToState(PlayerState.WeakNight);
            }
        }
        else // Day
        {
            TransitionToState(PlayerState.Physical);
        }
    }

    private void HandleStateAndStamina()
    {
        bool isGlobalDay = GameManager.Instance.CurrentTimeState == GameManager.TimeState.Day;
        bool inSun = isGlobalDay && !isInCloudShadow;

        if (inSun && currentState != PlayerState.GhostForm)
        {
            CurrentStamina += staminaRegenRate * Time.deltaTime;
            CurrentStamina = Mathf.Clamp(CurrentStamina, 0, maxStamina);
            
            if (currentState == PlayerState.GhostForm)
                TransitionToState(PlayerState.Physical);
        }

        if (currentState == PlayerState.GhostForm)
        {
            CurrentStamina -= ghostStaminaDrainRate * Time.deltaTime;
            if (CurrentStamina <= 0)
            {
                TransitionToState(PlayerState.Physical);
            }
        }

        if (currentState == PlayerState.FullPower)
        {
            if (CurrentStamina <= 0)
            {
                TransitionToState(PlayerState.WeakNight);
            }
        }
    }

    private void UpdateCooldowns()
    {
        if (dashTimer > 0) dashTimer -= Time.deltaTime;
        if (spinTimer > 0) spinTimer -= Time.deltaTime;
    }

    private void HandleInputs()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && spinTimer <= 0)
        {
            PerformLeftClickAction();
        }
        
        if (Mouse.current.rightButton.wasPressedThisFrame && dashTimer <= 0)
        {
            PerformRightClickAction();
        }
    }

    private void PerformLeftClickAction()
    {
        switch (currentState)
        {
            case PlayerState.Physical:
            case PlayerState.WeakNight:
                // No attacks in weak/physical form
                break;
            case PlayerState.GhostForm:
                if (ConsumeStamina(ghostSpinCost))
                {
                    spinTimer = spinCooldown;
                    PerformConeAttack(ghostAttackRadius, ghostDamage);
                }
                break;
            case PlayerState.FullPower:
                if (ConsumeStamina(powerSpinCost))
                {
                    spinTimer = spinCooldown;
                    PerformSpinAttack(powerAttackRadius, powerSpinDamage);
                }
                break;
        }
    }

    private void PerformRightClickAction()
    {
        switch (currentState)
        {
            case PlayerState.Physical:
                if (isInCloudShadow && CurrentStamina > 0)
                {
                    TransitionToState(PlayerState.GhostForm);
                }
                break;
            case PlayerState.WeakNight:
                break;
            case PlayerState.GhostForm:
                if (ConsumeStamina(ghostDashCost))
                {
                    dashTimer = dashCooldown;
                    // For MVP, just do damage in front
                    PerformSpinAttack(2f, ghostDamage); 
                }
                break;
            case PlayerState.FullPower:
                if (ConsumeStamina(powerDashCost))
                {
                    dashTimer = dashCooldown;
                    // For MVP, just do damage in front
                    PerformSpinAttack(4f, powerDashDamage); 
                }
                break;
        }
    }
    
    private void PerformSpinAttack(float radius, int damage)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyPlant enemy = hit.GetComponent<EnemyPlant>();
                if (enemy != null) enemy.TakeDamage(damage);
            }
        }
    }

    private void PerformConeAttack(float radius, int damage)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToTarget);
                
                // 45 degree cone means 22.5 degrees on each side of the forward vector
                if (angle < 22.5f)
                {
                    EnemyPlant enemy = hit.GetComponent<EnemyPlant>();
                    if (enemy != null) enemy.TakeDamage(damage);
                }
            }
        }
    }

    private bool ConsumeStamina(float amount)
    {
        if (CurrentStamina >= amount)
        {
            CurrentStamina -= amount;
            return true;
        }
        return false;
    }

    private void TransitionToState(PlayerState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log("Player State Changed to: " + currentState);
    }

    private void HandleContinuousMovement()
    {
        if (Mouse.current == null) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            Vector3 targetPoint = hit.point;
            targetPoint.y = transform.position.y;
            
            Vector3 direction = (targetPoint - transform.position).normalized;
            
            // "If the mouse is still, wait animation" -> checking distance
            if (Vector3.Distance(transform.position, targetPoint) > 0.2f)
            {
                rb.MovePosition(transform.position + direction * moveSpeed * Time.fixedDeltaTime);
                transform.rotation = Quaternion.LookRotation(direction);
                // Trigger Run Animation here
            }
            else
            {
                // Trigger Idle Animation here
            }
        }
    }

    public void SetInCloudShadow(bool inShadow)
    {
        isInCloudShadow = inShadow;
        if (!inShadow && currentState == PlayerState.GhostForm)
        {
            TransitionToState(PlayerState.Physical);
        }
    }

    public void TakeDamage(int damage)
    {
        // Add i-frames or logic to prevent instant death from multiple hits
        CurrentLives--;
        Debug.Log("Player Took Damage! Lives: " + CurrentLives);
        
        if (CurrentLives <= 0)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
}
