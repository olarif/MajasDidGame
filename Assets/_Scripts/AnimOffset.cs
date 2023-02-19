using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimOffset : MonoBehaviour
{
    Animator anim;
    private float offset;

    void Start()
    {
        offset = Random.Range(0f, 1f);
        anim = GetComponent<Animator>();

        anim.speed = Random.Range(0.75f, 1.25f);
        //anim.SetFloat("Cycle Offset", offset);
    }
}
