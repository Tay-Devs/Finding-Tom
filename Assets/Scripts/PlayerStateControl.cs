using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateControl : MonoBehaviour
{
    [System.Serializable]
    public class PuzzleSetup
    {
        public string puzzleName;
        public GameObject puzzleObject;
        public GameObject puzzleCamera;
        public PlayerState stateType;
    }

    public enum PlayerState
    {
        Moving,
        LaserPuzzle,
        DicePuzzle,
        MazePuzzle,
        // Add more puzzle states here
    }

    [SerializeField] private GameObject playerGameObject;
    [SerializeField] private GameObject playerCamera;
    [SerializeField] private Animator fadeAnimator;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private List<PuzzleSetup> puzzles = new List<PuzzleSetup>();

    private Dictionary<PlayerState, PuzzleSetup> puzzleDict = new Dictionary<PlayerState, PuzzleSetup>();
    public PlayerState currentState = PlayerState.Moving;
    private PuzzleSetup currentActivePuzzle;

    private void Awake()
    {
        // Initialize dictionary for faster lookups
        foreach (var puzzle in puzzles)
        {
            puzzleDict[puzzle.stateType] = puzzle;
            puzzle.puzzleObject.SetActive(false);
        }

        // Initial state setup
        playerGameObject.SetActive(true);
        playerCamera.SetActive(true);
    }

    private IEnumerator TransitionToState(PlayerState newState)
    {
        // Start fade animation
        fadeAnimator.SetBool("isFinished", false);
        fadeAnimator.SetTrigger("StartFade");
        yield return new WaitForSeconds(fadeInDuration);
        
        // Handle the transition based on target state
        if (newState == PlayerState.Moving)
        {
            // Disable current puzzle if there is one
            if (currentActivePuzzle != null)
            {
                currentActivePuzzle.puzzleCamera.SetActive(false);
                currentActivePuzzle.puzzleObject.SetActive(false);
                currentActivePuzzle = null;
            }
            
            // Enable player
            playerCamera.SetActive(true);
            yield return new WaitForSeconds(transitionDuration);
            playerGameObject.SetActive(true);
        }
        else if (puzzleDict.TryGetValue(newState, out PuzzleSetup targetPuzzle))
        {
            // Disable player
            playerCamera.SetActive(false);
            playerGameObject.SetActive(false);
            
            // Disable current puzzle if there is one
            if (currentActivePuzzle != null && currentActivePuzzle != targetPuzzle)
            {
                currentActivePuzzle.puzzleCamera.SetActive(false);
                currentActivePuzzle.puzzleObject.SetActive(false);
            }
            
            // Enable target puzzle
            yield return new WaitForSeconds(transitionDuration);
            targetPuzzle.puzzleCamera.SetActive(true);
            targetPuzzle.puzzleObject.SetActive(true);
            
            // Update current active puzzle
            currentActivePuzzle = targetPuzzle;
        }
        
        // Complete the transition
        yield return new WaitForSeconds(transitionDuration);
        fadeAnimator.SetBool("isFinished", true);
        
        // Update current state
        currentState = newState;
    }

    public void SetPlayerState(PlayerState state)
    {
        if (currentState != state)
        {
            StartCoroutine(TransitionToState(state));
        }
    }
}