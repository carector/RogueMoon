using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetalGibScript : MonoBehaviour
{
    public Sprite[] spritePool;

    SpriteRenderer spr;
    Rigidbody2D rb;
    PlayerController ply;
    GameManager gm;

    float lastFrameYVel;
    bool delayPassed;
    bool beingPickedUp;

    // Start is called before the first frame update
    void Start()
    {
        gm = FindObjectOfType<GameManager>();
        ply = FindObjectOfType<PlayerController>();
        spr = GetComponent<SpriteRenderer>();
        spr.sprite = spritePool[Random.Range(0, spritePool.Length)];
        rb = GetComponent<Rigidbody2D>();
        rb.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(0f, 1f)).normalized * 6;
        StartCoroutine(InitialDropDelay());
    }

    private void Update()
    {
        if (!beingPickedUp)
        {
            if (rb.velocity.y >= 0 && Mathf.Sign(lastFrameYVel) == -1 && Mathf.Abs(lastFrameYVel - rb.velocity.y) > 0.5f)
            {
                Sprite s = spritePool[Random.Range(0, spritePool.Length)];
                while (s == spr.sprite)
                    s = spritePool[Random.Range(0, spritePool.Length)];
                spr.sprite = s;
            }
            lastFrameYVel = rb.velocity.y;

            if (!delayPassed)
                return;

            if (Vector2.Distance(transform.position, ply.transform.position) < 2.15f)
            {
                ply.pResources.metal++;
                beingPickedUp = true;
                StartCoroutine(Pickup());
            }
        }
    }

    IEnumerator Pickup()
    {
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        //gm.PlaySFX(gm.sfx.generalSounds[0], Random.Range(0.85f, 1.15f));
        transform.parent = ply.transform;
        while(Vector2.Distance(transform.localPosition, Vector2.zero) > 0.05f)
        {
            transform.localPosition = Vector2.Lerp(transform.localPosition, Vector2.zero, 0.25f);
            yield return new WaitForFixedUpdate();
        }

        Destroy(this.gameObject);

    }

    IEnumerator InitialDropDelay()
    {
        yield return new WaitForSeconds(1);
        delayPassed = true;
    }
}
