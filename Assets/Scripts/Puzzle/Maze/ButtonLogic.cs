using System;
using UnityEngine;
using UnityEngine.Events;

public class ButtonLogic : MonoBehaviour
{
    [SerializeField] UnityEvent onButtonPressed;
    [SerializeField] UnityEvent onButtonUnpressed;
    [SerializeField] private bool canBePressedOnce = true;
    private bool hasButtonPressed = false;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BallOnButton()
    {
        animator.SetTrigger("ButtonPress");
        print("onButtonPressed");
        if (hasButtonPressed && canBePressedOnce)
        {
            return;
        }
        hasButtonPressed = true;
        onButtonPressed?.Invoke();
    }

    public void OnButtonUnpressed()
    {
        animator.SetTrigger("Release");
        print("OnButtonUnpressed");
        //Player animation of button unsmushed
        onButtonUnpressed?.Invoke();
    }
}
