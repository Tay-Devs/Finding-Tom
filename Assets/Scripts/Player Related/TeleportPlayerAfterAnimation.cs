using System.Collections;
using UnityEngine;

public class TeleportPlayerAfterAnimation : MonoBehaviour
{
    [SerializeField] 
    private Transform spawnPoint;
    
    [SerializeField]
    private GameObject playerGameObject;
    
    private CharacterController characterController;
    
    private void Start()
    {
        if (playerGameObject == null)
        {
            Debug.LogError("Player GameObject not assigned!");
            return;
        }
        
        // Get the CharacterController component from the player
        characterController = playerGameObject.GetComponent<CharacterController>();
        
        if (characterController == null)
        {
            Debug.LogError("Character Controller component not found on player!");
        }
        
        if (spawnPoint == null)
        {
            Debug.LogWarning("No spawn point assigned! Please assign a spawn point in the inspector.");
        }
    }

    private IEnumerator TeleportPlayer()
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Cannot teleport: No spawn point assigned!");
            yield break;
        }
        
        if (characterController == null || playerGameObject == null)
        {
            Debug.LogError("Cannot teleport: Player or Character Controller is missing!");
            yield break;
        }

        yield return new WaitForSeconds(0.1f);
        
        // Set position of the player GameObject, not this script's GameObject
        playerGameObject.transform.position = spawnPoint.position;
        
        // Match rotation if needed
        playerGameObject.transform.rotation = spawnPoint.rotation;
        
        yield return new WaitForSeconds(0.1f);
        EnablePlayer();
        Debug.Log("Player teleported to spawn point: " + spawnPoint.position); 
    }
    
    // Teleports the player to the assigned spawn point
    public void TeleportToSpawnPoint()
    {
        StartCoroutine(TeleportPlayer());
    }

    public void DisablePlayer()
    {
        // First disable the controller to prevent physics interference
        characterController.enabled = false;
        playerGameObject.SetActive(false);
    }

    private void EnablePlayer()
    {
        playerGameObject.SetActive(true);
        // Re-enable the character controller
        characterController.enabled = true;
    }
}