using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DieController : MonoBehaviour
{
    [Header("Dice Settings")]
    [Tooltip("List of all dice that can be controlled")]
    public List<Transform> diceObjects = new List<Transform>();
    
    [Header("Dice Value Settings")]
    [Tooltip("Minimum value for dice")]
    public int minValue = 1;
    
    [Tooltip("Maximum value for dice")]
    public int maxValue = 6;
    
    [Header("Animation Settings")]
    [Tooltip("Time in seconds for dice rotation to complete")]
    [Range(0.1f, 5f)]
    public float rotationSpeed = 1f;
    
    [Header("Outline Settings")]
    [Tooltip("Outline color for the selected die")]
    public Color outlineColor = Color.yellow;
    
    [Tooltip("Outline width for the selected die")]
    [Range(0f, 10f)]
    public float outlineWidth = 5f;
    
    [Header("Input Settings")]
    [Tooltip("Reference to an Input Action asset with move and changeValue actions")]
    public InputActionAsset inputActions;
    
    [SerializeField]
    private PauseController pauseController;
    // Input action references
    private InputAction moveAction;
    private InputAction changeValueAction;
    private InputAction printAction;
    
    // Currently selected die index
    public int currentDieIndex = 0;
    
    // Dice values
    private int[] diceValues;
    
    // Target rotations for each die
    private Quaternion[] targetRotations;
    private Quaternion[] startRotations;
    private float[] rotationTimers;
    private bool[] isRotating;
    
    // Store monobehaviours for each die that handle outlines
    private Dictionary<Transform, MonoBehaviour> diceOutlines = new Dictionary<Transform, MonoBehaviour>();
    
    // Input system values
    private Vector2 moveInput = Vector2.zero;
    private Vector2 moveInputPrev = Vector2.zero;
    private Vector2 changeValueInput = Vector2.zero;
    

    
    private void Awake()
    {
        // Initialize dice values and rotations
        diceValues = new int[diceObjects.Count];
        targetRotations = new Quaternion[diceObjects.Count];
        startRotations = new Quaternion[diceObjects.Count];
        rotationTimers = new float[diceObjects.Count];
        isRotating = new bool[diceObjects.Count];
        
        for (int i = 0; i < diceValues.Length; i++)
        {
            diceValues[i] = minValue;
            targetRotations[i] = Quaternion.Euler(0, 0, 0); // Default to showing "1"
            rotationTimers[i] = 0f;
            isRotating[i] = false;
            
            // Initialize the dice to show "1"
            if (diceObjects[i] != null)
            {
                diceObjects[i].rotation = Quaternion.Euler(0, 0, 0);
            }
        }
        
        // Set up outlines for each die
        foreach (Transform die in diceObjects)
        {
            if (die != null)
            {
                // Try to find an Outline component (from Quick Outline package)
                MonoBehaviour outlineComponent = FindOutlineComponent(die.gameObject);
                
                if (outlineComponent != null)
                {
                    // Store the outline component
                    diceOutlines[die] = outlineComponent;
                    
                    // Configure and disable initially
                    SetOutlineProperties(outlineComponent, outlineColor, outlineWidth);
                    EnableOutline(outlineComponent, false);
                }
                else
                {
                    Debug.LogWarning("No Outline component found on " + die.name + ". Make sure you've added the Quick Outline component to this object.");
                }
            }
     
        }
        
        // Set up input actions from the asset
        SetupInputActions();
        
        // Select the first die by default if we have any
        if (diceObjects.Count > 0)
        {
            currentDieIndex = 0;
            UpdateDieOutline();
        }
    }
    
    // Set up input actions using the asset
    private void SetupInputActions()
    {
        if (inputActions != null)
        {
            // Try to find the Gameplay action map (common naming convention)
            InputActionMap actionMap = inputActions.FindActionMap("Gameplay");
            
            // If not found, try to use the first available action map
            if (actionMap == null && inputActions.actionMaps.Count > 0)
            {
                actionMap = inputActions.actionMaps[0];
            }
            
            if (actionMap != null)
            {
                // Find the necessary actions
                moveAction = actionMap.FindAction("Move");
                changeValueAction = actionMap.FindAction("ChangeValue");
                printAction = actionMap.FindAction("Print");
                
                // Register callbacks
                if (moveAction != null)
                {
                    moveAction.performed += OnMove;
                    moveAction.canceled += OnMove;
                }
                
                if (changeValueAction != null)
                {
                    changeValueAction.performed += OnChangeValue;
                    changeValueAction.canceled += OnChangeValue;
                }
                
                if (printAction != null)
                {
                    printAction.performed += OnPrint;
                }
                
                // Enable the action map
                actionMap.Enable();
            }
            else
            {
                Debug.LogError("No action maps found in the input actions asset!");
            }
        }
        else
        {
            Debug.LogError("Input Actions asset not assigned! Add a reference to an Input Action Asset in the Inspector.");
        }
    }
    
    // Helper method to find any Outline component on the given game object
    private MonoBehaviour FindOutlineComponent(GameObject obj)
    {
        // Try to find a component named "Outline" using GetComponents
        MonoBehaviour[] components = obj.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component.GetType().Name == "Outline")
            {
                return component;
            }
        }
        
        // If no Outline component found, try to add one
        // Note: This will only work if Quick Outline is properly imported
        try
        {
            // Use reflection to create the component without direct reference
            System.Type outlineType = System.Type.GetType("Outline, Assembly-CSharp");
            if (outlineType != null)
            {
                return obj.AddComponent(outlineType) as MonoBehaviour;
            }
            else
            {
                // Try alternative namespace
                outlineType = System.Type.GetType("QuickOutline.Outline, Assembly-CSharp");
                if (outlineType != null)
                {
                    return obj.AddComponent(outlineType) as MonoBehaviour;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error adding Outline component: " + e.Message);
        }
        
        return null;
    }
    
    // Helper method to set outline properties using reflection
    private void SetOutlineProperties(MonoBehaviour outline, Color color, float width)
    {
        try
        {
            // Use reflection to set properties without direct reference
            System.Reflection.PropertyInfo colorProperty = outline.GetType().GetProperty("OutlineColor");
            System.Reflection.PropertyInfo widthProperty = outline.GetType().GetProperty("OutlineWidth");
            
            if (colorProperty != null)
            {
                colorProperty.SetValue(outline, color);
            }
            
            if (widthProperty != null)
            {
                widthProperty.SetValue(outline, width);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error setting outline properties: " + e.Message);
        }
    }
    
    // Helper method to enable/disable the outline
    private void EnableOutline(MonoBehaviour outline, bool enabled)
    {
        if (outline != null)
        {
            outline.enabled = enabled;
        }
    }
    
    private void OnMove(InputAction.CallbackContext context)
    {
        //If the game is paused don't select a different dice
        if (pauseController.isPaused)
        {
            return;
        }
        
        if (context.canceled)
        {
            moveInput = Vector2.zero;
        }
        else if (context.performed)
        {
            moveInput = context.ReadValue<Vector2>();
        }
    }
    
    private void OnChangeValue(InputAction.CallbackContext context)
    {
        //If the game is paused don't change value
        if (pauseController.isPaused)
        {
            return;
        }
       
        if (context.canceled)
        {
            changeValueInput = Vector2.zero;
        }
        else if (context.performed)
        {
            changeValueInput = context.ReadValue<Vector2>();
            
            // Only process if there's a significant y-input
            if (Mathf.Abs(changeValueInput.y) > 0.5f)
            {
                if (changeValueInput.y > 0.5f)
                {
                    IncreaseDieValue();
                }
                else if (changeValueInput.y < -0.5f)
                {
                    DecreaseDieValue();
                }
            }
        } 
        
        
    }
    
    private void OnPrint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PrintDiceValues();
        }
    }
    
    private void Update()
    {
        // Handle die selection based on the move input
        HandleDieSelection();
        
        // Update the rotations of the dice
        UpdateDiceRotations();
    }
    
    private void UpdateDiceRotations()
    {
        // Update all dice rotations
        for (int i = 0; i < diceObjects.Count; i++)
        {
            if (diceObjects[i] != null && isRotating[i])
            {
                // Increment the timer
                rotationTimers[i] += Time.deltaTime;
                
                // Calculate the lerp factor (0 to 1) based on the timer and rotation speed
                float t = Mathf.Clamp01(rotationTimers[i] / rotationSpeed);
                
                // Apply the rotation using Slerp
                diceObjects[i].rotation = Quaternion.Slerp(startRotations[i], targetRotations[i], t);
                
                // Check if rotation is complete
                if (t >= 1.0f)
                {
                    // Rotation complete, set to exact target and stop rotating
                    diceObjects[i].rotation = targetRotations[i];
                    isRotating[i] = false;
                }
            }
        }
    }
    
    // Handle die selection based on move input
    private void HandleDieSelection()
    {
        // Process changes in horizontal movement for die selection
        if (Mathf.Abs(moveInput.x - moveInputPrev.x) > 0.5f)
        {
            int previousIndex = currentDieIndex;
            
            // Detect direction change
            bool moveLeft = moveInput.x < -0.5f && moveInputPrev.x >= -0.5f;
            bool moveRight = moveInput.x > 0.5f && moveInputPrev.x <= 0.5f;
            
            // Change selected die based on direction
            if (moveLeft && currentDieIndex > 0)
            {
                currentDieIndex--;
            }
            else if (moveRight && currentDieIndex < diceObjects.Count - 1)
            {
                currentDieIndex++;
            }
            
            // Update die outline if selection changed
            if (previousIndex != currentDieIndex)
            {
                // Disable outline on the previously selected die
                if (previousIndex >= 0 && previousIndex < diceObjects.Count)
                {
                    Transform previousDie = diceObjects[previousIndex];
                    DisableDieOutline(previousDie);
                }
                
                // Update the die outline
                UpdateDieOutline();
            }
        }
        
        // Store current value for next frame comparison
        moveInputPrev = moveInput;
    }
    
    private void IncreaseDieValue()
    {
        if (diceObjects.Count > 0 && currentDieIndex >= 0 && currentDieIndex < diceObjects.Count)
        {
            diceValues[currentDieIndex]++;
            if (diceValues[currentDieIndex] > maxValue)
            {
                diceValues[currentDieIndex] = minValue;
            }
            
            UpdateDieVisual(currentDieIndex);
        }
    }
    
    private void DecreaseDieValue()
    {
        if (diceObjects.Count > 0 && currentDieIndex >= 0 && currentDieIndex < diceObjects.Count)
        {
            diceValues[currentDieIndex]--;
            if (diceValues[currentDieIndex] < minValue)
            {
                diceValues[currentDieIndex] = maxValue;
            }
            
            UpdateDieVisual(currentDieIndex);
        }
    }
    
    public void ResetDiceValue(Transform dieTransform)
    {
        // Check if the transform is in our dice collection
        if (dieTransform != null && diceObjects.Contains(dieTransform))
        {
            int dieIndex = diceObjects.IndexOf(dieTransform);
        
            // Set the value to 1 (minimum value)
            diceValues[dieIndex] = minValue;
        
            // Store the current rotation
            startRotations[dieIndex] = dieTransform.rotation;
        
            // Set the target rotation to show value 1 (0,0,0)
            targetRotations[dieIndex] = Quaternion.Euler(0, 0, 0);
        
            // Reset timer and mark the die as rotating
            rotationTimers[dieIndex] = 0f;
            isRotating[dieIndex] = true;
        }
    }
    private void PrintDiceValues()
    {
        string values = "Dice Values: ";
        for (int i = 0; i < diceValues.Length; i++)
        {
            values += "Die " + (i + 1) + ": " + diceValues[i];
            if (i < diceValues.Length - 1)
            {
                values += ", ";
            }
        }
        
        Debug.Log(values);
    }
    
    // Update the target rotation for a specific die based on its value
    private void UpdateDieVisual(int dieIndex)
    {
        if (dieIndex >= 0 && dieIndex < diceObjects.Count && diceObjects[dieIndex] != null)
        {
            int value = diceValues[dieIndex];
            Transform die = diceObjects[dieIndex];
            
            // Set the target rotation based on the die value using the provided rotation values
            Vector3 targetEuler = Vector3.zero;
            
            switch (value)
            {
                case 1:
                    targetEuler = new Vector3(0, 0, 0);
                    break;
                case 2:
                    targetEuler = new Vector3(90, 0, 0);
                    break;
                case 3:
                    targetEuler = new Vector3(90, 0, -90);
                    break;
                case 4:
                    targetEuler = new Vector3(90, 0, 90);
                    break;
                case 5:
                    targetEuler = new Vector3(90, 0, 180);
                    break;
                case 6:
                    targetEuler = new Vector3(180, 0, 180);
                    break;
            }
            
            // Store the current and target rotations
            startRotations[dieIndex] = die.rotation;
            targetRotations[dieIndex] = Quaternion.Euler(targetEuler);
            
            // Reset timer and mark the die as rotating
            rotationTimers[dieIndex] = 0f;
            isRotating[dieIndex] = true;
        }
    }
    
    public void UpdateDieOutline()
    {
        if (diceObjects.Count > 0 && currentDieIndex >= 0 && currentDieIndex < diceObjects.Count)
        {
            Transform currentDie = diceObjects[currentDieIndex];
            
            if (currentDie != null && diceOutlines.ContainsKey(currentDie))
            {
                // Get the outline component
                MonoBehaviour outline = diceOutlines[currentDie];
                
                // Update outline properties in case they were changed in the Inspector
                SetOutlineProperties(outline, outlineColor, outlineWidth);
                
                // Enable the outline
                EnableOutline(outline, true);
            }
        }
    }
    
    public void DisableDieOutline(Transform die)
    {
        if (die != null && diceOutlines.ContainsKey(die))
        {
            // Disable the outline component
            MonoBehaviour outline = diceOutlines[die];
            EnableOutline(outline, false);
        }
    }
    
    private void OnEnable()
    {
        // Enable input actions if they exist
        InputActionMap actionMap = inputActions?.FindActionMap("Gameplay");
        if (actionMap != null) actionMap.Enable();
    }


    private void OnDisable()
    {
        // Disable input actions if they exist
        InputActionMap actionMap = inputActions?.FindActionMap("Gameplay");
        if (actionMap != null) actionMap.Disable();
        
        // Disable all outlines
        foreach (Transform die in diceObjects)
        {
            DisableDieOutline(die);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up callbacks
        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
        }
        
        if (changeValueAction != null)
        {
            changeValueAction.performed -= OnChangeValue;
            changeValueAction.canceled -= OnChangeValue;
        }
        
        if (printAction != null)
        {
            printAction.performed -= OnPrint;
        }
        
        // Disable all outlines
        foreach (Transform die in diceObjects)
        {
            DisableDieOutline(die);
        }
    }
    public int GetDieValue(int index)
    {
        if (index >= 0 && index < diceValues.Length)
        {
            return diceValues[index];
        }
        return -1; // Invalid index
    }
}