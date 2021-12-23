using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicTransitionTrigger : MonoBehaviour
{
    public int newMusic;
    public float pitch = 1;
    GameManager gm;

    // Start is called before the first frame update
    void Start()
    {
        gm = FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player" && enabled)
        {
            gm.StartCoroutine(gm.TransitionMusic(newMusic, pitch));
            this.enabled = false;
        }
    }
}
