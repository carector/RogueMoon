using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabbableObject : Grabbable
{
    protected Collider2D col;
    protected Rigidbody2D rb;
    Animator anim;
    bool decaying;
    // Start is called before the first frame update
    void Start()
    {
        GetReferences();
    }

    protected void GetReferences()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!col.enabled)
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, 0.25f);
    }

    public override void GetPulledByHarpoon()
    {
        col.isTrigger = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        if (!decaying)
        {
            decaying = true;
            StartCoroutine(DecayOverTime());
        }
    }

    IEnumerator DecayOverTime()
    {
        yield return new WaitForSeconds(8);
        anim.Play("GrabbableFade");
        yield return new WaitForSeconds(4);
        Destroy(this.gameObject);
    }

    public override void GetReleasedByHarpoon(Vector2 velocity)
    {
        col.isTrigger = false;
        transform.parent = null;
        rb.bodyType = RigidbodyType2D.Dynamic;
        print(velocity);
        rb.velocity = velocity;
    }

    public void GetGrabbedByReceptor()
    {
        col.enabled = false;
        StopAllCoroutines();
        anim.Play("GrabbableIdle");
    }
}
