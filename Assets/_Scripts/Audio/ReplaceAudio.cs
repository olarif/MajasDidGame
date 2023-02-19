using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public class ReplaceAudio : MonoBehaviour
{
    public AudioSource audioSource;
    public string audioFileName = "menuMusic.wav";

    void Start()
    {
        string audioFilePath = Application.dataPath + "/AudioFiles/" + audioFileName;

        byte[] audioData = File.ReadAllBytes(audioFilePath);

        //AudioClip audioClip = WAVUtility.ToAudioClip(audioData);
    }

}
