using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GibScript : MonoBehaviour
{
    public Sprite[] spritePool;
    public bool initializedByParent;
    bool reachedZero;
    SpriteRenderer spr;
    Rigidbody2D rb;

    // Start is called before the first frame update
    void Start()
    {
        if (!initializedByParent)
        {
            spr = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            rb.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(0f, 1f)).normalized * 6;
            rb.gravityScale = 0.5f;
            spr.sprite = spritePool[Random.Range(0, spritePool.Length)];
            StartCoroutine(FadeOutAfterTime());
        }
    }

    public void InitializeGib(Sprite s, float angularVelocity)
    {
        spr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        rb.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(0f, 1f)).normalized * 6;
        rb.angularVelocity = angularVelocity;
        spr.sprite = s;
        StartCoroutine(FadeOutAfterTime());
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rb.velocity = Vector2.Lerp(rb.velocity, new Vector2(0, rb.velocity.y), 0.025f);
        rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -3, 3));
    }

    IEnumerator FadeOutAfterTime()
    {
        yield return new WaitForSeconds(3);
        while(spr.color.a > 0)
        {
            spr.color -= new Color(0, 0, 0, 0.01f);
            yield return new WaitForFixedUpdate();
        }

        Destroy(this.gameObject);
    }
}
