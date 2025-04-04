using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DiceCombinationPuzzleManager : MonoBehaviour
{
    [Header("Puzzle Settings")]
    [Tooltip("The dice controller script")]
    public DieController dieController;
    
    [Tooltip("The secret combination to unlock the puzzle")]
    public int[] secretCombination = new int[5] { 1, 2, 3, 4, 5 };
    
    [Header("Feedback Settings")]
    [Tooltip("Time between each die feedback effect (seconds)")]
    public float timeBetweenEffects = 0.5f;
    
    [Tooltip("GameObject that will show correct answer effects")]
    public GameObject correctEffectPrefab;
    
    [Tooltip("GameObject that will show incorrect answer effects")]
    public GameObject incorrectEffectPrefab;
    
    [Tooltip("Additional delay after checking before returning control")]
    public float returnControlDelay = 1.0f;
    
    [Header("Input Settings")]
    [Tooltip("Input Action Asset with the 'Select' action")]
    public InputActionAsset inputActions;
    
    [Tooltip("Name of the action map containing the Select action")]
    public string actionMapName = "Gameplay";
    
    [Tooltip("Name of the action to check combination")]
    public string selectActionName = "Select";
    
    [Tooltip("Fallback key in case input system fails")]
    public KeyCode fallbackKey = KeyCode.E;
    
    [Header("State Management")]
    [Tooltip("Player GameObject to enable/disable during checking")]
    public PlayerStateControl playerStateControl;
    
    // Input action references
    private InputAction selectAction;
    private InputActionMap actionMap;
    private bool inputSystemInitialized = false;
    
    private bool isPuzzleSolved = false;
    private bool isCheckingCombination = false;
    private Coroutine checkCoroutine;
    
    // Track original selection to restore it
    private int originalSelectedDieIndex = -1;
    
    [SerializeField]
    private UnityEvent puzzleCompleted;

    private bool isCheckOnCooldown;
    
    private void Awake()
    {
        // Validate references
        if (dieController == null)
        {
            Debug.LogError("DieController reference is missing! Please assign it in the Inspector.");
            enabled = false;
            return;
        }
        
        // Set up input actions
        inputSystemInitialized = SetupInputActions();
        
        if (!inputSystemInitialized)
        {
            Debug.LogWarning("Input system not initialized correctly. Using fallback key " + fallbackKey.ToString());
        }
    }

    private void Start()
    {
        // Ensure outline is visible on the initial die
        if (dieController != null && dieController.diceObjects.Count > 0)
        {
            // Force update outline on first selected die
            dieController.UpdateDieOutline();
        }
    }
    
    private bool SetupInputActions()
    {
        if (inputActions == null)
        {
            Debug.LogError("Input Actions asset not assigned! Using fallback key instead.");
            return false;
        }
        
        try
        {
            // Try to find the specified action map
            actionMap = inputActions.FindActionMap(actionMapName);
            
            // If not found, try to use the first available action map
            if (actionMap == null)
            {
                if (inputActions.actionMaps.Count > 0)
                {
                    actionMap = inputActions.actionMaps[0];
                    Debug.LogWarning("Action map '" + actionMapName + "' not found. Using '" + actionMap.name + "' instead.");
                }
                else
                {
                    Debug.LogError("No action maps found in the input actions asset!");
                    return false;
                }
            }
            
            // Find the Select action
            selectAction = actionMap.FindAction(selectActionName);
            
            if (selectAction == null)
            {
                Debug.LogError("Could not find '" + selectActionName + "' action in the action map!");
                return false;
            }
            
            // Register callback and enable
            selectAction.performed += OnSelect;
            
            // Enable the action map
            actionMap.Enable();
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error setting up input actions: " + e.Message);
            return false;
        }
    }
    
    private void Update()
    {
        // Fallback input detection if input system isn't working
        if (!inputSystemInitialized && Input.GetKeyDown(fallbackKey) && !isCheckingCombination)
        {
            Debug.Log("Fallback key detected: " + fallbackKey.ToString());
            CheckCombinationManually();
        }
    }
    
    private void OnSelect(InputAction.CallbackContext context)
    {
        // Only respond if not already checking
        if (context.performed && !isCheckingCombination)
        {
            
            // Cancel any running check
            if (checkCoroutine != null)
            {
                StopCoroutine(checkCoroutine);
            }

            if (isCheckOnCooldown)
            {
                return;
            }
            // Start a new check
            checkCoroutine = StartCoroutine(CheckCombination());
        }
    }
    
    private void CheckCombinationManually()
    {
        if (!isCheckingCombination)
        {
            // Cancel any running check
            if (checkCoroutine != null)
            {
                StopCoroutine(checkCoroutine);
            }
            if (isCheckOnCooldown)
            {
                return;
            }
            // Start a new check
            checkCoroutine = StartCoroutine(CheckCombination());
        }
    }
    
    // Disable all die interactions and hide outline
    private void DisableDicePuzzleState()
    {
        if (dieController != null)
        {
            
            // Save the current selected die index to restore later if needed
            originalSelectedDieIndex = dieController.currentDieIndex;
            
            // Disable die outline by calling DisableTowerOutline on all dice
            HideAllDiceOutlines();
            
            // Disable input to prevent value changes and selection movement
            DisableDiceInput();
        }
    }
    
    // Re-enable die interactions and show outline
    private void EnableDicePuzzleState()
    {
        if (dieController != null && !isPuzzleSolved)
        {
            Debug.Log("Enabling dice puzzle state");
            
            // Re-enable the die input
            EnableDiceInput();
            
            // Restore the selected die outline if we have a valid index
            if (originalSelectedDieIndex >= 0 && originalSelectedDieIndex < dieController.diceObjects.Count)
            {
                // Set the die controller's current die index back to the original
                RestoreSelectedDieOutline();
            }
        }
    }
    
    // Hide all dice outlines 
    private void HideAllDiceOutlines()
    {
        // Call a method to hide outlines in the die controller
        // This will use the accessibility of your DieController class members
        
        // First save the current selected die 
        if (dieController.diceObjects.Count > 0 && dieController.currentDieIndex >= 0 && 
            dieController.currentDieIndex < dieController.diceObjects.Count)
        {
            originalSelectedDieIndex = dieController.currentDieIndex;
            
            // Access the outline dictionary and disable all outlines
            foreach (Transform die in dieController.diceObjects)
            {
                dieController.DisableDieOutline(die);
            }
        }
    }
    
    // Restore the selected die outline
    private void RestoreSelectedDieOutline()
    {
        if (originalSelectedDieIndex >= 0 && originalSelectedDieIndex < dieController.diceObjects.Count)
        {
            // Set the current die index back
            dieController.currentDieIndex = originalSelectedDieIndex;
            
            // Update the outline
            dieController.UpdateDieOutline();
        }
    }
    
    // Disable the dice input
    private void DisableDiceInput()
    {
        // Temporarily disable all input in the die controller
        if (dieController.inputActions != null)
        {
            foreach (InputActionMap map in dieController.inputActions.actionMaps)
            {
                map.Disable();
            }
        }
    }
    
    // Re-enable the dice input
    private void EnableDiceInput()
    {
        // Re-enable all input in the die controller
        if (dieController.inputActions != null)
        {
            foreach (InputActionMap map in dieController.inputActions.actionMaps)
            {
                map.Enable();
            }
        }
    }
    
    // Toggle player movement state (switching to moving state)
    private void ReturnToPlayerFromPuzzle()
    {
        if (playerStateControl != null)
        {
            playerStateControl.SetPlayerState(PlayerStateControl.PlayerState.Moving);
            Debug.Log("Returned to player movement state");
        }
    }
    
    private IEnumerator CheckCombination()
    {
        
        // Set state to checking
        isCheckingCombination = true;
        
        // Disable dice puzzle state (hide outline, disable interaction)
        DisableDicePuzzleState();
        
        // Get current dice values
        int[] currentValues = new int[dieController.diceObjects.Count];
        for (int i = 0; i < dieController.diceObjects.Count; i++)
        {
            currentValues[i] = dieController.GetDieValue(i);
        }
        
        // Check if there are enough dice for the combination
        if (currentValues.Length < secretCombination.Length)
        {
            
            // Return control
            isCheckingCombination = false;
            
            // We'll only re-enable dice in OnEnable now
            // EnableDicePuzzleState(); 
            
            yield break;
        }
        
        bool allCorrect = true;
        
        // Check each die one by one with a delay between
        for (int i = 0; i < secretCombination.Length; i++)
        {
            bool isCorrect = currentValues[i] == secretCombination[i];
           
            // Show the appropriate effect
            ShowEffect(dieController.diceObjects[i], isCorrect);
            
            // Update the overall result
            if (!isCorrect)
            {
                allCorrect = false;
            }
            
            // Wait before checking the next die
            yield return new WaitForSeconds(timeBetweenEffects);
        }
        
        // If all correct, mark the puzzle as solved
        if (allCorrect)
        {
            isPuzzleSolved = true;
            
            puzzleCompleted.Invoke();
            // Additional puzzle-solved effects could be triggered here
        }

        
        // Wait a bit before returning control
        yield return new WaitForSeconds(returnControlDelay);
        
        // Additional delay of 0.5 seconds before returning to player movement
        yield return new WaitForSeconds(0.5f);
        
        // Return control based on puzzle state
        isCheckingCombination = false;
        
        // Switch to player movement
        ReturnToPlayerFromPuzzle();
        
        // We won't restore dice puzzle state here anymore - it will be handled in OnEnable
        // if (!isPuzzleSolved)
        // {
        //     EnableDicePuzzleState();
        // }
        
       
    }
    
    private void ShowEffect(Transform dieTransform, bool isCorrect)
    {
        // Create the appropriate effect at the die's position
        GameObject effectPrefab = isCorrect ? correctEffectPrefab : incorrectEffectPrefab;
        
        if (effectPrefab != null)
        {
            // Instantiate the effect slightly above the die
            Vector3 effectPosition = dieTransform.position + Vector3.up * 0.5f;
            GameObject effectInstance = Instantiate(effectPrefab, effectPosition, Quaternion.identity);
            
            // Parent to the die so it moves with it
            effectInstance.transform.SetParent(dieTransform);
            
            // Get the effect controller
            DieEffectController effectController = effectInstance.GetComponent<DieEffectController>();
            if (effectController != null)
            {
                effectController.PlayEffect();
            }
            else
            {
                // If no controller, destroy after a few seconds
                Debug.LogWarning("No DieEffectController component on effect prefab!");
                Destroy(effectInstance, 2f);
            }
        }
        else
        {
            Debug.LogWarning("No effect prefab assigned for " + (isCorrect ? "correct" : "incorrect") + " answer!");
        }
    }
    
    private void OnEnable()
    {
        StartCoroutine(AddCooldownForStartCheck());
        if (inputSystemInitialized && actionMap != null)
        {
            actionMap.Enable();
        }
    
        // Reset checking state
        isCheckingCombination = false;
    
        // Only re-enable dice control if puzzle is not solved
        if (!isPuzzleSolved)
        {
            // Re-enable dice input first
            EnableDiceInput();
        
            // Always select the leftmost die (index 0) when the component is enabled
            if (dieController != null && dieController.diceObjects.Count > 0)
            {
                // Set the current die index to 0 (leftmost)
                dieController.currentDieIndex = 0;
            
                // Update the outline to make it visible
                dieController.UpdateDieOutline();
            }
        }
    }
    
    private void OnDisable()
       {
           if (inputSystemInitialized && actionMap != null)
           {
               actionMap.Disable();
           }
       }
    
    private void OnDestroy()
    {
        // Clean up callbacks
        if (inputSystemInitialized && selectAction != null)
        {
            selectAction.performed -= OnSelect;
        }
    }
    
    // Public method to reset the puzzle if needed
    public void ResetPuzzle()
    {
        isPuzzleSolved = false;
        isCheckingCombination = false;
        EnableDicePuzzleState();
    }
    IEnumerator AddCooldownForStartCheck()
    {
        isCheckOnCooldown = true;
        yield return new WaitForSeconds(2f);
        isCheckOnCooldown = false;
    }
}