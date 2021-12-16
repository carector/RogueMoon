using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpitterProjectileScript : MonoBehaviour
{
    PlayerController ply;

    public void Initialize(int parentDir)
    {
        ply = FindObjectOfType<PlayerController>();
        Vector2 dir = ply.transform.position - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        print(angle);
        if (parentDir == 1)
            angle = Mathf.Clamp(angle, -45, 20);
        else
        {
            if(angle < 0)
                angle = Mathf.Clamp(angle, -180, -135);
            else
                angle = Mathf.Clamp(angle, 0, 160);
        }
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }

    private void FixedUpdate()
    {
        transform.position += transform.right * 0.1f;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            ply.TakeDamage();
            Destroy(this.gameObject);
        }
        else if(collision.tag == "Ground")
            Destroy(this.gameObject);
    }
}
