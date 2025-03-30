using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class InputUIElement : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("The scriptable object containing the input action data")]
    [SerializeField] private InputActionData inputData;
    
    [Header("UI Components")]
    [Tooltip("Image component to display the input icon (will use this GameObject's Image if not set)")]
    [SerializeField] private Image iconImage;
    
    [Tooltip("Optional text component to display the action name")]
    [SerializeField] private TMP_Text actionNameText;
    
    [Tooltip("Optional text component to display the binding (e.g., 'E' or 'X button')")]
    [SerializeField] private TMP_Text bindingText;
    
    [Header("Settings")]
    [Tooltip("Should this update automatically when the input type changes?")]
    [SerializeField] private bool autoUpdate = true;
    
    private void Awake()
    {
        // If no icon image is assigned, try to get it from this GameObject
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to input type change events if we want auto-updates
        if (autoUpdate)
        {
            InputManager.OnInputTypeChanged += UpdateVisuals;
        }
        
        // Initial update
        UpdateVisuals();
    }
    
    private void OnDisable()
    {
        if (autoUpdate)
        {
            InputManager.OnInputTypeChanged -= UpdateVisuals;
        }
    }
    
    // Update the visual elements based on the current input type
    public void UpdateVisuals()
    {
        if (inputData == null)
        {
            Debug.LogError("No InputActionData assigned to " + gameObject.name);
            return;
        }
        
        // Get the current input type
        string currentInputType = InputTypeSelector.GetCurrentInputType();
        
        // Update the icon
        if (iconImage != null)
        {
            Sprite icon = inputData.GetSpriteForInputType(currentInputType);
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false; // Hide if no icon found
            }
        }
        
        // Update the action name text
        if (actionNameText != null)
        {
            actionNameText.text = inputData.actionName;
        }
        
        // Update the binding text
        if (bindingText != null)
        {
            bindingText.text = inputData.GetBindingDisplayString(currentInputType);
        }
    }
    
    // Method to manually force an update (e.g., after changing settings)
    public void ForceUpdate()
    {
        UpdateVisuals();
    }
}