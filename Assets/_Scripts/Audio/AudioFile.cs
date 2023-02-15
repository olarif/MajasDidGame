using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Audio Clip", menuName = "ScriptableObjects/Audio File")]
public class AudioFile : ScriptableObject
{
    public AudioClip audioClip;
    public AudioSource source;
 
    public void PlayAudio()
    {
        
    }
}
