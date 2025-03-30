using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// Displays the correct input prompt based on the current input device
public class InputPromptUI : MonoBehaviour
{
    [Tooltip("Reference to the Input Action that this UI represents")]
    [SerializeField] private InputActionReference inputAction;
    
    [Tooltip("Optional explicit action name (use this instead of auto-detection)")]
    [SerializeField] private string explicitActionName = "";
    
    [Tooltip("Type of UI display (Text, Image, or Both)")]
    [SerializeField] private InputPromptType promptType = InputPromptType.Both;
    
    [Tooltip("Text component to update (leave empty if not using text)")]
    [SerializeField] private TMP_Text promptText;
    
    [Tooltip("Image component to update (leave empty if not using image)")]
    [SerializeField] private Image promptImage;
    
    [Header("Sprite Sets")]
    [Tooltip("Sprites for Keyboard prompts")]
    [SerializeField] private InputSpriteSet keyboardSprites;
    
    [Tooltip("Sprites for PlayStation prompts")]
    [SerializeField] private InputSpriteSet playstationSprites;
    
    [Tooltip("Sprites for Xbox prompts")]
    [SerializeField] private InputSpriteSet xboxSprites;
    
    [Tooltip("Sprites for Nintendo Pro Controller prompts")]
    [SerializeField] private InputSpriteSet proControllerSprites;
    
    [Tooltip("When true, will hide the image if no sprite is found instead of showing an error")]
    [SerializeField] private bool hideImageWhenNoSprite = true;

    private void OnEnable()
    {
        // Register with InputDisplayManager if it exists
        if (InputDisplayManager.Instance != null)
        {
            InputDisplayManager.Instance.RegisterUIElement(this);
        }
    }

    private void OnDisable()
    {
        // Unregister with InputDisplayManager if it exists
        if (InputDisplayManager.Instance != null)
        {
            InputDisplayManager.Instance.UnregisterUIElement(this);
        }
    }

    // Update the display based on the current input device
    public void UpdateDisplay(InputDeviceType deviceType)
    {
        // Get the appropriate sprite set for the current device
        InputSpriteSet spriteSet = GetSpriteSetForDevice(deviceType);
        
        // Get the binding display string from the input action
        string bindingString = string.IsNullOrEmpty(explicitActionName) 
            ? GetBindingDisplayString(deviceType) 
            : explicitActionName;
        
        // Get the action name itself for display purposes
        string actionPath = string.IsNullOrEmpty(explicitActionName) && inputAction != null
            ? inputAction.action.name
            : explicitActionName;

        // Update the text if we're using it
        if ((promptType == InputPromptType.Text || promptType == InputPromptType.Both) && promptText != null)
        {
            promptText.text = bindingString;
        }

        // Update the image if we're using it
        if ((promptType == InputPromptType.Image || promptType == InputPromptType.Both) && promptImage != null && spriteSet != null)
        {
            // Try to find the appropriate sprite for this input, first with binding string
            Sprite sprite = spriteSet.GetSpriteForInput(bindingString);
            
            // If no sprite found, try with action path as a fallback
            if (sprite == null && !string.IsNullOrEmpty(actionPath))
            {
                sprite = spriteSet.GetSpriteForInput(actionPath);
            }
            
            if (sprite != null)
            {
                promptImage.sprite = sprite;
                promptImage.enabled = true;
            }
            else if (hideImageWhenNoSprite)
            {
                // Just hide the image if no sprite is found
                promptImage.enabled = false;
            }
        }
    }

    // Get the appropriate sprite set for the current device
    private InputSpriteSet GetSpriteSetForDevice(InputDeviceType deviceType)
    {
        switch (deviceType)
        {
            case InputDeviceType.Keyboard:
                return keyboardSprites;
            case InputDeviceType.PlayStation:
                return playstationSprites;
            case InputDeviceType.Xbox:
                return xboxSprites;
            case InputDeviceType.ProController:
                return proControllerSprites;
            default:
                return keyboardSprites; // Default to keyboard
        }
    }

    // Get the display string for the current binding based on the device
    private string GetBindingDisplayString(InputDeviceType deviceType)
    {
        if (inputAction == null || inputAction.action == null)
            return "Undefined";

        // Get all bindings for this action
        var bindings = inputAction.action.bindings;
        
        // Find the appropriate binding for the current device type
        for (int i = 0; i < bindings.Count; i++)
        {
            if (IsBindingForDevice(bindings[i], deviceType))
            {
                // Return the display string for this binding
                return InputControlPath.ToHumanReadableString(
                    bindings[i].effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);
            }
        }

        // If no specific binding found, return the first binding as a fallback
        if (bindings.Count > 0)
        {
            return InputControlPath.ToHumanReadableString(
                bindings[0].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        return "Undefined";
    }

    // Determine if a binding is for a specific device type
    private bool IsBindingForDevice(InputBinding binding, InputDeviceType deviceType)
    {
        string path = binding.path.ToLower();
        
        switch (deviceType)
        {
            case InputDeviceType.Keyboard:
                return path.Contains("<keyboard>") || path.Contains("<mouse>");
                
            case InputDeviceType.PlayStation:
                return path.Contains("<dualshock") || path.Contains("<playstation");
                
            case InputDeviceType.Xbox:
                return path.Contains("<gamepad>") || path.Contains("<xbox");
                
            case InputDeviceType.ProController:
                return path.Contains("<procontroller>") || path.Contains("<nintendo");
                
            default:
                return true; // Default matcher
        }
    }
}

// Type of UI display for input prompts
public enum InputPromptType
{
    Text,
    Image,
    Both
}