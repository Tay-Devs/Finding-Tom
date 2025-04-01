using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class MenuNavigationController : MonoBehaviour
{
    [System.Serializable]
    public class MenuGroup
    {
        public string groupName;
        public GameObject menuPanel;
        public Selectable firstSelectedButton;
        public MenuGroup previousGroup; // Optional: for nested menus to return to previous menu
    }

    [Header("Menu Groups")]
    [SerializeField] private List<MenuGroup> menuGroups = new List<MenuGroup>();
    
    [Header("Input System")]
    [SerializeField] private InputActionReference navigationAction;
    [SerializeField] private InputActionReference submitAction;
    [SerializeField] private InputActionReference cancelAction;
    
    [Header("Navigation Settings")]
    [SerializeField] private float inputDelay = 0.2f;
    
    private MenuGroup currentMenuGroup;
    private Selectable currentSelectedButton;
    private float lastInputTime;
    
    private void Awake()
    {
        // Disable all menu groups initially
        foreach (var group in menuGroups)
        {
            if (group.menuPanel != null)
            {
                group.menuPanel.SetActive(false);
            }
        }
    }
    
    private void OnEnable()
    {
        // Enable input actions
        if (navigationAction != null && navigationAction.action != null)
        {
            navigationAction.action.Enable();
        }
        
        if (submitAction != null && submitAction.action != null)
        {
            submitAction.action.Enable();
            submitAction.action.performed += OnSubmitPerformed;
        }
        
        if (cancelAction != null && cancelAction.action != null)
        {
            cancelAction.action.Enable();
            cancelAction.action.performed += OnCancelPerformed;
        }
    }
    
    private void OnDisable()
    {
        // Disable input actions
        if (navigationAction != null && navigationAction.action != null)
        {
            navigationAction.action.Disable();
        }
        
        if (submitAction != null && submitAction.action != null)
        {
            submitAction.action.Disable();
            submitAction.action.performed -= OnSubmitPerformed;
        }
        
        if (cancelAction != null && cancelAction.action != null)
        {
            cancelAction.action.Disable();
            cancelAction.action.performed -= OnCancelPerformed;
        }
    }
    
    private void Update()
    {
        // Only process navigation when we have an active menu group
        if (currentMenuGroup == null || navigationAction == null)
            return;
            
        // Read input value from the navigation action (expected to be a Vector2)
        Vector2 navigationInput = navigationAction.action.ReadValue<Vector2>();
        
        // Only process input if there's significant movement and enough time has passed
        if (navigationInput.magnitude > 0.5f && Time.unscaledTime - lastInputTime > inputDelay)
        {
            NavigateUI(navigationInput);
            lastInputTime = Time.unscaledTime;
        }
        
        // If nothing is selected but we have a current menu group, select its first button
        if (EventSystem.current.currentSelectedGameObject == null && currentMenuGroup?.firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(currentMenuGroup.firstSelectedButton.gameObject);
            currentSelectedButton = currentMenuGroup.firstSelectedButton;
        }
    }
    
    private void NavigateUI(Vector2 direction)
    {
        // Get the currently selected UI element
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return;
            
        Selectable current = selected.GetComponent<Selectable>();
        if (current == null)
            return;
            
        // Determine navigation direction based on input
        Selectable nextSelectable = null;
        
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Horizontal navigation
            if (direction.x > 0)
                nextSelectable = current.FindSelectableOnRight();
            else
                nextSelectable = current.FindSelectableOnLeft();
        }
        else
        {
            // Vertical navigation
            if (direction.y > 0)
                nextSelectable = current.FindSelectableOnUp();
            else
                nextSelectable = current.FindSelectableOnDown();
        }
        
        // If we found a button to navigate to, select it
        if (nextSelectable != null)
        {
            EventSystem.current.SetSelectedGameObject(nextSelectable.gameObject);
            currentSelectedButton = nextSelectable;
            
            // Optional: Play a sound or animation here
        }
    }
    
    private void OnSubmitPerformed(InputAction.CallbackContext context)
    {
        // Get the currently selected UI element and simulate a click
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return;
            
        Button button = selected.GetComponent<Button>();
        if (button != null && button.IsInteractable())
        {
            button.onClick.Invoke();
            
            // Optional: Play a selection sound
        }
    }
    
    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        // Go back to the previous menu group if one exists
        if (currentMenuGroup != null && currentMenuGroup.previousGroup != null)
        {
            ShowMenuGroup(currentMenuGroup.previousGroup.groupName);
            
            // Optional: Play a back/cancel sound
        }
    }
    
    // Public method to open a specific menu group by name
    public void ShowMenuGroup(string groupName)
    {
        MenuGroup targetGroup = menuGroups.Find(g => g.groupName == groupName);
        
        if (targetGroup != null)
        {
            // Hide current menu group if exists
            if (currentMenuGroup != null && currentMenuGroup.menuPanel != null)
            {
                currentMenuGroup.menuPanel.SetActive(false);
            }
            
            // Show new menu group
            targetGroup.menuPanel.SetActive(true);
            currentMenuGroup = targetGroup;
            
            // Select the first button in this group
            if (targetGroup.firstSelectedButton != null)
            {
                EventSystem.current.SetSelectedGameObject(targetGroup.firstSelectedButton.gameObject);
                currentSelectedButton = targetGroup.firstSelectedButton;
            }
            
            Debug.Log($"Showing menu group: {groupName}");
        }
        else
        {
            Debug.LogWarning($"Menu group '{groupName}' not found!");
        }
    }
    
    // Helper method to set up a menu button that opens another menu group
    public void SetupMenuButton(Button button, string targetGroupName)
    {
        if (button != null)
        {
            // Find the target group
            MenuGroup targetGroup = menuGroups.Find(g => g.groupName == targetGroupName);
            
            if (targetGroup != null)
            {
                // Clear previous listeners
                button.onClick.RemoveAllListeners();
                
                // Add listener to show the target group
                button.onClick.AddListener(() => ShowMenuGroup(targetGroupName));
            }
            else
            {
                Debug.LogWarning($"Target menu group '{targetGroupName}' not found for button setup!");
            }
        }
    }
    
    // Public method to show the first menu group (useful for initialization)
    public void ShowFirstMenuGroup()
    {
        if (menuGroups.Count > 0)
        {
            ShowMenuGroup(menuGroups[0].groupName);
        }
    }
}