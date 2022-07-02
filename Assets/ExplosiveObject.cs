using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosiveObject : Grabbable
{
    public GameObject explosionPrefab;
    SpriteRenderer spr;
    protected Collider2D col;
    protected Rigidbody2D rb;


    // Start is called before the first frame update
    void Start()
    {
        GetReferences();
        spr = GetComponentInChildren<SpriteRenderer>();
    }

    protected void GetReferences()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    public override void GetReleasedByHarpoon(Vector2 velocity)
    {
        col.isTrigger = false;
        transform.parent = null;
        rb.bodyType = RigidbodyType2D.Dynamic;
        print(velocity);
        rb.velocity = velocity;
    }

    public override void GetPulledByHarpoon()
    {
        col.isTrigger = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;

        StartCoroutine(ExplosionCountdown());
    }

    IEnumerator ExplosionCountdown()
    {
        for (int i = 0; i < 9; i++)
        {
            if (i == 7)
                StartCoroutine(ShakeSprite());
            else if (i < 7)
                StartCoroutine(RedFlash());

            yield return new WaitForSeconds(1);
        }

        Explode();
    }

    public void Explode()
    {
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(this.gameObject);
    }

    IEnumerator RedFlash()
    {
        float gb = 0;
        while (gb < 1)
        {
            gb += 0.05f;
            spr.color = new Color(1, gb, gb, 1);
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator ShakeSprite()
    {
        while (true)
        {
            spr.transform.localPosition = new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f))*1.5f;
            float rand = Random.Range(0, 1f);
            spr.color = new Color(1, rand, rand, 1);
            yield return new WaitForFixedUpdate();
        }
    }
}
