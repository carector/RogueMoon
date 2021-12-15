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
        if (Mathf.Abs(rb.velocity.x) > 0.05f && !reachedZero)
        {
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, 0.025f);
        }
        else
            reachedZero = true;
    }

    IEnumerator FadeOutAfterTime()
    {
        yield return new WaitForSeconds(6);
        while(spr.color.a > 0)
        {
            spr.color -= new Color(0, 0, 0, 0.01f);
            yield return new WaitForFixedUpdate();
        }

        Destroy(this.gameObject);
    }
}
