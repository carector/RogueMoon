using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FantasiaActivator : MonoBehaviour
{
    public FantasiaScript f;
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
        if(collision.tag == "Player")
        {
            gm.StartCoroutine(gm.TransitionMusic(gm.sfx.music[2]));
            f.Activate();
        }
    }
}
