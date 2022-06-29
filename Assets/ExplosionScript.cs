using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3);
        foreach(Collider2D c in hits)
        {
            c.SendMessage("HitByExplosion", SendMessageOptions.DontRequireReceiver);
        }

    }

    // Update is called once per frame
    void Update()
    {

    }
}
