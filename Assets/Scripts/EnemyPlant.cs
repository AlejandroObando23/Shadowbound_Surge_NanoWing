using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyPlant : MonoBehaviour
{
    public enum PlantState { DayStatic, NightChase, FleeGhost }
    public PlantState currentState;
    
    [Header("Stats")]
    public int currentHealth = 2;
    public int damage = 1;
    public float moveSpeedNight = 3f;
    public float moveSpeedFlee = 2f;
    
    [Header("Cooldowns")]
    public float attackCooldown = 1f;
    private float attackTimer = 0f;

    private Transform playerTarget;
    private PlayerController playerController;
    private Rigidbody rb;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeStateChanged += HandleTimeStateChange;
            DetermineState(GameManager.Instance.CurrentTimeState);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeStateChanged -= HandleTimeStateChange;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != GameManager.GameState.Playing)
            return;

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        HandleDynamicState();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != GameManager.GameState.Playing)
            return;

        HandleMovement();
    }

    private void HandleTimeStateChange(GameManager.TimeState newGlobalState)
    {
        DetermineState(newGlobalState);
    }

    private void DetermineState(GameManager.TimeState globalState)
    {
        if (globalState == GameManager.TimeState.Night)
        {
            currentState = PlantState.NightChase;
            currentHealth = 10; 
        }
        else 
        {
            currentState = PlantState.DayStatic;
            currentHealth = 2; 
        }
    }

    private void HandleDynamicState()
    {
        // Override state if player is ghost and near
        if (GameManager.Instance.CurrentTimeState == GameManager.TimeState.Day)
        {
            if (playerController != null && playerController.currentState == PlayerController.PlayerState.GhostForm)
            {
                float dist = Vector3.Distance(transform.position, playerTarget.position);
                if (dist < 5f) // Flee radius
                {
                    currentState = PlantState.FleeGhost;
                }
                else
                {
                    currentState = PlantState.DayStatic;
                }
            }
            else
            {
                currentState = PlantState.DayStatic;
            }
        }
    }

    private void HandleMovement()
    {
        switch (currentState)
        {
            case PlantState.DayStatic:
                // Point head (forward vector) away from center (0,0,0) simulating seeking sun
                Vector3 dirFromCenter = (transform.position - Vector3.zero).normalized;
                dirFromCenter.y = 0;
                if (dirFromCenter != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(dirFromCenter);
                }
                break;
                
            case PlantState.NightChase:
                if (playerTarget != null)
                {
                    Vector3 direction = (playerTarget.position - transform.position).normalized;
                    direction.y = 0;
                    rb.MovePosition(transform.position + direction * moveSpeedNight * Time.fixedDeltaTime);
                    
                    if (direction != Vector3.zero)
                        transform.rotation = Quaternion.LookRotation(direction);
                }
                break;

            case PlantState.FleeGhost:
                if (playerTarget != null)
                {
                    Vector3 direction = (transform.position - playerTarget.position).normalized;
                    direction.y = 0;
                    rb.MovePosition(transform.position + direction * moveSpeedFlee * Time.fixedDeltaTime);
                    
                    if (direction != Vector3.zero)
                        transform.rotation = Quaternion.LookRotation(direction);
                }
                break;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (attackTimer <= 0 && collision.gameObject.CompareTag("Player"))
        {
            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(damage);
                attackTimer = attackCooldown;
            }
        }
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log("Enemy took damage. HP: " + currentHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlantDestroyed();
        }
        Destroy(gameObject);
    }
}
