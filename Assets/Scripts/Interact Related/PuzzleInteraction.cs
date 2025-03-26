using System;
using UnityEngine;
using UnityEngine.Events;

public class PuzzleInteraction : MonoBehaviour
{
    private Canvas interactCanvas;
    public bool hasInteracted = false;
    
    [SerializeField]
    private PlayerStateControl playerStateControl;
    
    [SerializeField]
    private PlayerStateControl.PlayerState puzzleType;
    
   
    private void Awake()
    {
        interactCanvas = GetComponentInChildren<Canvas>();
        interactCanvas.gameObject.SetActive(false);
    }

    public void StartPuzzle()
    {
        if (hasInteracted)
        {
            return;
        }
        playerStateControl.SetPlayerState(puzzleType);
    }

    public void PuzzleComplete()
    {
        HideUI();
        hasInteracted = true;
    }
    public void DisplayUI()
    {
        if (hasInteracted)
        {
            return;
        }
        interactCanvas.gameObject.SetActive(true);
    }

    public void HideUI()
    {
        interactCanvas.gameObject.SetActive(false);
    }
}