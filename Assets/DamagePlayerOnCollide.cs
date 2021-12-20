using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagePlayerOnCollide : MonoBehaviour
{
    PlayerController ply;
    Rigidbody2D prb;
    Collider2D col;

    public float knockbackAmount = 4;

    // Start is called before the first frame update
    void Start()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        ply = FindObjectOfType<PlayerController>();
        prb = ply.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.tag == "Player" && !ply.pAbilities.damageDelayInProgress)
        {   
            prb.velocity = (ply.transform.position - (Vector3)col.ClosestPoint(ply.transform.position)).normalized * knockbackAmount;
            print(prb.velocity);
            ply.TakeDamage();
        }
    }
}
