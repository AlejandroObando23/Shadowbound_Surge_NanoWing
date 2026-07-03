using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Egg : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            // Solo recolectar si está en estado físico
            if (pc != null && pc.currentState == PlayerController.PlayerState.Physical)
            {
                CollectEgg();
            }
        }
    }

    private void CollectEgg()
    {
        Debug.Log("Egg Collected!");
        // Add to global score in GameManager
        Destroy(gameObject);
    }
}
