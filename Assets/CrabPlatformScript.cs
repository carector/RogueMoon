using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrabPlatformScript : MonoBehaviour
{
    PlayerController ply;

    // Start is called before the first frame update
    void Start()
    {
        ply = FindObjectOfType<PlayerController>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position += Vector3.left * 0.01f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            if (ply.pMovement.isGrounded)
                ply.transform.parent = transform;
            else
                ply.transform.parent = null;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
            ply.transform.parent = null;
    }
}
