using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using EasyTextEffects;

public class UINavigationController : MonoBehaviour
{
    [System.Serializable]
    public class MenuCanvas
    {
        public string menuName;
        public Canvas canvas;
        public GameObject firstSelectedButton;
        public MenuCanvas parentMenu; // Reference to parent menu for back navigation
    }

    [Header("Menu Configuration")]
    [SerializeField] private List<MenuCanvas> menuCanvases = new List<MenuCanvas>();
    [SerializeField] private string initialMenuName = "MainMenu";
    [SerializeField] private float inputDelay = 0.2f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference navigationAction;
    [SerializeField] private InputActionReference submitAction;
    [SerializeField] private InputActionReference cancelAction;

    [Header("Text Effects")]
    [SerializeField] private TextEffectManager textEffectManager;

    private EventSystem eventSystem;
    private float lastInputTime = 0f;
    private MenuCanvas currentMenu;
    private Stack<MenuCanvas> menuHistory = new Stack<MenuCanvas>();
    private GameObject lastSelectedButton; // Track the previously selected button

    private void Awake()
    {
        eventSystem = EventSystem.current;
        
        // Initialize menu references
        foreach (MenuCanvas menu in menuCanvases)
        {
            // Disable all canvases initially
            if (menu.canvas != null)
            {
                menu.canvas.gameObject.SetActive(false);
            }
        }
        
        // Open initial menu
        OpenMenu(initialMenuName);
    }

    private void OnEnable()
    {
        // Enable input actions
        navigationAction.action.Enable();
        submitAction.action.Enable();
        cancelAction.action.Enable();

        // Set up action callbacks
        navigationAction.action.performed += OnNavigate;
        submitAction.action.performed += OnSubmit;
        cancelAction.action.performed += OnCancel;
    }

    private void OnDisable()
    {
        // Disable input actions and remove callbacks
        navigationAction.action.Disable();
        submitAction.action.Disable();
        cancelAction.action.Disable();

        navigationAction.action.performed -= OnNavigate;
        submitAction.action.performed -= OnSubmit;
        cancelAction.action.performed -= OnCancel;
    }

    private void Update()
    {
        // Check if selection has changed (handles mouse selection or other ways selection might change)
        if (eventSystem.currentSelectedGameObject != lastSelectedButton)
        {
            HandleSelectionChange(lastSelectedButton, eventSystem.currentSelectedGameObject);
            lastSelectedButton = eventSystem.currentSelectedGameObject;
        }
    }

    private void HandleSelectionChange(GameObject oldSelection, GameObject newSelection)
    {
        // Stop text effect on previously selected button
        if (oldSelection != null)
        {
            TextEffect oldTextEffect = GetTextEffectFromButton(oldSelection);
            if (oldTextEffect != null && textEffectManager != null)
            {
                textEffectManager.StopTextEffect(oldTextEffect);
            }
        }

        // Start text effect on newly selected button
        if (newSelection != null)
        {
            TextEffect newTextEffect = GetTextEffectFromButton(newSelection);
            if (newTextEffect != null && textEffectManager != null)
            {
                textEffectManager.StartTextEffect(newTextEffect);
            }
        }
    }

    private TextEffect GetTextEffectFromButton(GameObject button)
    {
        // First try to find Text component with TextEffect attached in the direct children
        for (int i = 0; i < button.transform.childCount; i++)
        {
            Transform child = button.transform.GetChild(i);
            TextEffect textEffect = child.GetComponent<TextEffect>();
            if (textEffect != null)
            {
                return textEffect;
            }
        }

        // If not found in direct children, search in all children
        return button.GetComponentInChildren<TextEffect>();
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        // Implement navigation debounce to prevent skipping through buttons too quickly
        if (Time.unscaledTime - lastInputTime < inputDelay)
            return;
            
        lastInputTime = Time.unscaledTime;
        
        Vector2 direction = context.ReadValue<Vector2>();
        GameObject currentSelected = eventSystem.currentSelectedGameObject;
        
        if (currentSelected != null)
        {
            Selectable currentSelectable = currentSelected.GetComponent<Selectable>();
            if (currentSelectable != null)
            {
                Selectable nextSelectable = null;
                
                if (direction.y > 0.5f)
                    nextSelectable = currentSelectable.FindSelectableOnUp();
                else if (direction.y < -0.5f)
                    nextSelectable = currentSelectable.FindSelectableOnDown();
                else if (direction.x < -0.5f)
                    nextSelectable = currentSelectable.FindSelectableOnLeft();
                else if (direction.x > 0.5f)
                    nextSelectable = currentSelectable.FindSelectableOnRight();
                
                if (nextSelectable != null)
                {
                    // Store the current selected object before changing selection
                    GameObject oldSelection = currentSelected;
                    
                    // Change the selection
                    eventSystem.SetSelectedGameObject(nextSelectable.gameObject);
                    
                    // Handle text effects for the selection change
                    HandleSelectionChange(oldSelection, nextSelectable.gameObject);
                    
                    // Update last selected button
                    lastSelectedButton = nextSelectable.gameObject;
                }
            }
        }
        else if (currentMenu != null && currentMenu.firstSelectedButton != null)
        {
            // Fallback if no button is currently selected
            eventSystem.SetSelectedGameObject(currentMenu.firstSelectedButton);
            
            // Handle text effect for initial selection
            HandleSelectionChange(null, currentMenu.firstSelectedButton);
            
            // Update last selected button
            lastSelectedButton = currentMenu.firstSelectedButton;
        }
    }

