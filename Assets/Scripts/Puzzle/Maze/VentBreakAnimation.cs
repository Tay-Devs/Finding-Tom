using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class VentBreakAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("The target X rotation in degrees")]
    [SerializeField] private float targetXRotation = -75f;
    
    [Tooltip("Total animation duration in seconds")]
    [SerializeField] private float animationDuration = 1.5f;
    
    [Tooltip("Initial delay before the vent starts to give way")]
    [SerializeField] private float initialDelay = 0.2f;
    
    [Tooltip("How much the vent initially resists (0-1)")]
    [SerializeField] private float initialResistance = 0.8f;
    
    [Tooltip("Controls the 'snapping' effect when the vent gives way")]
    [SerializeField] private float breakForce = 8f;
    
    [Tooltip("Amount of spring-like oscillation after breaking")]
    [SerializeField] private float springiness = 5f;
    
    [Tooltip("How quickly the oscillations dampen")]
    [SerializeField] private float dampingFactor = 3f;
    
    [Tooltip("Sound to play when the vent begins to break")]
    [SerializeField] private AudioClip creakSound;
    
    [SerializeField]
    private PlayerStateControl playerStateControl;
    
    [Header("Cameras")]
    [SerializeField]
    private GameObject mainCamera;
    
    [SerializeField]
    private GameObject endCamera;

    [Header("Events")] 
    
    [SerializeField]
    private UnityEvent onPuzzleFinished;
    
    [SerializeField]
    private UnityEvent puzzleCompleted;

 
   
    
    
    private AudioSource audioSource;
    private Quaternion initialRotation;
    private Quaternion targetRotation;
    private bool animationTriggered = false;

    private void Awake()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        mainCamera.SetActive(true);
        endCamera.SetActive(false);
        initialRotation = transform.rotation;
    }

    // Call this method to trigger the vent breaking animation
    public void TriggerVentBreak()
    {
        if (!animationTriggered)
        {
            animationTriggered = true;
            targetRotation = Quaternion.Euler(targetXRotation, initialRotation.eulerAngles.y, initialRotation.eulerAngles.z);
            StartCoroutine(AnimateVentBreak());
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            print("Cheat");
            mainCamera.SetActive(false);
            endCamera.SetActive(true);
            TriggerVentBreak();
        }
    }

    // This can be called by a trigger or collision event
    private void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object has a specific tag like "Heavy" or check its mass
        if (other.CompareTag("Player"))
        {
            mainCamera.SetActive(false);
            endCamera.SetActive(true);
            TriggerVentBreak();
        }
    }

    private IEnumerator AnimateVentBreak()
    {
        onPuzzleFinished?.Invoke();
        // Initial delay - vent holding the weight
        yield return new WaitForSeconds(initialDelay);
        
        // Play the initial creaking sound
        if (creakSound != null)
        {
            audioSource.PlayOneShot(creakSound);
        }
        
        float elapsedTime = 0f;
        bool breakSoundPlayed = false;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / animationDuration;
            
            // Calculate a custom curve for the animation
            float t = CalculateAnimationCurve(normalizedTime);
            
            // Interpolate between initial and target rotation
            transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, t);
            
            yield return null;
        }
        
        // Ensure we reach the exact target rotation
        transform.rotation = targetRotation;
        yield return new WaitForSeconds(0.60f);
        puzzleCompleted?.Invoke();
        playerStateControl.SetPlayerState(PlayerStateControl.PlayerState.Moving);
        
    }
    
    private float CalculateAnimationCurve(float t)
    {
        // First phase: Initial resistance
        if (t < initialResistance)
        {
            // Slow, subtle movement as the vent starts to give
            return Mathf.Pow(t / initialResistance, 3) * 0.1f;
        }
        
        // Second phase: Breaking point - fast movement
        float breakPoint = (t - initialResistance) / (1 - initialResistance);
        float basicProgress = Mathf.Pow(breakPoint, breakForce);
        
        // Add oscillation for a spring effect after the break
        float oscillation = 0;
        if (breakPoint > 0)
        {
            oscillation = Mathf.Sin(breakPoint * springiness * Mathf.PI) * 
                          Mathf.Exp(-breakPoint * dampingFactor) * 
                          0.15f; // Scale the oscillation effect
        }
        
        return Mathf.Clamp01(0.1f + (basicProgress * 0.9f) + oscillation);
    }
    
    // Helper method for testing - activates the vent break via Inspector or UI button
    public void DebugTriggerBreak()
    {
        TriggerVentBreak();
    }
}