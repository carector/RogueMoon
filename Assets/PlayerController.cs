using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Harpoons, metal, refinable resources
    // Tracked by save data
    [System.Serializable]
    public class PlayerResources
    {
        public int health = 8;
        public int harpoons;
    }

    [System.Serializable]
    public class PlayerMovementSettings
    {
        public bool canMove = true;
        public bool isGrounded;
        public float acceleration = 5;
        public float groundSpeed = 5;
        public float airSpeed = 3;
        public float pulledSpeed = 8;
    }

    [System.Serializable]
    public class PlayerAbilitySettings
    {
        public int activeAbility = 0;
        public bool aiming;
        public float harpoonRange = 10;
        public bool firingHarpoon;
        public bool beingPulledTowardsHarpoon;
        public float impactDelayTime = 0.35f;
        public float jumpDelayTime = 0.25f;
        public bool jumpDelayInProgress = false;
        public bool impactDelayInProgress = false;
    }

    public PlayerMovementSettings pMovement;
    public PlayerResources pResources;
    public PlayerAbilitySettings pAbilities;

    GameManager gm;
    SpriteRenderer harpoonEndpoint;
    SpriteRenderer harpoonChain;
    Rigidbody2D rb;
    SpriteRenderer bodySpr;
    SpriteRenderer armsSpr;
    Animator bodyAnim;
    Animator armsAnim;
    Transform harpoonStartPoint;
    AudioSource harpoonLoopingAudio;
    Vector2 mouseWorldPos;

    public LineRenderer lineTEMP;

    float gravityScale;
    bool hasRetractedArm;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        gravityScale = rb.gravityScale;
        bodySpr = transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>();
        bodyAnim = transform.GetChild(0).GetChild(0).GetComponent<Animator>();
        armsSpr = transform.GetChild(0).GetChild(1).GetComponent<SpriteRenderer>();
        armsAnim = transform.GetChild(0).GetChild(1).GetComponent<Animator>();
        harpoonEndpoint = GameObject.Find("HarpoonEndpoint").GetComponent<SpriteRenderer>();
        harpoonStartPoint = armsSpr.transform.GetChild(0);
        harpoonLoopingAudio= armsSpr.transform.GetChild(1).GetComponent<AudioSource>();
        harpoonChain = GameObject.Find("HarpoonChain").GetComponent<SpriteRenderer>();
        harpoonEndpoint.color = Color.clear;
        gm = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        bool lastGroundedState = pMovement.isGrounded;
        pMovement.isGrounded = CheckForGround();

        // Play impact animation if we just hit the ground
        if (lastGroundedState != pMovement.isGrounded)
        {
            if (!pAbilities.impactDelayInProgress && !pAbilities.jumpDelayInProgress)
            {
                if (pMovement.isGrounded)
                {
                    StartCoroutine(ImpactDelayCoroutine());
                }
                else
                    CheckAndPlayClip(bodyAnim, "Mech_Midair");
            }
        }
        MovePlayer();
        AddWaterResistanceForce();
        UpdateSprite();
    }

    private void Update()
    {
        mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lineTEMP.SetPositions(new Vector3[2] { transform.position, transform.position });
        RotateArm();
        CheckButtonInputs();
    }

    void MovePlayer()
    {
        if (pAbilities.beingPulledTowardsHarpoon || !pMovement.canMove || pAbilities.impactDelayInProgress)
            return;

        bodyAnim.SetFloat("WalkSpeed", Mathf.Clamp(rb.velocity.magnitude / 3, 0, pMovement.groundSpeed / 3f));
        armsAnim.SetFloat("WalkSpeed", Mathf.Clamp(rb.velocity.magnitude / 3, 0, pMovement.groundSpeed / 3f));

        float horiz = Input.GetAxis("Horizontal");
        float vert = Input.GetAxis("Vertical");
        Vector2 vel = Vector2.zero;

        // Can move up and down only if not grounded
        if (pMovement.isGrounded)
        {
            if (horiz == 0)
                vel = Vector2.zero;
            else
                vel = new Vector2(horiz, 0) * pMovement.acceleration * rb.mass;
            rb.gravityScale = gravityScale;
        }
        else
        {
            // Minor gravity + additional downwards force if we're airborne
            rb.gravityScale = 0.15f;
            vel = new Vector2(horiz, vert) * pMovement.acceleration * rb.mass;
            vel += vel = Vector2.down * pMovement.acceleration * rb.mass / 4;
        }

        if ((pMovement.isGrounded && rb.velocity.magnitude < pMovement.groundSpeed) || (!pMovement.isGrounded && rb.velocity.magnitude < pMovement.airSpeed))
        {
            // Slow robot down if we're grounded so we don't slide all over
            if (pMovement.isGrounded && vel.x == 0)
                rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, 0.25f);

            rb.AddForce(vel);
        }
    }

    void RotateArm()
    {
        if (pAbilities.firingHarpoon)
            return;

        bool lastAimState = pAbilities.aiming;

        if (Input.GetMouseButton(1))
        {
            pAbilities.aiming = true;
            CheckAndPlayClip(armsAnim, "Arm_Ready");

            // If we weren't aiming in the previous frame, fix rotation and scale to prevent weird rotation visual
            if (!lastAimState && armsSpr.transform.localScale.x == -1)
            {
                armsSpr.transform.rotation = Quaternion.Euler(0, 0, armsSpr.transform.rotation.eulerAngles.z + 180);
                armsSpr.transform.localScale = new Vector2(1, -1);
            }

            Vector2 mouseDir = (Vector3)mouseWorldPos - transform.position;
            float angle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
            armsSpr.transform.rotation = Quaternion.Lerp(armsSpr.transform.rotation, Quaternion.Euler(new Vector3(0, 0, angle)), 0.15f);
            hasRetractedArm = false;
        }
        else
        {
            pAbilities.aiming = false;
            if (!hasRetractedArm)
                CheckAndPlayClip(armsAnim, "Arm_Unready");

            // Same as above - If we were aiming in the previous frame, fix rotation and scale to prevent weird rotation visual
            if (lastAimState && armsSpr.transform.localScale.y == -1)
            {
                armsSpr.transform.rotation = Quaternion.Euler(0, 0, armsSpr.transform.rotation.eulerAngles.z + 180);
                armsSpr.transform.localScale = new Vector2(-1, 1);
            }
            armsSpr.transform.rotation = Quaternion.Lerp(armsSpr.transform.rotation, Quaternion.identity, 0.15f);
            hasRetractedArm = true;
        }
    }

    void CheckButtonInputs()
    {
        // Fire harpoon
        if (Input.GetMouseButtonDown(0) && !pAbilities.firingHarpoon && pAbilities.aiming)
        {
            pAbilities.firingHarpoon = true;
            StartCoroutine(FireHarpoon());
        }

        if(Input.GetKeyDown(KeyCode.Q))
        {
            pAbilities.activeAbility++;
            if (pAbilities.activeAbility > 1)
                pAbilities.activeAbility = 0;
        }

        // Jump
        if (Input.GetKeyDown(KeyCode.W) && pMovement.isGrounded && !pAbilities.jumpDelayInProgress && !pAbilities.impactDelayInProgress)
        {
            StartCoroutine(JumpDelayCoroutine());
        }
    }

    IEnumerator FireHarpoon()
    {
        harpoonEndpoint.color = Color.white;
        CheckAndPlayClip(armsAnim, "Arm_FireHarpoon");
        pResources.harpoons--;

        // Fire harpoon and wait for hit
        // IF hit object: Pull object towards player
        // IF hit terrain: Pull player towards point
        Vector3 dir = (Vector3)mouseWorldPos - harpoonStartPoint.position;
        Vector2 pos = harpoonStartPoint.position + dir * pAbilities.harpoonRange;

        harpoonEndpoint.transform.position = harpoonStartPoint.position;
        Vector2 mouseDir = (Vector3)mouseWorldPos - transform.position;
        float angle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
        harpoonEndpoint.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        Vector3 hitGroundPoint = Vector3.zero;
        Transform hitObject = null;
        Rigidbody2D hitRb = null;

        Vector2 previousPointPos = harpoonStartPoint.position;
        while (pAbilities.firingHarpoon && Vector2.Distance(harpoonEndpoint.transform.position, pos) > 1 && !pAbilities.beingPulledTowardsHarpoon)
        {
            harpoonEndpoint.transform.position = Vector2.MoveTowards(harpoonEndpoint.transform.position, pos, 0.5f);
            harpoonChain.size = new Vector2(Vector2.Distance(harpoonEndpoint.transform.position, harpoonStartPoint.position), 0.375f);
            harpoonChain.transform.position = (harpoonEndpoint.transform.position + harpoonStartPoint.position) / 2;
            harpoonChain.transform.right = (harpoonEndpoint.transform.position - harpoonStartPoint.position).normalized;

            Collider2D[] cols = Physics2D.OverlapBoxAll(harpoonEndpoint.transform.position - harpoonEndpoint.transform.right*0.125f, new Vector2(0.5f, 0.25f), harpoonEndpoint.transform.eulerAngles.z);
            if (cols.Length > 0)
            {
                foreach (Collider2D col in cols)
                {
                    if (col.tag.Equals("Ground"))
                    {
                        hitGroundPoint = col.ClosestPoint(harpoonEndpoint.transform.position);
                        pAbilities.beingPulledTowardsHarpoon = true;
                        break;
                    }
                    else if(col.tag.Equals("Harpoonable"))
                    {
                        gm.PlaySFX(gm.sfx.playerSounds[0]);
                        hitObject = col.transform;
                        harpoonEndpoint.transform.parent = col.transform;
                        hitRb = hitObject.GetComponent<Rigidbody2D>();
                        hitRb.velocity = (harpoonEndpoint.transform.position - hitObject.transform.position).normalized * 15;
                        hitRb.angularVelocity = Vector2.Angle(harpoonEndpoint.transform.position, hitObject.position);
                        break;
                    }
                }
            }
            if (hitGroundPoint != Vector3.zero || hitObject != null)
                break;

            yield return new WaitForFixedUpdate();
        }

        if (hitGroundPoint != Vector3.zero)
        {
            harpoonLoopingAudio.Play();
            if (pAbilities.beingPulledTowardsHarpoon)
            {
                float timePassed = 0;
                while (timePassed < 1.75f && Vector2.Distance(transform.position, hitGroundPoint) > 3 && !pMovement.isGrounded)
                {
                    harpoonChain.size = new Vector2(Vector2.Distance(harpoonEndpoint.transform.position, harpoonStartPoint.position), 0.375f);
                    harpoonChain.transform.position = (harpoonEndpoint.transform.position + transform.position) / 2;
                    harpoonChain.transform.right = (harpoonEndpoint.transform.position - harpoonStartPoint.position).normalized;

                    if (rb.velocity.magnitude < pMovement.pulledSpeed)
                        rb.AddForce((hitGroundPoint - transform.position) * rb.mass * pMovement.pulledSpeed);
                    timePassed += Time.fixedDeltaTime;

                    yield return new WaitForFixedUpdate();
                }
            }
        }
        else if(hitObject != null)
        {
            harpoonLoopingAudio.Play();
            pMovement.canMove = false;
            rb.velocity = new Vector2(0, -2);
            hitRb.angularVelocity = 0;
            float timePassed = 0;
            float angleBetweenHarpoon = Vector2.Angle(harpoonEndpoint.transform.forward, hitObject.transform.forward);
            while (timePassed < 1.75f && Vector2.Distance(hitObject.position, transform.position) > 3 && pMovement.isGrounded)
            {
                harpoonChain.size = new Vector2(Vector2.Distance(harpoonEndpoint.transform.position, harpoonStartPoint.position), 0.375f);
                harpoonChain.transform.position = (harpoonEndpoint.transform.position + transform.position) / 2;
                harpoonChain.transform.right = (harpoonEndpoint.transform.position - harpoonStartPoint.position).normalized;

                hitRb.transform.rotation = Quaternion.Lerp(hitRb.transform.rotation, Quaternion.Euler(0, 0, angleBetweenHarpoon), 0.25f);

                if (hitRb.velocity.magnitude < pMovement.pulledSpeed/2)
                    hitRb.AddForce((transform.position - hitObject.position) * hitRb.mass * pMovement.pulledSpeed/5);

                timePassed += Time.fixedDeltaTime;

                yield return new WaitForFixedUpdate();
            }
            pMovement.canMove = true;
            harpoonEndpoint.transform.parent = null;
        }

        harpoonLoopingAudio.Stop();
        harpoonEndpoint.color = Color.clear;
        harpoonEndpoint.transform.position = transform.position;
        harpoonChain.size = new Vector2(0, 0.375f);
        pAbilities.beingPulledTowardsHarpoon = false;
        pAbilities.firingHarpoon = false;
    }

    bool CheckForGround()
    {
        bool grounded = false;
        int mask = ~(1 << 8);
        //Debug.DrawRay(transform.position + Vector3.down * 2, Vector2.down*0.25f, Color.red, Time.fixedDeltaTime);
        if (Physics2D.Raycast(transform.position + Vector3.down * 2, Vector2.down, 0.35f, mask))
            grounded = true;

        return grounded;
    }

    IEnumerator ImpactDelayCoroutine()
    {
        pAbilities.impactDelayInProgress = true;
        CheckAndPlayClip(bodyAnim, "Mech_Impact");
        rb.velocity = new Vector2(0, -2);
        yield return new WaitForSeconds(pAbilities.impactDelayTime);
        pAbilities.impactDelayInProgress = false;
    }

    IEnumerator JumpDelayCoroutine()
    {
        pAbilities.jumpDelayInProgress = true;
        CheckAndPlayClip(bodyAnim, "Mech_Jump");
        rb.velocity = new Vector2(rb.velocity.x, 4.5f);
        yield return new WaitForSeconds(pAbilities.jumpDelayTime);
        pAbilities.jumpDelayInProgress = false;
    }

    void AddWaterResistanceForce()
    {
        rb.velocity -= rb.velocity.normalized * 0.05f;
    }

    void UpdateSprite()
    {
        if (pAbilities.firingHarpoon)
            return;

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (mousePosition.x < transform.position.x)
        {
            bodySpr.flipX = true;
            int dirX = 1;
            int dirY = 1;
            if (!pAbilities.aiming)
                dirX = -1;
            else
                dirY = -1;

            armsSpr.transform.localScale = new Vector3(dirX, dirY, 1);
        }
        else
        {
            bodySpr.flipX = false;
            armsSpr.transform.localScale = Vector3.one;
        }
    }

    public void TakeDamage()
    {
        pResources.health--;
    }

    public void CheckAndPlayClip(Animator anim, string clipName)
    {
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(clipName))
            anim.Play(clipName);
    }
}