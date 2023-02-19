using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.VisualScripting;

public class CameraSwap : MonoBehaviour
{
    public CinemachineVirtualCamera disableThisCam;
    public CinemachineVirtualCamera enableThisCam;

    private bool toggle = false;

    private void Awake()
    {

    }

    public void SwapCam()
    {
        toggle = !toggle;

        if (toggle)
        {
            disableThisCam.Priority = 1;
            enableThisCam.Priority = 10;

            disableThisCam.GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineFramingTransposer>().enabled = false;
            disableThisCam.GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineFramingTransposer>().enabled = true;
        }
        else
        {
            disableThisCam.Priority = 10;
            enableThisCam.Priority = 1;
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            SwapCam();
        }
    }
}
