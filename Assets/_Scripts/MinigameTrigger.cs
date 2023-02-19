using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MinigameTrigger : MonoBehaviour
{
    private PlayerInputActions inputActions;
    
    public GameObject speechBubble;
    public GameObject minigame;

    private bool toggled = false;
    private bool isOpen;
    
    void Start()
    {
        speechBubble.SetActive(false);
    }
    
    void Update()
    {
        if (toggled)
        {
            speechBubble.SetActive(true);
        }
        else
        {
            speechBubble.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();

        inputActions.Player.Interact.performed += Interacted;

    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Interacted(InputAction.CallbackContext obj)
    {
        //press again to close it todo

        isOpen = !isOpen;

        if (isOpen)
        {
            minigame.SetActive(true);
        }
        else
        {
            minigame.SetActive(false);
        }
            
           
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        toggled = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        toggled = false;
        minigame.SetActive(false);
    }
}
