using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FantasiaScript : Fish
{
    public bool attacking;
    bool canAttack = true;

    SpriteRenderer spr;
    // Start is called before the first frame update
    void Start()
    {
        spr = GetComponent<SpriteRenderer>();
        GetReferences();
        StartCoroutine(MovementCycle());
    }

    private void Update()
    {
        movementSettings.noticedPlayer = CanSeePlayer();
    }

    IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(1.5f);
        canAttack = true;
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

            Vector2 plyDirection = (ply.transform.position - transform.position).normalized;
            rb.AddForce(plyDirection * movementSettings.acceleration);
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.ClampMagnitude(rb.velocity, movementSettings.noticedPlayerSwimSpeed), 0.1f);

            if (plyDirection.x > 0)
                CheckAndPlayClip(animationPrefix + "_SwimRight");
            else
                CheckAndPlayClip(animationPrefix + "_SwimLeft");

            // Attack player if we're close enough to do so
            if (canAttack && Vector2.Distance(ply.transform.position, transform.position) < movementSettings.attackDistance)
            {
                attacking = true;
                rb.velocity = Vector2.zero;
                CheckAndPlayClip(animationPrefix + "_Attack");

                // Flip if we're facing left
                if (plyDirection.x < 0)
                {
                    spr.flipY = true;
                    transform.GetChild(0).localScale = new Vector2(-1, 1);
                    transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z + 180);
                }

                // Rotate towards player before charging
                float timer = anim.GetCurrentAnimatorClipInfo(0)[0].clip.length + 0.25f;
                while (timer > 0)
                {
                    Vector2 dir = ply.transform.position - transform.position;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), 0.25f);
                    spr.flipY = (dir.x < 0);
                    rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, 0.1f);
                    timer -= Time.fixedDeltaTime;
                    yield return new WaitForFixedUpdate();
                }
                rb.velocity = plyDirection * 14;
                BecomeHazardForTime(1.5f);
                yield return new WaitForSeconds(1.5f);
                transform.rotation = Quaternion.identity;
                spr.flipY = false;
                attacking = false;
                StartCoroutine(AttackCooldown());
            }

            yield return new WaitForFixedUpdate();
        }
    }
}
