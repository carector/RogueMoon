using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockerScript : MonoBehaviour
{
    SpriteRenderer spr;
    Collider2D col;

    // Start is called before the first frame update
    void Start()
    {
        spr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ActivateSelf()
    {
        col.enabled = true;
        spr.color = Color.white;
    }
    public void DisableSelf()
    {
        col.enabled = false;
        spr.color = Color.clear;
    }
}
