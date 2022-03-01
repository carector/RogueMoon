using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Kissyfish : Fish
{
    public bool kissing;
    bool canSmooch = true;

    // Start is called before the first frame update
    void Start()
    {
        GetReferences();
        StartCoroutine(MovementCycle());
    }

    // Update is called once per frame
    void Update()
    {

    }

    public IEnumerator MovementCycle()
    {
        // Default movement example
        direction = (Vector3.right * Random.Range(-1, 1f) + (Vector3.up * Random.Range(-1, 1))).normalized;
        if (direction.x > 0)
            CheckAndPlayClip(animationPrefix + "_SwimRight");
        else
            CheckAndPlayClip(animationPrefix + "_SwimLeft");

        while (true)
        {
            // Move in one direction until we leave the swim area
            rb.AddForce(direction * movementSettings.acceleration);
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.ClampMagnitude(rb.velocity, movementSettings.swimSpeed), 0.1f);
            anim.SetFloat("SwimSpeed", 1);

            // Flip if we're outside the bounds of the swim area
            if (!movementSettings.inSwimBounds || ShouldReturnToHitZone())
            {
                rb.velocity = Vector2.zero;

                // Check for smooch
                float smoochChance = Random.Range(0, 1f);
                if (CanSeePlayer() && smoochChance > 0.7f)
                {
                    // Another preliminary smooch check
                    if (SmoochZoneUnoccupied() && canSmooch)
                    {
                        kissing = true;
                        yield return SmoochSequence();
                        kissing = false;
                    }
                }

                // Flip direction and add randomness
                direction = (swimAreaBounds.bounds.center - transform.position);
                direction += new Vector2(Random.Range(-1, 1), Random.Range(-1, 1) * 3);
                direction.Normalize();
                yield return Flip();

                if (direction.x > 0)
                    CheckAndPlayClip(animationPrefix + "_SwimRight");
                else
                    CheckAndPlayClip(animationPrefix + "_SwimLeft");

                rb.velocity = direction * movementSettings.swimSpeed;
                movementSettings.inSwimBounds = true;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    bool SmoochZoneUnoccupied()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 25);
        for (int i = 0; i < hits.Length; i++)
            if (hits[i].tag == "Kissyfish" && hits[i].GetComponent<Kissyfish>().kissing)
                return false;
        return true;
    }

    IEnumerator SmoochCooldown()
    {
        canSmooch = false; // :(
        yield return new WaitForSeconds(3);
        canSmooch = true;  // :)
    }

    IEnumerator SmoochSequence()
    {
        transform.parent = ply.transform;
        float dir = transform.position.x - ply.transform.position.x;
        if (dir < 0)
            CheckAndPlayClip(animationPrefix + "_SwimRight");
        else
            CheckAndPlayClip(animationPrefix + "_SwimLeft");

        movementSettings.flipX = direction.x < 0;
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Approach
        Vector3 targetPos = new Vector2(2.2f * Mathf.Sign(dir), 0.5f);
        while (Vector2.Distance(transform.localPosition, targetPos) > 0.05f)
        {
            transform.localPosition = Vector2.Lerp(transform.localPosition, targetPos, 0.1f);
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForSeconds(0.25f);

        // Smuch
        gm.PlaySFX(gm.sfx.generalSounds[2]);
        gm.SpawnSmoochMark();
        targetPos = new Vector2(1.7f * Mathf.Sign(dir), 0.5f);
        float timer = 0.35f;
        while (timer > 0)
        {
            transform.localPosition = Vector2.Lerp(transform.localPosition, targetPos, 0.5f);
            timer -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Desmuch
        targetPos = new Vector2(2.2f * Mathf.Sign(dir), 0.5f);
        while (Vector2.Distance(transform.localPosition, targetPos) > 0.05f)
        {
            transform.localPosition = Vector2.Lerp(transform.localPosition, targetPos, 0.5f);
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForSeconds(0.5f);
        transform.parent = null;
        rb.bodyType = RigidbodyType2D.Dynamic;
        StartCoroutine(SmoochCooldown());
    }
}
