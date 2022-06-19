using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabbableObject : MonoBehaviour
{
    Collider2D col;
    Rigidbody2D rb;

    // Start is called before the first frame update
    void Start()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GetPulledByHarpoon()
    {
        col.isTrigger = true;
    }

    public void GetReleasedByHarpoon()
    {
        col.isTrigger = false;
    }

    public void DisableCollider()
    {
        col.enabled = false;
    }
}
