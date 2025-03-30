using System;
using System.Collections.Generic;
using UnityEngine;

// ScriptableObject that holds sets of input prompt sprites for a specific device type
[CreateAssetMenu(fileName = "New Input Sprite Set", menuName = "Input System/Input Sprite Set")]
public class InputSpriteSet : ScriptableObject
{
    [Tooltip("Device type this sprite set is for")]
    public InputDeviceType deviceType;
    
    [Tooltip("List of input sprites")]
    public List<InputSprite> inputSprites = new List<InputSprite>();

    [Tooltip("When true, will match inputs regardless of path structure")]
    public bool ignoreInputPath = true;
    
    [Tooltip("When true, will not log warnings when sprites are not found")]
    public bool suppressWarnings = true;
    
    // Get the sprite for a specific input binding
    // inputName: Input binding name from Input System
    // Returns: Sprite for that input, or null if not found
    public Sprite GetSpriteForInput(string inputName)
    {
        if (string.IsNullOrEmpty(inputName))
            return null;
            
        // Normalize input name for matching
        inputName = inputName.ToLower().Trim();
        
        // If input contains a path (like "Player/Interact"), get just the action name if enabled
        string actionNameOnly = inputName;
        if (ignoreInputPath && inputName.Contains("/"))
        {
            actionNameOnly = inputName.Substring(inputName.LastIndexOf('/') + 1);
        }

        // Try to find direct match first with full path
        foreach (var inputSprite in inputSprites)
        {
            if (string.Equals(inputSprite.inputName.ToLower(), inputName, StringComparison.OrdinalIgnoreCase))
            {
                return inputSprite.sprite;
            }
        }
        
        // Try to find match with just the action name (if path is being ignored)
        if (ignoreInputPath && inputName.Contains("/"))
        {
            foreach (var inputSprite in inputSprites)
            {
                if (string.Equals(inputSprite.inputName.ToLower(), actionNameOnly, StringComparison.OrdinalIgnoreCase))
                {
                    return inputSprite.sprite;
                }
            }
        }
        
        // Try to find partial match if direct match fails
        foreach (var inputSprite in inputSprites)
        {
            // Check if the input name contains one of our input sprite names
            // This helps with compound inputs like "Left Shift + W"
            if (inputName.Contains(inputSprite.inputName.ToLower()))
            {
                return inputSprite.sprite;
            }
            
            // Also check if action name only (without path) matches
            if (ignoreInputPath && actionNameOnly.Contains(inputSprite.inputName.ToLower()))
            {
                return inputSprite.sprite;
            }
        }
        
        // No match found - only log a warning if suppressWarnings is false
        if (!suppressWarnings)
        {
            Debug.LogWarning($"No sprite found for input '{inputName}' in sprite set for {deviceType}");
        }
        return null;
    }
}

// Maps an input name to its sprite representation
[Serializable]
public class InputSprite
{
    [Tooltip("Name of the input binding from the Input System")]
    public string inputName;
    
    [Tooltip("Sprite to display for this input")]
    public Sprite sprite;
}