using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

    private EventSystem eventSystem;
    private float lastInputTime = 0f;
    private MenuCanvas currentMenu;
    private Stack<MenuCanvas> menuHistory = new Stack<MenuCanvas>();

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
                    eventSystem.SetSelectedGameObject(nextSelectable.gameObject);
                }
            }
        }
        else if (currentMenu != null && currentMenu.firstSelectedButton != null)
        {
            // Fallback if no button is currently selected
            eventSystem.SetSelectedGameObject(currentMenu.firstSelectedButton);
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
            
            // Activate new menu
            targetMenu.canvas.gameObject.SetActive(true);
            currentMenu = targetMenu;
            
            // Set initial selection
            if (targetMenu.firstSelectedButton != null)
            {
                eventSystem.SetSelectedGameObject(targetMenu.firstSelectedButton);
            }
            else
            {
                // Try to find any selectable in the menu
                Selectable[] selectables = targetMenu.canvas.GetComponentsInChildren<Selectable>(true);
                if (selectables.Length > 0)
                {
                    eventSystem.SetSelectedGameObject(selectables[0].gameObject);
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
            }
            else
            {
                // Try to find any selectable
                Selectable[] selectables = currentMenu.canvas.GetComponentsInChildren<Selectable>(true);
                if (selectables.Length > 0)
                {
                    eventSystem.SetSelectedGameObject(selectables[0].gameObject);
                }
            }
        }
        else if (currentMenu != null && currentMenu.parentMenu != null)
        {
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