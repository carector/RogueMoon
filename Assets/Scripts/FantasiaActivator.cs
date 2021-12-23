using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FantasiaActivator : MonoBehaviour
{
    public FantasiaScript[] f;
    GameManager gm;
    // Start is called before the first frame update
    void Start()
    {
        gm = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player" && enabled)
        {
            gm.StopAmbience();
            gm.StartCoroutine(gm.TransitionMusic(3, 1));
            f[0].Activate();
            this.enabled = false;
        }
    }
}
