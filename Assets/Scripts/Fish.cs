using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fish : MonoBehaviour
{
    [System.Serializable]
    public class FishMovementSettings
    {
        public bool flipX;
        public int health = 1;
        public float acceleration = 10;
        public float swimSpeed = 3;
        public float noticedPlayerSwimSpeed = 4;
        public bool inSwimBounds = true;
        public bool noticedPlayer;
        public float damageRadius;
        public Vector2 damageCircleOffset;
        public float noticeDistance;
        public float attackDistance;
        public LayerMask playerLayermask;
    }

    public FishMovementSettings movementSettings;
    public Collider2D swimAreaBounds;
    public GameObject gib;
    public GameObject bloodGib;
    public Sprite[] goreBits;
    public Color bloodColor;
    public string animationPrefix = "Fish";
    public Vector2 direction;

    protected Rigidbody2D rb;
    protected PlayerController ply;
    protected Animator anim;
    protected SpriteRenderer spr;
    protected GameManager gm;
    protected Rigidbody2D prb;
    protected Collider2D col;

    protected float remainingPathTimer = 6;
    bool bumpedPlayerCooldownInProgress;

    Transform harpoonEndpoint;

    // Start is called before the first frame update
    void Start()
    {
        GetReferences();
        StartCoroutine(MovementCycle());
    }

    // Called by any subclasses
    public void GetReferences()
    {
        harpoonEndpoint = GameObject.Find("HarpoonEndpoint").transform;
        spr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        ply = FindObjectOfType<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        prb = ply.GetComponent<Rigidbody2D>();
        gm = FindObjectOfType<GameManager>();
        anim = GetComponent<Animator>();
        if(anim != null)
            anim.SetFloat("SwimSpeed", 1);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TakeDamage(int damage)
    {
        movementSettings.health -= damage;
        if (rb != null)
        {
            Vector2 dir = (transform.position - harpoonEndpoint.position).normalized;
            rb.velocity += dir * 10;
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, 10);
        }
        StartCoroutine(DamageFlashCoroutine());
        if (movementSettings.health <= 0)
            BreakApart();
    }

    protected virtual IEnumerator DamageFlashCoroutine()
    {
        for (int i = 0; i < 3; i++)
        {
            spr.color = Color.red;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            spr.color = Color.white;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }
    }

    public virtual void BreakApart()
    {
        foreach (Sprite s in goreBits)
            Instantiate(gib, transform.position, Quaternion.identity).GetComponent<GibScript>().InitializeGib(s, Random.Range(-50, 50));
        if (bloodGib != null)
        {
            ParticleSystem p = Instantiate(bloodGib, transform.position, Quaternion.identity).GetComponent<ParticleSystem>();
            p.startColor = new Color(bloodColor.r, bloodColor.g, bloodColor.b, 50f / 255f);
            p.Play();
        }

        Destroy(this.gameObject);
    }

    public bool CanSeePlayer()
    {
        bool inLOS = false;
        Vector2 dir = (ply.transform.position - transform.position).normalized;
        Debug.DrawRay(transform.position, dir * movementSettings.noticeDistance, Color.red, Time.fixedDeltaTime);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, movementSettings.noticeDistance, movementSettings.playerLayermask);
        if (hit.transform != null && hit.transform.tag == "Player")
            inLOS = true;
        return inLOS;
    }

    // Can be overriden by any subclasses
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
            remainingPathTimer -= Time.deltaTime;

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
                remainingPathTimer = 6;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    public void CheckAndPlayClip(string clipName)
    {
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(clipName))
            anim.Play(clipName, 0, 0);
    }

    public void DamagePlayer()
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, movementSettings.damageRadius);
        foreach (Collider2D c in cols)
        {
            if (c.tag == "Player")
            {
                ply.TakeDamage();
                break;
            }
        }
    }

    public bool ShouldReturnToHitZone()
    {
        bool state = Vector2.Distance(transform.position, swimAreaBounds.ClosestPoint(transform.position)) > 10;
        state = state || remainingPathTimer <= 0;
        return state;
    }


    public void BecomeHazardForTime(float time)
    {
        StartCoroutine(BecomeHazardForTimeCoroutine(time));
    }

    protected IEnumerator Flip()
    {
        if (direction.x <= 0 && !movementSettings.flipX)
            CheckAndPlayClip(animationPrefix + "_Flip");
        else if (direction.x > 0 && movementSettings.flipX)
            CheckAndPlayClip(animationPrefix + "_FlipReverse");

        yield return new WaitForSeconds(anim.GetCurrentAnimatorClipInfo(0)[0].clip.length);
        movementSettings.flipX = direction.x <= 0;
    }

    IEnumerator BecomeHazardForTimeCoroutine(float time)
    {
        while (time > 0)
        {
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position + (Vector3)movementSettings.damageCircleOffset, movementSettings.damageRadius);
            foreach (Collider2D c in cols)
            {
                if (c.tag == "Player")
                {
                    Collider2D col = GetComponent<Collider2D>();
                    Rigidbody2D prb = ply.GetComponent<Rigidbody2D>();
                    prb.velocity = (ply.transform.position - (Vector3)col.ClosestPoint(ply.transform.position)).normalized * 4;
                    ply.TakeDamage();
                    break;
                }
            }
            time -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision == swimAreaBounds)
            movementSettings.inSwimBounds = false;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Player" && !bumpedPlayerCooldownInProgress)
        {
            BumpPlayer();
        }
    }

    void BumpPlayer()
    {
        Vector3 dir = (ply.transform.position - (Vector3)col.ClosestPoint(ply.transform.position)).normalized * 2.5f;
        prb.velocity = dir;
        rb.velocity = -dir;
        StartCoroutine(BumpDelay());
    }

    IEnumerator BumpDelay()
    {
        bumpedPlayerCooldownInProgress = true;
        yield return new WaitForSeconds(0.5f);
        bumpedPlayerCooldownInProgress = false;
    }
}
