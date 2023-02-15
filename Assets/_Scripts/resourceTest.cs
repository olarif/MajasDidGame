using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEditor;

public class resourceTest : MonoBehaviour
{
    public TextMeshProUGUI data;
    
    public AudioFile[] allAudio;
    public AudioSource audioSource;
    
    public string audioName;
    
    public string dataPath;
    
    
    
    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = Resources.Load<AudioClip>(audioName);
        audioSource.Play();
        
        
        
        dataPath = Application.dataPath;
        data.text = dataPath;
    }
}
