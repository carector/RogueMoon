using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrabPlatformScript : MonoBehaviour
{
    public float leftMax;
    public float rightMax;


    PlayerController ply;
    Animator anim;


    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        ply = FindObjectOfType<PlayerController>();
        StartCoroutine(MovementCycle());
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
    }

    IEnumerator MovementCycle()
    {
        while (true)
        {
            // Move left
            anim.Play("CrabMoveLeft");
            while (transform.position.x > leftMax)
            {
                transform.position += Vector3.left * 0.01f;
                yield return null;
            }

            // Wait
            anim.Play("CrabIdle");
            yield return new WaitForSeconds(3);

            // Move right
            anim.Play("CrabMoveRight");
            while (transform.position.x < rightMax)
            {
                transform.position += Vector3.right * 0.01f;
                yield return null;
            }

            // Wait
            anim.Play("CrabIdle");
            yield return new WaitForSeconds(3);
        }
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
