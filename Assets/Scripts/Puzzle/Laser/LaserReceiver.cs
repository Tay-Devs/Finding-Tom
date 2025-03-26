using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LaserReceiver : MonoBehaviour
{
    [Tooltip("If enabled, the receiver will check that all deflectors have been hit")]
    public bool requireAllDeflectors = true;
    
    private LaserEmitter laserEmitter;
    
    [Tooltip("Optional visual feedback when the receiver is activated")]
    public GameObject activationEffect;

    [Tooltip("Color to change the receiver to when activated")]
    //public Color activatedColor = Color.green;
    public GameObject colorlessRainbow;
    public GameObject colorfulRainbow;
    
    // State tracking
    public bool isPuzzleSolved = false;
    private Renderer receiverRenderer;
    private Color originalColor;
    public Animation testAnimation;
    [SerializeField] private PlayerStateControl playerStateControl;

    public UnityEvent onPuzzleSolved;
    private void Awake()
    {
        laserEmitter = GameObject.FindWithTag("Laser Emitter").GetComponent<LaserEmitter>();
        // Cache the renderer component
        receiverRenderer = GetComponent<Renderer>();
        if (receiverRenderer != null)
        {
            originalColor = receiverRenderer.material.color;
        }
        
        // Disable any activation effect initially
        if (activationEffect != null)
        {
            activationEffect.SetActive(false);
        }
    }
    
    /// <summary>
    /// Called by the LaserEmitter when a laser hits this receiver
    /// </summary>
    /// <param name="hitDeflectors">List of deflectors the laser has passed through</param>
    /// <param name="totalDeflectors">Total number of deflectors in the scene</param>
    public void ReceiveLaser(List<LaserDeflector> hitDeflectors, int totalDeflectors)
    {
        // If puzzle is already solved, no need to process again
        if (isPuzzleSolved)
        {
            
            return;
        }
           
            
        bool allDeflectorsHit = (hitDeflectors.Count == totalDeflectors);
        
        // Check puzzle completion condition
        if (!requireAllDeflectors || allDeflectorsHit)
        {
            
            // Visual feedback
            if (receiverRenderer != null)
            {
                //receiverRenderer.material.color = activatedColor;
                activationEffect.SetActive(true);
            }

            StartCoroutine(FinishPuzzleAnimation());
            // Show activation effect if available
            if (activationEffect != null)
            {
                activationEffect.SetActive(true);
            }
            
            // Debug message - this would be replaced with actual game events later
            if (allDeflectorsHit)
            {
                colorfulRainbow.SetActive(true);
                colorlessRainbow.SetActive(false);
                isPuzzleSolved = true;
                print("Solved");
            }
            else
            {
                //Debug.Log("Receiver activated! Laser has reached the destination.");
            }
        }
        else
        {
           // Debug.Log("Receiver activated! Laser has reached the destination but didnt hit all the deflectors.");
        }
    }

    private IEnumerator FinishPuzzleAnimation()
    {
        onPuzzleSolved.Invoke();
        isPuzzleSolved = true;
        laserEmitter.isContinuous = true;
        print("Start Animation");
        yield return new WaitForSeconds(1f);
        print("finish animation and move camera");
        playerStateControl.SetPlayerState(PlayerStateControl.PlayerState.Moving);

    }
    /// Resets the receiver to its initial state (for puzzle reset)
    public void ResetReceiver()
    {
        isPuzzleSolved = false;
        
        // Reset visual elements
        if (receiverRenderer != null)
        {
            receiverRenderer.material.color = originalColor;
        }
        
        if (activationEffect != null)
        {
            colorlessRainbow.SetActive(false);
            colorfulRainbow.SetActive(true);
        }
    }
    
 
    /// Returns whether the puzzle has been solved
    /*{
     //add the correct using before resume the work on the animaton
        //Camera effect goes zoom out + fade to white.
        while (testAnimation.isPlaying)
        {
            yield return new WaitForSeconds(1);
        }
        
    }*/
}