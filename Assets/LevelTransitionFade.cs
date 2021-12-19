using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelTransitionFade : MonoBehaviour
{
    public PathDarkness[] dark;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            dark[0].Fade();
            dark[1].Fade();
            this.enabled = false;
        }
    }
}
