using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem.Controls;

public enum InputDeviceType
{
    Keyboard,
    PlayStation,
    Xbox,
    ProController,
    Unknown
}

// Central manager for handling input device detection and UI updates
public class InputDisplayManager : MonoBehaviour
{
    public static InputDisplayManager Instance { get; private set; }

    [SerializeField] private float deviceCheckInterval = 1.0f;
    [SerializeField] private string inputUILayerName = "Input";
    
    [Tooltip("How long to wait for keyboard input before considering switching back")]
    [SerializeField] private float deviceStickinessTime = 5.0f;

    [Tooltip("Enable debug logs for device changes")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Current detected input device
    public InputDeviceType CurrentDevice { get; private set; } = InputDeviceType.Unknown;
    
    // Event fired when input device changes
    public event Action<InputDeviceType> OnDeviceChanged;

    private float _lastCheckTime;
    private int _inputUILayer;
    private Dictionary<string, InputPromptUI> _activePrompts = new Dictionary<string, InputPromptUI>();
    
    // Track last active device and time
    private InputDeviceType _lastActiveDevice = InputDeviceType.Unknown;
    private float _lastGamepadActiveTime;
    private bool _keyboardHasBeenActive = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Cache the layer for efficiency
            _inputUILayer = LayerMask.NameToLayer(inputUILayerName);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Initial device detection
        DetectInputDevice();
        
        // Register to scene load events to find UI elements in new scenes
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Find all input UI elements in the current scene
        FindAndRegisterUIElements();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Find input UI elements when a new scene loads
        FindAndRegisterUIElements();
    }

    private void Update()
    {
        // Check for device changes periodically
        if (Time.time - _lastCheckTime > deviceCheckInterval)
        {
            DetectInputDevice();
            _lastCheckTime = Time.time;
        }
    }

    // Detects the current input device being used
    private void DetectInputDevice()
    {
        InputDeviceType newDevice = CurrentDevice;
        bool gamepadIsActive = false;
        bool keyboardIsActive = false;

        // Check for active gamepad input
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            gamepadIsActive = IsGamepadActive(gamepad);
            
            if (gamepadIsActive)
            {
                // Update the timestamp when gamepad was last active
                _lastGamepadActiveTime = Time.time;
                _lastActiveDevice = DetermineGamepadType(gamepad);
                newDevice = _lastActiveDevice;
            }
        }

        // Check for keyboard/mouse activity
        if (Keyboard.current != null || Mouse.current != null)
        {
            keyboardIsActive = IsKeyboardOrMouseActive();
            if (keyboardIsActive)
            {
                _keyboardHasBeenActive = true;
                _lastActiveDevice = InputDeviceType.Keyboard;
                newDevice = InputDeviceType.Keyboard;
            }
        }

        // If neither input is actively being used, use stickiness logic
        if (!gamepadIsActive && !keyboardIsActive)
        {
            // If we had a gamepad active recently, stick with it
            if (_lastActiveDevice != InputDeviceType.Keyboard && 
                _lastActiveDevice != InputDeviceType.Unknown &&
                Time.time - _lastGamepadActiveTime < deviceStickinessTime)
            {
                // Keep using the last active gamepad device
                newDevice = _lastActiveDevice;
            }
            else if (_keyboardHasBeenActive)
            {
                // Default to keyboard if we've used it before and gamepad stickiness expired
                newDevice = InputDeviceType.Keyboard;
            }
        }

        // If device has changed, update and notify
        if (newDevice != CurrentDevice && newDevice != InputDeviceType.Unknown)
        {
            CurrentDevice = newDevice;
            OnDeviceChanged?.Invoke(CurrentDevice);
            if (enableDebugLogs)
            {
                Debug.Log($"Input device changed to: {CurrentDevice}");
            }
            
            // Update all registered UI elements
            UpdateAllUIElements();
        }
    }

    // Determine the type of gamepad
    private InputDeviceType DetermineGamepadType(Gamepad gamepad)
    {
        string gamepadName = gamepad.name.ToLower();
        
        if (gamepadName.Contains("dualshock") || gamepadName.Contains("playstation"))
        {
            return InputDeviceType.PlayStation;
        }
        else if (gamepadName.Contains("xbox") || gamepadName.Contains("xinput"))
        {
            return InputDeviceType.Xbox;
        }
        else if (gamepadName.Contains("pro controller") || gamepadName.Contains("nintendo"))
        {
            return InputDeviceType.ProController;
        }
        
        // Default to Xbox for generic gamepads as it's the most common format
        return InputDeviceType.Xbox;
    }

    private bool IsGamepadActive(Gamepad gamepad)
    {
        if (gamepad == null) return false;

        // Check for any button press, stick movement, or trigger activation
        foreach (var control in gamepad.allControls)
        {
            if (control is ButtonControl button && button.wasPressedThisFrame)
                return true;
            
            if (control is StickControl stick && stick.ReadValue().magnitude > 0.1f)
                return true;
            
            if (control is AxisControl axis && Math.Abs(axis.ReadValue()) > 0.1f)
                return true;
        }

        return false;
    }

    private bool IsKeyboardOrMouseActive()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        
        // Check for any keyboard key press
        if (keyboard != null)
        {
            foreach (var key in keyboard.allKeys)
            {
                if (key.wasPressedThisFrame)
                    return true;
            }
        }
        
        // Check for mouse movement or button press
        if (mouse != null)
        {
            if (mouse.delta.ReadValue().magnitude > 0.1f)
                return true;
                
            if (mouse.leftButton.wasPressedThisFrame || 
                mouse.rightButton.wasPressedThisFrame || 
                mouse.middleButton.wasPressedThisFrame)
                return true;
        }
        
        return false;
    }

    // Find all UI elements with the InputUI layer and register them
    private void FindAndRegisterUIElements()
    {
        // Find all active input UI elements in the scene
        InputPromptUI[] uiElements = FindObjectsOfType<InputPromptUI>();
        
        foreach (var element in uiElements)
        {
            // Only register elements in the Input layer
            if (element.gameObject.layer == _inputUILayer)
            {
                RegisterUIElement(element);
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Found and registered {_activePrompts.Count} input UI elements");
        }
    }

    // Register a UI element to be updated when the input device changes
    public void RegisterUIElement(InputPromptUI element)
    {
        if (element == null) return;
        
        string key = element.GetInstanceID().ToString();
        
        if (!_activePrompts.ContainsKey(key))
        {
            _activePrompts.Add(key, element);
            // Update the element with current device info
            element.UpdateDisplay(CurrentDevice);
        }
    }

    // Unregister a UI element (e.g., when it's destroyed)
    public void UnregisterUIElement(InputPromptUI element)
    {
        if (element == null) return;
        
        string key = element.GetInstanceID().ToString();
        
        if (_activePrompts.ContainsKey(key))
        {
            _activePrompts.Remove(key);
        }
    }

    // Update all registered UI elements with the current device
    private void UpdateAllUIElements()
    {
        foreach (var element in _activePrompts.Values)
        {
            if (element != null)
            {
                element.UpdateDisplay(CurrentDevice);
            }
        }
    }

    private void OnDestroy()
    {
        // Unregister from scene loading events
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}