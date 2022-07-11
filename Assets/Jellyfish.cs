using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jellyfish : Fish
{
    // Start is called before the first frame update
    void Start()
    {
        GetReferences();
        StartCoroutine(MovementCycle());
    }

    // Update is called once per frame
    void Update()
    {
        if (rb.velocity.y < 0)
            rb.velocity = Vector2.ClampMagnitude(rb.velocity, 2);
    }
    public IEnumerator MovementCycle()
    {
        Bounds swimBounds = swimAreaBounds.bounds;
        print(swimBounds.min.y + 3);
        while (true)
        {
            while (transform.position.y > swimBounds.max.y - 2)
                yield return null;

            yield return new WaitForSeconds(0.1f * (transform.position.y - swimBounds.min.y));
            print("Played animation");
            anim.Play(animationPrefix+"_Swim", 0, 0);

            yield return new WaitForSeconds(0.25f);
            while (rb.velocity.y > 1)
            {
                rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, 0.05f);
                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(Random.Range(0.5f, 1.25f));
        }
    }

    public void Bob()
    {
        rb.velocity = new Vector2(Random.Range(0, 4f) * Mathf.Sign(swimAreaBounds.bounds.center.x - transform.position.x), Random.Range(4, 6f)*(0.25f*(swimAreaBounds.bounds.max.y - transform.position.y)));
    }
}
