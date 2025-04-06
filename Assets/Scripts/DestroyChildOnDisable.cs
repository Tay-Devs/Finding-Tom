using UnityEngine;

public class DestroyChildOnDisable : MonoBehaviour
{
    private void OnDisable()
    {
        // Check if there's at least one child
        if (transform.childCount > 0)
        {
            // Get the first child
            Transform child = transform.GetChild(0);
            
            // Destroy the child GameObject
            Destroy(child.gameObject);
        }
    }
}