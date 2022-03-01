using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PufferfishScript : Fish
{
    public bool attacking;
    public Sprite[] fishGibsSmall;

    // Start is called before the first frame update
    void Start()
    {
        GetReferences();
        StartCoroutine(MovementCycle());
    }

    // Update is called once per frame
    void Update()
    {
        movementSettings.noticedPlayer = CanSeePlayer();
    }

    public void BreakApart()
    {
        if (attacking)
            for (int i = 0; i < goreBits.Length / 2; i++)
                Instantiate(gib, transform.position, Quaternion.identity).GetComponent<GibScript>().InitializeGib(goreBits[Random.Range(0, goreBits.Length)], Random.Range(-50, 50));
        else
            foreach (Sprite s in fishGibsSmall)
                Instantiate(gib, transform.position, Quaternion.identity).GetComponent<GibScript>().InitializeGib(s, Random.Range(-50, 50));

        Destroy(this.gameObject);
    }

    public IEnumerator MovementCycle()
    {
        direction = (Vector3.right * Random.Range(-1, 1f) + (Vector3.up * Random.Range(-1, 1))).normalized;
        if (direction.x > 0)
            CheckAndPlayClip(animationPrefix + "_SwimRight");
        else
            CheckAndPlayClip(animationPrefix + "_SwimLeft");

        movementSettings.flipX = direction.x <= 0;

        while (true)
        {
            if (!movementSettings.noticedPlayer || ply.pResources.health <= 0)
            {
                // Move in one direction until we leave the swim area
                rb.AddForce(direction * movementSettings.acceleration);
                rb.velocity = Vector2.Lerp(rb.velocity, Vector2.ClampMagnitude(rb.velocity, movementSettings.swimSpeed), 0.25f);

                // Flip if we're outside the bounds of the swim area
                if (!movementSettings.inSwimBounds || ShouldReturnToHitZone())
                {
                    rb.velocity = Vector2.zero;

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
            }
            else
            {
                Vector2 plyDirection = (ply.transform.position - transform.position).normalized;
                rb.AddForce(plyDirection * movementSettings.acceleration);
                rb.velocity = Vector2.Lerp(rb.velocity, Vector2.ClampMagnitude(rb.velocity, movementSettings.noticedPlayerSwimSpeed), 0.1f);

                if (plyDirection.x > 0)
                    CheckAndPlayClip(animationPrefix + "_SwimRight");
                else
                    CheckAndPlayClip(animationPrefix + "_SwimLeft");

                // Attack player if we're close enough to do so
                if (Vector2.Distance(ply.transform.position, transform.position) < movementSettings.attackDistance)
                {
                    attacking = true;
                    rb.velocity = plyDirection * 3;
                    CheckAndPlayClip(animationPrefix + "_Attack");

                    yield return new WaitForSeconds(1.75f);
                    attacking = false;
                }
            }

            yield return new WaitForFixedUpdate();
        }
    }
}
