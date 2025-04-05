using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private string mainGameSceneName = "Main Game Scene";
    [SerializeField] private Object mainGameScene; // For inspector reference
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private UnityEngine.UI.Slider progressBar;

    // Singleton pattern
    public static GameManager Instance { get; private set; }
    
    private AsyncOperation asyncSceneLoad;
    private bool isPreloaded = false;
    
    private void Start()
    {
        // Start preloading the scene when the game starts
        PreloadMainScene();
    }

    /// <summary>
    /// Preloads the main game scene in the background
    /// </summary>
    private void PreloadMainScene()
    {
        // Determine which scene to load (inspector reference or by name)
        string sceneToLoad;
        
        if (mainGameScene != null)
        {
            // Get the scene path from the Object reference
            sceneToLoad = mainGameScene.name;
            Debug.Log("Preloading scene from inspector reference: " + sceneToLoad);
        }
        else
        {
            // Use the name if no reference was provided
            sceneToLoad = mainGameSceneName;
            Debug.Log("Preloading scene by name: " + sceneToLoad);
        }
        
        // Start async loading operation
        StartCoroutine(PreloadSceneAsync(sceneToLoad));
    }

    /// <summary>
    /// Starts the game by activating the preloaded scene
    /// </summary>

    public void TestScene()
    {
        SceneManager.LoadScene(mainGameScene.name);
    }
    public void StartGame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("StartGame method called - activating preloaded scene");
        
        if (isPreloaded && asyncSceneLoad != null)
        {
            // Show loading screen if assigned
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
            }
            
            // Allow scene activation (switches to the preloaded scene)
            asyncSceneLoad.allowSceneActivation = true;
            
            // Start coroutine to hide loading screen after scene is fully loaded
            StartCoroutine(HideLoadingScreenWhenLoaded());
        }
        else
        {
            // If the scene isn't preloaded yet, load it directly
            Debug.LogWarning("Scene wasn't preloaded. Loading scene now...");
            
            // Determine which scene to load
            string sceneToLoad = (mainGameScene != null) ? mainGameScene.name : mainGameSceneName;
            StartCoroutine(LoadSceneAsync(sceneToLoad));
        }
    }

    /// <summary>
    /// Exits the application
    /// </summary>
    public void ExitGame()
    {
        // Log the exit in the editor
        Debug.Log("Exiting game");

        // In editor, this doesn't actually quit
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Preloads a scene asynchronously but doesn't activate it
    /// </summary>
    /// <param name="sceneName">The name of the scene to preload</param>
    private IEnumerator PreloadSceneAsync(string sceneName)
    {
        // Start async loading operation
        asyncSceneLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Don't allow scene activation until StartGame is called
        asyncSceneLoad.allowSceneActivation = false;
        
        // Update progress bar while loading
        while (asyncSceneLoad.progress < 0.9f)
        {
            // Update progress bar if assigned
            if (progressBar != null)
            {
                // AsyncOperation.progress goes from 0 to 0.9 during preload
                float progress = Mathf.Clamp01(asyncSceneLoad.progress / 0.9f);
                progressBar.value = progress;
            }
            
            yield return null;
        }
        
        // Mark as preloaded when it reaches 90%
        isPreloaded = true;
        Debug.Log("Scene preloaded and ready to activate");
    }

    /// <summary>
    /// Loads a scene asynchronously with a loading screen (used as fallback)
    /// </summary>
    /// <param name="sceneName">The name of the scene to load</param>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Activate loading screen if assigned
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        // Start async loading operation
        asyncSceneLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Update progress bar while loading
        while (!asyncSceneLoad.isDone)
        {
            // Update progress bar if assigned
            if (progressBar != null)
            {
                // AsyncOperation.progress goes from 0 to 0.9
                // We divide by 0.9 to get a value between 0 and 1
                float progress = Mathf.Clamp01(asyncSceneLoad.progress / 0.9f);
                progressBar.value = progress;
            }
            
            yield return null;
        }
        
        // Hide loading screen when done
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }
    
    /// <summary>
    /// Hides the loading screen after the scene is fully loaded
    /// </summary>
    private IEnumerator HideLoadingScreenWhenLoaded()
    {
        // Wait until the scene is fully loaded
        while (asyncSceneLoad != null && !asyncSceneLoad.isDone)
        {
            yield return null;
        }
        
        // Hide loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }
}