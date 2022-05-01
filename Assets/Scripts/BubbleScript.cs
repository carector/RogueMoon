using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleScript : MonoBehaviour
{
    public enum BubbleSize
    {
        tiny,
        small,
        medium,
        large,
        random
    };

    public bool initializedByParent;
    public BubbleSize bubbleSize;
    public float startingXIntensity;
    public AudioClip[] sfx;

    string animationPrefix;
    bool popping;
    bool initialDelayOver;

    AudioSource audio;
    Animator anim;
    Rigidbody2D rb;

    // Start is called before the first frame update
    void Start()
    {
        Initialize(startingXIntensity, bubbleSize);
    }

    public void Initialize(float intensity, BubbleSize size)
    {
        audio = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.velocity = new Vector2(Random.Range(-1, 1f)*intensity, Random.Range(-0.5f, 1f));

        if (Random.Range(0, 1f) > 0.92f)
        {
            audio.pitch = Random.Range(0.9f, 1.1f);
            audio.PlayOneShot(sfx[Random.Range(0, sfx.Length)]);
        }

        if (size == BubbleSize.random)
        {
            int rand = Random.Range(0, 4);
            switch(rand)
            {
                case 0:
                    size = BubbleSize.tiny;
                    break;
                case 1:
                    size = BubbleSize.small;
                    break;
                case 2:
                    size = BubbleSize.medium;
                    break;
                case 3:
                    size = BubbleSize.large;
                    break;
            }
        }

        switch(size)
        {
            case BubbleSize.tiny:
                animationPrefix = "Tiny";
                break;
            case BubbleSize.small:
                animationPrefix = "Small";
                break;
            case BubbleSize.medium:
                animationPrefix = "Medium";
                break;
            case BubbleSize.large:
                animationPrefix = "Large";
                break;
        }

        anim.Play("Bubble" + animationPrefix + "_Appear");
        StartCoroutine(WaitAndPop());
    }

    IEnumerator WaitAndPop()
    {
        yield return new WaitForSeconds(0.5f);
        initialDelayOver = true;
        yield return new WaitForSeconds(Random.Range(1, 6));
        StartCoroutine(Pop());
    }

    IEnumerator Pop()
    {
        popping = true;
        anim.Play("Bubble"+animationPrefix + "_Pop");
        yield return new WaitForSeconds(1);
        Destroy(this.gameObject);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(!popping)
            rb.velocity = Vector2.Lerp(rb.velocity, new Vector2(0, 3), 0.025f);
        else
            rb.velocity = Vector2.zero;
    }

    public void BreakApart()
    {
        StopAllCoroutines();
        StartCoroutine(Pop());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision != null && collision.tag == "Ground" && initialDelayOver)
        {
            StopAllCoroutines();
            StartCoroutine(Pop());
        }
    }
}
