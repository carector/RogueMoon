using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PufferfishScript : Fish
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
        movementSettings.noticedPlayer = (Vector2.Distance(transform.position, ply.transform.position) < 8);
    }

    public IEnumerator MovementCycle()
    {
        direction = (Vector3.right * Random.Range(-1, 1f) + (Vector3.up * Random.Range(-1, 1))).normalized;
        if (direction.x > 0)
            CheckAndPlayClip(animationPrefix + "_SwimRight");
        else
            CheckAndPlayClip(animationPrefix + "_SwimLeft");

        while (true)
        {
            if (!movementSettings.noticedPlayer)
            {
                // Move in one direction until we leave the swim area
                rb.AddForce(direction * movementSettings.acceleration);
                rb.velocity = Vector2.Lerp(rb.velocity, Vector2.ClampMagnitude(rb.velocity, movementSettings.swimSpeed), 0.1f);

                // Flip if we're outside the bounds of the swim area
                if (!movementSettings.inSwimBounds)
                {
                    rb.velocity = Vector2.zero;

                    if (direction.x > 0)
                        CheckAndPlayClip(animationPrefix + "_Flip");
                    else
                        CheckAndPlayClip(animationPrefix + "_FlipReverse");

                    yield return new WaitForSeconds(anim.GetCurrentAnimatorClipInfo(0)[0].clip.length);

                    // Flip direction and add randomness
                    direction = (swimAreaBounds.bounds.center - transform.position);
                    direction += new Vector2(Random.Range(-1, 1), Random.Range(-1, 1) * 3);
                    direction.Normalize();

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
                if(Vector2.Distance(ply.transform.position, transform.position) < 4)
                {
                    rb.velocity = Vector2.zero;
                    CheckAndPlayClip(animationPrefix + "_Attack");
                    yield return new WaitForSeconds(2);
                }
            }

            yield return new WaitForFixedUpdate();
        }
    }
}