    private void OnSubmit(InputAction.CallbackContext context)
    {
        GameObject currentSelected = eventSystem.currentSelectedGameObject;
        if (currentSelected != null)
        {
            // Manually trigger the button click
            Button button = currentSelected.GetComponent<Button>();
            if (button != null && button.onClick != null)
            {
                button.onClick.Invoke();
            }
        }
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        // Go back to the previous menu
        GoBack();
    }

    /// <summary>
    /// Opens a menu by name and adds it to the navigation history
    /// </summary>
    /// <param name="menuName">Name of the menu to open</param>
    public void OpenMenu(string menuName)
    {
        MenuCanvas targetMenu = FindMenu(menuName);
        
        if (targetMenu != null)
        {
            // Store current menu in history for back navigation
            if (currentMenu != null)
            {
                menuHistory.Push(currentMenu);
                currentMenu.canvas.gameObject.SetActive(false);
            }
            
            // Deactivate text effect on the last selected button
            if (lastSelectedButton != null)
            {
                TextEffect textEffect = GetTextEffectFromButton(lastSelectedButton);
                if (textEffect != null && textEffectManager != null)
                {
                    textEffectManager.StopTextEffect(textEffect);
                }
                lastSelectedButton = null;
            }
            
            // Activate new menu
            targetMenu.canvas.gameObject.SetActive(true);
            currentMenu = targetMenu;
            
            // Set initial selection
            if (targetMenu.firstSelectedButton != null)
            {
                eventSystem.SetSelectedGameObject(targetMenu.firstSelectedButton);
                lastSelectedButton = targetMenu.firstSelectedButton;
                
                // Activate text effect on the initially selected button
                TextEffect textEffect = GetTextEffectFromButton(targetMenu.firstSelectedButton);
                if (textEffect != null && textEffectManager != null)
                {
                    textEffectManager.StartTextEffect(textEffect);
                }
            }
            else
            {
                // Try to find any selectable in the menu
                Selectable[] selectables = targetMenu.canvas.GetComponentsInChildren<Selectable>(true);
                if (selectables.Length > 0)
                {
                    eventSystem.SetSelectedGameObject(selectables[0].gameObject);
                    lastSelectedButton = selectables[0].gameObject;
                    
                    // Activate text effect on the first selectable
                    TextEffect textEffect = GetTextEffectFromButton(selectables[0].gameObject);
                    if (textEffect != null && textEffectManager != null)
                    {
                        textEffectManager.StartTextEffect(textEffect);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Menu not found: " + menuName);
        }
    }

    /// <summary>
    /// Goes back to the previous menu in history
    /// </summary>
    public void GoBack()
    {
        if (menuHistory.Count > 0)
        {
            // Deactivate text effect on the last selected button
            if (lastSelectedButton != null)
            {
                TextEffect textEffect = GetTextEffectFromButton(lastSelectedButton);
                if (textEffect != null && textEffectManager != null)
                {
                    textEffectManager.StopTextEffect(textEffect);
                }
                lastSelectedButton = null;
            }
            
            // Disable current menu
            if (currentMenu != null)
            {
                currentMenu.canvas.gameObject.SetActive(false);
            }
            
            // Get previous menu from history
            currentMenu = menuHistory.Pop();
            
            // Enable previous menu
            currentMenu.canvas.gameObject.SetActive(true);
            
            // Restore selection in previous menu
            if (currentMenu.firstSelectedButton != null)
            {
                eventSystem.SetSelectedGameObject(currentMenu.firstSelectedButton);
                lastSelectedButton = currentMenu.firstSelectedButton;
                
                // Activate text effect on the initially selected button
                TextEffect textEffect = GetTextEffectFromButton(currentMenu.firstSelectedButton);
                if (textEffect != null && textEffectManager != null)
                {
                    textEffectManager.StartTextEffect(textEffect);
                }
            }
            else
            {
                // Try to find any selectable
                Selectable[] selectables = currentMenu.canvas.GetComponentsInChildren<Selectable>(true);
                if (selectables.Length > 0)
                {
                    eventSystem.SetSelectedGameObject(selectables[0].gameObject);
                    lastSelectedButton = selectables[0].gameObject;
                    
                    // Activate text effect on the first selectable
                    TextEffect textEffect = GetTextEffectFromButton(selectables[0].gameObject);
                    if (textEffect != null && textEffectManager != null)
                    {
                        textEffectManager.StartTextEffect(textEffect);
                    }
                }
            }
        }
        else if (currentMenu != null && currentMenu.parentMenu != null)
        {
            // Deactivate text effect on the last selected button
            if (lastSelectedButton != null)
            {
                TextEffect textEffect = GetTextEffectFromButton(lastSelectedButton);
                if (textEffect != null && textEffectManager != null)
                {
                    textEffectManager.StopTextEffect(textEffect);
                }
                lastSelectedButton = null;
            }
            
            // If we have a parent menu reference but it's not in history
            // (useful for manually configured hierarchies)
            MenuCanvas parentMenu = currentMenu.parentMenu;
            
            // Disable current menu
            currentMenu.canvas.gameObject.SetActive(false);
            
            // Enable parent menu
            parentMenu.canvas.gameObject.SetActive(true);
            currentMenu = parentMenu;
            
            // Set selection in parent menu
            if (parentMenu.firstSelectedButton != null)
            {
                eventSystem.SetSelectedGameObject(parentMenu.firstSelectedButton);
                lastSelectedButton = parentMenu.firstSelectedButton;
                
                // Activate text effect on the initially selected button
                TextEffect textEffect = GetTextEffectFromButton(parentMenu.firstSelectedButton);
                if (textEffect != null && textEffectManager != null)
                {
                    textEffectManager.StartTextEffect(textEffect);
                }
            }
            
            Debug.Log("Went back to parent menu: " + parentMenu.menuName);
        }
    }

    /// <summary>
    /// Finds a menu by name
    /// </summary>
    private MenuCanvas FindMenu(string menuName)
    {
        return menuCanvases.Find(menu => menu.menuName == menuName);
    }

    /// <summary>
    /// Opens a menu from a button click
    /// </summary>
    public void OpenMenuFromButton(string menuName)
    {
        OpenMenu(menuName);
    }

    /// <summary>
    /// Sets up a direct parent-child relationship between menus
    /// </summary>
    public void SetMenuParent(string childMenuName, string parentMenuName)
    {
        MenuCanvas childMenu = FindMenu(childMenuName);
        MenuCanvas parentMenu = FindMenu(parentMenuName);
        
        if (childMenu != null && parentMenu != null)
        {
            childMenu.parentMenu = parentMenu;
        }
    }

    /// <summary>
    /// Adds a new menu canvas at runtime
    /// </summary>
    public void AddMenuCanvas(string menuName, Canvas canvas, GameObject firstSelectedButton = null)
    {
        MenuCanvas newMenu = new MenuCanvas
        {
            menuName = menuName,
            canvas = canvas,
            firstSelectedButton = firstSelectedButton
        };
        
        menuCanvases.Add(newMenu);
        canvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Returns to the initial/main menu, clearing all history
    /// </summary>
    public void ReturnToMainMenu()
    {
        // Deactivate text effect on the last selected button
        if (lastSelectedButton != null)
        {
            TextEffect textEffect = GetTextEffectFromButton(lastSelectedButton);
            if (textEffect != null && textEffectManager != null)
            {
                textEffectManager.StopTextEffect(textEffect);
            }
            lastSelectedButton = null;
        }
        
        // Disable current menu
        if (currentMenu != null)
        {
            currentMenu.canvas.gameObject.SetActive(false);
        }
        
        // Clear menu history
        menuHistory.Clear();
        
        // Open initial menu
        OpenMenu(initialMenuName);
    }
}