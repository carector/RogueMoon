﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationSfxManager : MonoBehaviour
{
    public GameObject sendMessageTarget;
    public AudioClip[] a;
    GameManager gm;
    // Start is called before the first frame update
    void Start()
    {
        gm = FindObjectOfType<GameManager>();
    }

    public void PlaySFX(int index)
    {
        gm.PlaySFX(a[index]);
    }

    public void SendMessage(string s)
    {
        sendMessageTarget.SendMessage(s);
    }
}
