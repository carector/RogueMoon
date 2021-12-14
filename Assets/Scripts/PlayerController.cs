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
        public int depthCharges;
        public int metal;
    }

    [System.Serializable]
    public class PlayerMovementSettings
    {
        public bool canMove = true;
        public bool isGrounded;
        public bool isTouchingCeiling;
        public float hoverTime = 4;
        public float acceleration = 5;
        public float groundSpeed = 5;
        public float airSpeed = 3;
        public float pulledSpeed = 8;
        public PhysicsMaterial2D groundedMaterial;
        public PhysicsMaterial2D airMaterial;
    }

    [System.Serializable]
    public class PlayerAbilitySettings
    {
        public int activeAbility = 0;
        public bool aiming;
        public float harpoonRange = 10;
        public bool firingHarpoon;
        public bool attacking;
        public int attackCharges = 2;
        public bool beingPulledTowardsHarpoon;
        public float impactDelayTime = 0.35f;
        public float jumpDelayTime = 0.25f;
        public float attackDelayTime = 0.25f;
        public float attackRecoveryTime = 3f;
        public float swapToolDelayTime = 0.35f;
        public bool jumpDelayInProgress = false;
        public bool impactDelayInProgress = false;
        public bool attackDelayInProgress = false;
        public bool swapToolDelayInProgress = false;
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
    Transform damageStartPoint;
    AudioSource harpoonLoopingAudio;
    AudioSource attackLoopingAudio;
    ParticleSystem[] steamParticles;
    Vector2 mouseWorldPos;

    public LineRenderer lineTEMP;

    float gravityScale;
    bool hasRetractedArm;
    float storedHoverTime;
    int mask = ~((1 << 8) | (1 << 10) | (1 << 9)); // Ground + ceiling raycast layermask
    bool harpoonStartingGroundedState;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        storedHoverTime = pMovement.hoverTime;
        gravityScale = rb.gravityScale;
        bodySpr = transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>();
        bodyAnim = transform.GetChild(0).GetChild(0).GetComponent<Animator>();
        armsSpr = transform.GetChild(0).GetChild(1).GetComponent<SpriteRenderer>();
        armsAnim = transform.GetChild(0).GetChild(1).GetComponent<Animator>();
        harpoonEndpoint = GameObject.Find("HarpoonEndpoint").GetComponent<SpriteRenderer>();
        harpoonStartPoint = armsSpr.transform.GetChild(0);
        harpoonLoopingAudio = armsSpr.transform.GetChild(1).GetComponent<AudioSource>();
        attackLoopingAudio = armsSpr.transform.GetChild(2).GetComponent<AudioSource>();
        steamParticles = new ParticleSystem[2];
        steamParticles[0] = armsSpr.transform.GetChild(3).GetComponent<ParticleSystem>();
        steamParticles[1] = armsSpr.transform.GetChild(4).GetComponent<ParticleSystem>();
        damageStartPoint = armsSpr.transform.GetChild(5).transform;
        harpoonChain = GameObject.Find("HarpoonChain").GetComponent<SpriteRenderer>();
        harpoonEndpoint.color = Color.clear;
        gm = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        bool lastGroundedState = pMovement.isGrounded;
        pMovement.isGrounded = (CheckForGround() && !harpoonStartingGroundedState);
        pMovement.isTouchingCeiling = CheckForCeiling();

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

        if (pMovement.isGrounded)
            rb.sharedMaterial = pMovement.groundedMaterial;
        else
            rb.sharedMaterial = pMovement.airMaterial;

        if (Time.timeScale == 0)
            return;

        MovePlayer();
        AddWaterResistanceForce();
        UpdateSprite();
    }

    private void Update()
    {
        mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lineTEMP.SetPositions(new Vector3[2] { transform.position, transform.position });

        if (Time.timeScale == 0)
            return;

        RotateArm();
        CheckButtonInputs();
    }

    void MovePlayer()
    {
        if (pAbilities.beingPulledTowardsHarpoon || pAbilities.impactDelayInProgress)
            return;

        bodyAnim.SetFloat("WalkSpeed", Mathf.Clamp(rb.velocity.magnitude / 3, 0, pMovement.groundSpeed / 3f));
        armsAnim.SetFloat("WalkSpeed", Mathf.Clamp(rb.velocity.magnitude / 3, 0, pMovement.groundSpeed / 3f));

        float horiz = Input.GetAxis("Horizontal");
        float vert = Input.GetAxis("Vertical");
        if (!pMovement.canMove)
        {
            horiz = 0;
            vert = 0;
        }

        Vector2 vel = Vector2.zero;

        // Can move up and down only if not grounded
        if (pMovement.isGrounded)
        {
            storedHoverTime = pMovement.hoverTime;

            if (horiz == 0)
                vel = Vector2.zero;
            else
                vel = new Vector2(horiz, 0) * pMovement.acceleration * rb.mass;
            rb.gravityScale = gravityScale;
        }
        else
        {
            if (vert > 0 && storedHoverTime > 0)
            {
                CheckAndPlayClip(bodyAnim, "Mech_Midair");
                storedHoverTime -= Time.fixedDeltaTime;
                if (storedHoverTime <= 0)
                {
                    gm.PlaySFX(gm.sfx.playerSounds[2]);
                    CheckAndPlayClip(bodyAnim, "Mech_NoThrust");
                }
            }
            else if (storedHoverTime <= 0)
                vert = Mathf.Clamp(vert, -1, 0);
            else if (vert <= 0)
                CheckAndPlayClip(bodyAnim, "Mech_NoThrust");

            // Minor gravity + additional downwards force if we're airborne
            rb.gravityScale = 0.35f;
            vel = new Vector2(horiz * 0.75f, Mathf.Clamp(vert, -0.25f, 0.75f)) * pMovement.acceleration * rb.mass;
            vel.x /= 2;
            //vel += vel = Vector2.down * pMovement.acceleration * rb.mass / 4;
        }

        if ((pMovement.isGrounded && rb.velocity.magnitude < pMovement.groundSpeed))
        {
            // Slow robot down if we're grounded so we don't slide all over
            if (pMovement.isGrounded && vel.x == 0)
                rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, 0.25f);

            rb.AddForce(vel);
        }
        else if (!pMovement.isGrounded)
        {
            if (rb.velocity.x < pMovement.airSpeed && rb.velocity.x > -pMovement.airSpeed)
                rb.AddForce(Vector2.right * vel.x);
            if (rb.velocity.y < pMovement.airSpeed && rb.velocity.y > -pMovement.airSpeed * 1.75f)
                rb.AddForce(Vector2.up * vel.y);

            //rb.velocity = Vector2.Lerp(rb.velocity, new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -pMovement.airSpeed * 1.75f, pMovement.airSpeed)), 0.1f);
        }
    }

    void RotateArm()
    {
        if (pAbilities.firingHarpoon || pAbilities.swapToolDelayInProgress)
            return;

        bool lastAimState = pAbilities.aiming;

        if (Input.GetMouseButton(1) && !pAbilities.attacking && !pAbilities.attackDelayInProgress && pMovement.canMove)
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
        if (!pMovement.canMove)
            return;

        if (pAbilities.aiming)
        {
            // Fire harpoon
            if (pAbilities.activeAbility == 0 && Input.GetMouseButtonDown(0) && !pAbilities.firingHarpoon)
            {
                pAbilities.firingHarpoon = true;
                StartCoroutine(FireHarpoon());
            }
        }
        else
        {
            // Attack
            if (pAbilities.activeAbility == 1 && Input.GetMouseButtonDown(0) && !pAbilities.attacking && !pAbilities.attackDelayInProgress)
            {
                pAbilities.attacking = true;
                StartCoroutine(Attack());
            }
        }

        // Swap current tool (when aiming vs not aiming)
        if (!pAbilities.firingHarpoon && !pAbilities.swapToolDelayInProgress && !pAbilities.attackDelayInProgress)
        {
            if (pAbilities.aiming && pAbilities.activeAbility == 1)
                SetActiveAbility(0);
            else if (!pAbilities.aiming && pAbilities.activeAbility == 0)
                SetActiveAbility(1);
        }

        // Reload
        if (pAbilities.activeAbility == 1 && Input.GetKeyDown(KeyCode.R) && !pAbilities.attacking && !pAbilities.attackDelayInProgress && pAbilities.attackCharges != 2)
            StartCoroutine(RechargeAttack());

        // Jump
        if (!pMovement.isTouchingCeiling && !pAbilities.firingHarpoon && Input.GetKeyDown(KeyCode.W) && pMovement.isGrounded && !pAbilities.jumpDelayInProgress && !pAbilities.impactDelayInProgress)
        {
            StartCoroutine(JumpDelayCoroutine());
        }
    }

    void SetActiveAbility(int ability)
    {
        gm.PlaySFX(gm.sfx.playerSounds[1]);
        gm.SwitchActiveToolHUD();
        pAbilities.activeAbility = ability;
        //StartCoroutine(SwitchToolDelayCoroutine());
    }

    IEnumerator FireHarpoon()
    {
        harpoonEndpoint.color = Color.white;
        CheckAndPlayClip(armsAnim, "Arm_FireHarpoon");
        if (pResources.harpoons > 0)
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
            harpoonEndpoint.transform.position = Vector2.MoveTowards(harpoonEndpoint.transform.position, pos, 0.75f);
            harpoonChain.size = new Vector2(Vector2.Distance(harpoonEndpoint.transform.position, harpoonStartPoint.position), 0.375f);
            harpoonChain.transform.position = (harpoonEndpoint.transform.position + harpoonStartPoint.position) / 2;
            harpoonChain.transform.right = (harpoonEndpoint.transform.position - harpoonStartPoint.position).normalized;

            Collider2D[] cols = Physics2D.OverlapBoxAll(harpoonEndpoint.transform.position - harpoonEndpoint.transform.right * 0.25f, new Vector2(1f, 0.25f), harpoonEndpoint.transform.eulerAngles.z);
            if (cols.Length > 0)
            {
                foreach (Collider2D col in cols)
                {
                    if (col.tag.Equals("Ground") || col.tag.Equals("Breakable"))
                    {
                        hitGroundPoint = col.ClosestPoint(harpoonEndpoint.transform.position);
                        pAbilities.beingPulledTowardsHarpoon = true;
                        break;
                    }
                    else if (col.tag.Equals("Harpoonable"))
                    {
                        gm.PlaySFX(gm.sfx.playerSounds[0]);
                        hitObject = col.transform;
                        harpoonEndpoint.transform.parent = col.transform;
                        hitRb = hitObject.GetComponent<Rigidbody2D>();
                        break;
                    }
                }
            }
            if (hitGroundPoint != Vector3.zero || hitObject != null)
                break;

            yield return new WaitForFixedUpdate();
        }
        harpoonLoopingAudio.loop = true;

        // Pull towards ground point
        if (hitGroundPoint != Vector3.zero)
        {
            harpoonStartingGroundedState = CheckForGround();
            harpoonLoopingAudio.Play();
            pMovement.isGrounded = false;
            CheckAndPlayClip(bodyAnim, "Mech_NoThrust");

            if (pAbilities.beingPulledTowardsHarpoon)
            {
                float timePassed = 0;
                while (timePassed < 1.75f && Vector2.Distance(transform.position, hitGroundPoint) > 3 && Input.GetMouseButton(0) && pAbilities.aiming && !pMovement.isGrounded)
                {
                    harpoonLoopingAudio.pitch = 1.25f - (Mathf.Clamp(Vector2.Distance(transform.position, harpoonEndpoint.transform.position) / 28, 0, 0.25f));
                    harpoonChain.size = new Vector2(Vector2.Distance(harpoonEndpoint.transform.position, harpoonStartPoint.position), 0.375f);
                    harpoonChain.transform.position = (harpoonEndpoint.transform.position + transform.position) / 2;
                    harpoonChain.transform.right = (harpoonEndpoint.transform.position - harpoonStartPoint.position).normalized;

                    if (harpoonStartingGroundedState)
                        harpoonStartingGroundedState = CheckForGround();

                    if (rb.velocity.magnitude < pMovement.pulledSpeed)
                        rb.AddForce((hitGroundPoint - transform.position) * rb.mass * pMovement.pulledSpeed);
                    timePassed += Time.fixedDeltaTime;

                    yield return new WaitForFixedUpdate();
                }
            }
        }
        // Pull object towards player
        else if (hitObject != null)
        {
            bodyAnim.SetFloat("WalkSpeed", 0);
            armsAnim.SetFloat("WalkSpeed", 0);
            harpoonLoopingAudio.Play();
            pMovement.canMove = false;
            rb.velocity = new Vector2(0, -2);
            hitRb.angularVelocity = 0;
            float timePassed = 0;
            float angleBetweenHarpoon = Vector2.Angle(harpoonEndpoint.transform.forward, hitObject.transform.forward);
            while (timePassed < 1.75f && Vector2.Distance(hitObject.position, transform.position) > 3)
            {
                harpoonLoopingAudio.pitch = 1.25f - (Mathf.Clamp(Vector2.Distance(transform.position, harpoonEndpoint.transform.position) / 28, 0, 0.25f));
                harpoonChain.size = new Vector2(Vector2.Distance(harpoonEndpoint.transform.position, harpoonStartPoint.position), 0.375f);
                harpoonChain.transform.position = (harpoonEndpoint.transform.position + transform.position) / 2;
                harpoonChain.transform.right = (harpoonEndpoint.transform.position - harpoonStartPoint.position).normalized;

                //hitRb.transform.rotation = Quaternion.Lerp(hitRb.transform.rotation, Quaternion.Euler(0, 0, angleBetweenHarpoon), 0.25f);

                if (hitRb.velocity.magnitude < pMovement.pulledSpeed)
                    hitRb.AddForce((transform.position - hitObject.position) * hitRb.mass * pMovement.pulledSpeed / 6);

                timePassed += Time.fixedDeltaTime;

                yield return new WaitForFixedUpdate();
            }
            pMovement.canMove = true;
        }

        harpoonStartingGroundedState = false;
        harpoonEndpoint.transform.parent = null;
        harpoonLoopingAudio.loop = false;
        harpoonEndpoint.color = Color.clear;
        harpoonEndpoint.transform.position = transform.position;
        harpoonChain.size = new Vector2(0, 0.375f);
        pAbilities.beingPulledTowardsHarpoon = false;
        pAbilities.firingHarpoon = false;
    }

    bool CheckForGround()
    {
        bool grounded = false;
        Debug.DrawRay(transform.position + Vector3.down * 2, Vector2.down * 0.25f, Color.red, Time.fixedDeltaTime);
        RaycastHit2D hit = Physics2D.Raycast(transform.position + Vector3.down * 2, Vector2.down, 0.35f, mask);
        if (hit.transform != null && hit.transform.tag == "Ground")
            grounded = true;

        return grounded;
    }

    bool CheckForCeiling()
    {
        bool ceilinged = false;
        Debug.DrawRay(transform.position + Vector3.up, Vector2.up * 0.25f, Color.red, Time.fixedDeltaTime);
        Collider2D[] cols = Physics2D.OverlapBoxAll(transform.position + Vector3.up * 1f, new Vector2(0.75f, 0.5f), 0, mask);
        foreach (Collider2D col in cols)
        {
            if (col.transform != null && col.transform.tag == "Ground")
            {
                ceilinged = true;
                break;
            }
        }

        return ceilinged;
    }

    IEnumerator ImpactDelayCoroutine()
    {
        pAbilities.impactDelayInProgress = true;
        CheckAndPlayClip(bodyAnim, "Mech_Impact");
        rb.velocity = new Vector2(0, -2);
        yield return new WaitForSeconds(pAbilities.impactDelayTime);
        pAbilities.impactDelayInProgress = false;
    }

    IEnumerator Attack()
    {
        pAbilities.attackCharges--;
        CheckAndPlayClip(armsAnim, "Arm_Attack");

        /*if (armsSpr.transform.localScale.x == -1)
        {
            armsSpr.transform.rotation = Quaternion.Euler(0, 0, armsSpr.transform.rotation.eulerAngles.z + 180);
            armsSpr.transform.localScale = new Vector2(1, -1);
        }

        Vector2 mouseDir = (Vector3)mouseWorldPos - transform.position;
        float angle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
        armsSpr.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        */
        Collider2D[] cols = Physics2D.OverlapCapsuleAll(damageStartPoint.position, new Vector2(2, 0.5f), CapsuleDirection2D.Horizontal, armsAnim.transform.rotation.eulerAngles.z);
        foreach (Collider2D c in cols)
        {
            if (c.tag == "Breakable")
            {
                c.SendMessage("BreakApart");
            }
        }

        yield return new WaitForSeconds(pAbilities.attackDelayTime);
        /*
        if (armsSpr.transform.localScale.y == -1)
        {
            armsSpr.transform.rotation = Quaternion.Euler(0, 0, armsSpr.transform.rotation.eulerAngles.z + 180);
            armsSpr.transform.localScale = new Vector2(-1, 1);
        }
        armsSpr.transform.rotation = Quaternion.identity;*/
        CheckAndPlayClip(armsAnim, "Arm_Walk");
        pAbilities.attacking = false;
        if (pAbilities.attackCharges == 0)
            StartCoroutine(RechargeAttack());
    }

    IEnumerator RechargeAttack()
    {
        pAbilities.attackDelayInProgress = true;
        attackLoopingAudio.Play();
        steamParticles[0].Play();
        steamParticles[1].Play();
        yield return new WaitForSeconds(pAbilities.attackRecoveryTime - (pAbilities.attackRecoveryTime * 0.5f * pAbilities.attackCharges));
        gm.PlaySFX(gm.sfx.playerSounds[0]);
        attackLoopingAudio.Stop();
        steamParticles[0].Stop();
        steamParticles[1].Stop();
        pAbilities.attackCharges = 2;
        pAbilities.attackDelayInProgress = false;
    }

    IEnumerator JumpDelayCoroutine()
    {
        pAbilities.jumpDelayInProgress = true;
        CheckAndPlayClip(bodyAnim, "Mech_Jump");
        rb.velocity = new Vector2(rb.velocity.x, 4.5f);
        yield return new WaitForSeconds(pAbilities.jumpDelayTime);
        pAbilities.jumpDelayInProgress = false;
    }

    IEnumerator SwitchToolDelayCoroutine()
    {
        pAbilities.swapToolDelayInProgress = true;
        yield return new WaitForSeconds(pAbilities.swapToolDelayTime);
        pAbilities.swapToolDelayInProgress = false;
    }

    void AddWaterResistanceForce()
    {
        rb.velocity -= new Vector2(rb.velocity.normalized.x * 0.05f, 0);
        if (rb.velocity.y < -pMovement.airSpeed * 1.75f)
            rb.velocity -= new Vector2(0, rb.velocity.normalized.y * 0.05f);
    }

    void UpdateSprite()
    {
        if (!pMovement.canMove || pAbilities.firingHarpoon || Mathf.Abs(mouseWorldPos.x - transform.position.x) <= 0.5f)
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
            anim.Play(clipName, 0, 0);
    }
}