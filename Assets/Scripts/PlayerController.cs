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
        public float midairGravityScale = 0.35f;
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
        public float damageImmuneTime = 0.75f;
        public bool jumpDelayInProgress = false;
        public bool impactDelayInProgress = false;
        public bool attackDelayInProgress = false;
        public bool swapToolDelayInProgress = false;
        public bool damageDelayInProgress = false;
        public GameObject explosion;
        public GameObject bubble;
    }

    public PlayerMovementSettings pMovement;
    public PlayerResources pResources;
    public PlayerAbilitySettings pAbilities;

    GameManager gm;
    [HideInInspector]
    public SpriteRenderer harpoonEndpoint;
    SpriteRenderer harpoonChain;
    Rigidbody2D rb;
    SpriteRenderer bodySpr;
    SpriteRenderer armsSpr;
    Animator bodyAnim;
    Animator armsAnim;
    Transform harpoonStartPoint;
    Transform damageStartPoint;
    [HideInInspector]
    public AudioSource harpoonLoopingAudio;
    [HideInInspector]
    public AudioSource attackLoopingAudio;
    [HideInInspector]
    public AudioSource thrustLoopingAudio;
    ParticleSystem[] steamParticles;
    Vector2 mouseWorldPos;

    public LineRenderer lineTEMP;

    float gravityScale;
    bool hasRetractedArm;
    bool lastAttackState;
    bool invisible;
    [HideInInspector]
    public float storedHoverTime;
    int mask = ~((1 << 8) | (1 << 10) | (1 << 9) | (1 << 12)); // Ignore ground + ceiling + objects raycast layermask
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
        thrustLoopingAudio = armsSpr.transform.GetChild(6).GetComponent<AudioSource>();
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
        if (pResources.health > 0 || invisible)
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
    }

    private void Update()
    {
        if (pResources.health > 0)
        {
            mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            lineTEMP.SetPositions(new Vector3[2] { transform.position, transform.position });

            if (Time.timeScale == 0)
                return;

            RotateArm();
            CheckButtonInputs();
        }
    }

    void MovePlayer()
    {
        if (pAbilities.beingPulledTowardsHarpoon || pAbilities.impactDelayInProgress || invisible)
        {
            thrustLoopingAudio.volume = Mathf.Lerp(thrustLoopingAudio.volume, 0, 0.75f);
            return;
        }

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
            // Thrust
            if (vert > 0 && storedHoverTime > 0)
            {
                if (!thrustLoopingAudio.isPlaying || thrustLoopingAudio.volume != 1)
                {
                    thrustLoopingAudio.Play();
                    thrustLoopingAudio.volume = 1;
                }
                CheckAndPlayClip(bodyAnim, "Mech_Midair");
                armsAnim.SetFloat("WalkSpeed", 1);

                storedHoverTime -= Time.fixedDeltaTime;
                if (storedHoverTime <= 0)
                {
                    gm.PlaySFX(gm.sfx.playerSounds[2]);
                    CheckAndPlayClip(bodyAnim, "Mech_NoThrust");
                    armsAnim.SetFloat("WalkSpeed", 0);
                }

                if (rb.velocity.y < 0)
                    rb.velocity = Vector2.Lerp(rb.velocity, new Vector2(rb.velocity.x, 1), 0.05f);
            }
            else
            {
                FadeOutThrustAudio();
                armsAnim.SetFloat("WalkSpeed", 0);

                if (storedHoverTime <= 0)
                    vert = Mathf.Clamp(vert, -1, 0);
                else if (vert <= 0)
                    CheckAndPlayClip(bodyAnim, "Mech_NoThrust");
            }

            // Minor gravity + additional downwards force if we're airborne
            rb.gravityScale = pMovement.midairGravityScale;
            vel = new Vector2(horiz * 0.75f, Mathf.Clamp(vert, -0.25f, 0.75f)) * pMovement.acceleration * rb.mass;
            vel.x /= 1.33f;
            //vel += vel = Vector2.down * pMovement.acceleration * rb.mass / 4;
        }

        if (pMovement.isGrounded && rb.velocity.magnitude < pMovement.groundSpeed)
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

    public void FadeOutThrustAudio()
    {
        if (thrustLoopingAudio.isPlaying)
        {
            thrustLoopingAudio.volume = Mathf.Lerp(thrustLoopingAudio.volume, 0, 0.75f);
            if (thrustLoopingAudio.volume <= 0.01f)
                thrustLoopingAudio.Stop();
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
            armsSpr.transform.rotation = Quaternion.Lerp(armsSpr.transform.rotation, Quaternion.Euler(new Vector3(0, 0, angle)), 0.75f);
            hasRetractedArm = false;
        }
        else if (!pAbilities.attacking)
        {
            pAbilities.aiming = false;
            if (!hasRetractedArm)
                CheckAndPlayClip(armsAnim, "Arm_Unready");

            // Same as above - If we were aiming in the previous frame, fix rotation and scale to prevent weird rotation visual
            if ((lastAimState || lastAttackState) && armsSpr.transform.localScale.y == -1)
            {
                armsSpr.transform.rotation = Quaternion.Euler(0, 0, armsSpr.transform.rotation.eulerAngles.z + 180);
                armsSpr.transform.localScale = new Vector2(-1, 1);
            }
            armsSpr.transform.rotation = Quaternion.Lerp(armsSpr.transform.rotation, Quaternion.identity, 0.15f);
            hasRetractedArm = true;
        }

        lastAttackState = pAbilities.attacking;
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
        Vector2 pos = harpoonStartPoint.position + (dir * pAbilities.harpoonRange);

        harpoonEndpoint.transform.position = harpoonStartPoint.position;
        Vector2 mouseDir = (Vector3)mouseWorldPos - transform.position;
        float angle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
        harpoonEndpoint.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        Transform hitGroundTransform = null;
        Vector3 hitGroundPointOffset = Vector3.zero;
        Transform hitObject = null;
        Rigidbody2D hitRb = null;
        bool hitFish = false;

        Vector2 previousPointPos = harpoonStartPoint.position;
        while (pAbilities.firingHarpoon && Vector2.Distance(harpoonEndpoint.transform.position, transform.position) < pAbilities.harpoonRange && !pAbilities.beingPulledTowardsHarpoon)
        {
            harpoonEndpoint.transform.position = Vector2.MoveTowards(harpoonEndpoint.transform.position, pos, 1f);
            harpoonChain.size = new Vector2(Vector2.Distance(harpoonEndpoint.transform.position, harpoonStartPoint.position), 0.375f);
            harpoonChain.transform.position = (harpoonEndpoint.transform.position + harpoonStartPoint.position) / 2;
            harpoonChain.transform.right = (harpoonEndpoint.transform.position - harpoonStartPoint.position).normalized;

            Collider2D[] cols = Physics2D.OverlapBoxAll(harpoonEndpoint.transform.position - harpoonEndpoint.transform.right * 0.25f, new Vector2(1.5f, 0.25f), harpoonEndpoint.transform.eulerAngles.z);
            if (cols.Length > 0)
            {
                foreach (Collider2D col in cols)
                {
                    if (col.tag.Equals("Ground") || col.tag.Equals("Breakable"))
                    {
                        hitGroundTransform = col.transform;
                        hitGroundPointOffset = col.ClosestPoint(harpoonEndpoint.transform.position) - (Vector2)col.transform.position;
                        pAbilities.beingPulledTowardsHarpoon = true;
                        break;
                    }
                    else if (col.tag.Equals("Harpoonable"))
                    {
                        gm.PlaySFX(gm.sfx.playerSounds[0]);
                        hitObject = col.transform;
                        hitObject.SendMessage("GetPulledByHarpoon", SendMessageOptions.DontRequireReceiver);
                        hitObject.transform.parent = harpoonEndpoint.transform;
                        break;
                    }
                    else if (col.tag.Equals("Fish"))
                    {
                        col.GetComponent<Fish>().TakeDamage(1);
                        if (col != null)
                        {
                            hitGroundTransform = col.transform;
                            hitGroundPointOffset = col.ClosestPoint(harpoonEndpoint.transform.position) - (Vector2)col.transform.position;
                            pAbilities.beingPulledTowardsHarpoon = true;
                            break;
                        }
                    }
                    else if (col.tag.Equals("Bubble"))
                    {
                        col.SendMessage("BreakApart");
                        break;
                    }
                }
            }
            if (hitGroundTransform != null || hitObject != null || hitFish)
                break;

            yield return new WaitForFixedUpdate();
        }
        harpoonLoopingAudio.loop = true;

        // Pull towards ground point
        if (hitGroundTransform != null)
        {
            harpoonStartingGroundedState = CheckForGround();
            harpoonLoopingAudio.Play();
            pMovement.isGrounded = false;
            CheckAndPlayClip(bodyAnim, "Mech_NoThrust");

            if (pAbilities.beingPulledTowardsHarpoon)
            {
                float timePassed = 0;
                while (timePassed < 1.75f && hitGroundTransform != null && Vector2.Distance(transform.position, hitGroundTransform.position + hitGroundPointOffset) > 3 && Input.GetMouseButton(0) && pAbilities.aiming && !pMovement.isGrounded)
                {
                    harpoonLoopingAudio.pitch = 0.95f + Mathf.Clamp(rb.velocity.magnitude / pMovement.pulledSpeed * 0.2f, 0, 0.2f);
                    harpoonChain.size = new Vector2(Vector2.Distance(harpoonEndpoint.transform.position, harpoonStartPoint.position), 0.375f);
                    harpoonChain.transform.position = (harpoonEndpoint.transform.position + transform.position) / 2;
                    harpoonChain.transform.right = (harpoonEndpoint.transform.position - harpoonStartPoint.position).normalized;
                    harpoonEndpoint.transform.position = (hitGroundTransform.position + hitGroundPointOffset);

                    if (harpoonStartingGroundedState)
                        harpoonStartingGroundedState = CheckForGround();

                    Vector3 force = (hitGroundTransform.position + hitGroundPointOffset - transform.position) / 3;
                    rb.AddForce(force * rb.mass * pMovement.pulledSpeed);
                    if (rb.velocity.magnitude > pMovement.pulledSpeed)
                        rb.velocity = Vector3.ClampMagnitude(rb.velocity, pMovement.pulledSpeed);
                    timePassed += Time.fixedDeltaTime;

                    yield return new WaitForFixedUpdate();
                }
            }
        }
        // Pull object towards player
        else if (hitObject != null)
        {
            harpoonLoopingAudio.Play();
            //rb.velocity = new Vector2(0, -2);

            hitRb = hitObject.GetComponent<Rigidbody2D>();
            hitRb.bodyType = RigidbodyType2D.Kinematic;
            hitRb.velocity = Vector2.zero;
            hitRb.angularVelocity = 0;

            pAbilities.beingPulledTowardsHarpoon = false;
            pAbilities.firingHarpoon = false;

            //float angleBetweenHarpoon = Vector2.Angle(harpoonEndpoint.transform.forward, hitObject.transform.forward);
            while (Input.GetMouseButton(0) && pAbilities.aiming && (hitRb.transform.parent == harpoonStartPoint.transform || hitRb.transform.parent == harpoonEndpoint.transform))
            {
                
                if(Vector2.Distance(harpoonEndpoint.transform.position, harpoonStartPoint.position) < 0.1f && harpoonLoopingAudio.isPlaying)
                {
                    harpoonEndpoint.transform.position = harpoonStartPoint.position;
                    hitRb.transform.parent = harpoonStartPoint.transform;
                    hitRb.transform.localPosition = Vector2.zero + Vector2.right*0.25f;
                    harpoonLoopingAudio.loop = false;
                    harpoonLoopingAudio.Stop();
                    harpoonEndpoint.color = Color.clear;
                    harpoonChain.size = new Vector2(0, 0.375f);
                }
                else
                {
                    harpoonLoopingAudio.pitch = 1.25f - Mathf.Clamp(Vector2.Distance(transform.position, harpoonEndpoint.transform.position) / 40, 0, 0.35f);
                    harpoonEndpoint.transform.position = Vector2.MoveTowards(harpoonEndpoint.transform.position, harpoonStartPoint.position, 0.5f);
                    harpoonChain.size = new Vector2(Vector2.Distance(harpoonEndpoint.transform.position, harpoonStartPoint.position), 0.375f);
                    harpoonChain.transform.position = (harpoonEndpoint.transform.position + transform.position) / 2;
                    harpoonChain.transform.right = (harpoonEndpoint.transform.position - harpoonStartPoint.position).normalized;
                }

                if (hitRb.transform.parent == harpoonStartPoint.transform)
                    hitRb.transform.localPosition = Vector2.Lerp(hitRb.transform.localPosition, Vector2.zero + Vector2.right * 0.25f, 0.25f);

                yield return new WaitForFixedUpdate();
            }

            if ((hitRb.transform.parent == harpoonStartPoint.transform || hitRb.transform.parent == harpoonEndpoint.transform))
            {
                hitRb.transform.parent = null;
                hitRb.SendMessage("GetReleasedByHarpoon");
                hitRb.bodyType = RigidbodyType2D.Dynamic;
                hitRb.velocity = harpoonStartPoint.forward * 10;
            }
            pMovement.canMove = true;
        }

        harpoonStartingGroundedState = false;
        harpoonEndpoint.transform.parent = null;
        harpoonLoopingAudio.loop = false;
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
        Debug.DrawRay(transform.position + Vector3.down * 2, Vector2.down * 0.25f, Color.red, Time.fixedDeltaTime);
        RaycastHit2D hit = Physics2D.BoxCast(transform.position + Vector3.down * 2, new Vector2(0.25f, 0.25f), 0, Vector2.down, 0.1f, mask);
        if (hit.transform != null && (hit.transform.tag == "Ground" || hit.transform.tag == "Breakable"))
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
        if(!pAbilities.aiming)
            CheckAndPlayClip(armsAnim, "Arm_Walk");
        rb.velocity = new Vector2(0, -2);
        yield return new WaitForSeconds(pAbilities.impactDelayTime);
        pAbilities.impactDelayInProgress = false;
    }

    IEnumerator Attack()
    {
        pAbilities.attackCharges--;
        CheckAndPlayClip(armsAnim, "Arm_Attack");

        bool lastAimState = pAbilities.aiming;
        lastAttackState = true;
        float offset = 0;
        // If we weren't aiming in the previous frame, fix rotation and scale to prevent weird rotation visual
        if (armsSpr.transform.localScale.x == -1)
            offset = 180;

        Vector2 mouseDir = (Vector3)mouseWorldPos - transform.position;
        float angle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
        armsSpr.transform.rotation = Quaternion.Lerp(armsSpr.transform.rotation, Quaternion.Euler(new Vector3(0, 0, angle + offset)), 1f);

        Collider2D[] cols = Physics2D.OverlapCapsuleAll(damageStartPoint.position, new Vector2(3f, 0.75f), CapsuleDirection2D.Horizontal, armsAnim.transform.rotation.eulerAngles.z);
        foreach (Collider2D c in cols)
        {
            if (c.tag == "Fish")
            {
                c.GetComponent<Fish>().TakeDamage(2);
                break;
            }
            else if (c.tag == "Breakable" || c.tag == "Harpoonable" || c.tag == "Fish" || c.tag == "Bubble")
            {
                c.SendMessage("BreakApart", SendMessageOptions.DontRequireReceiver);
            }
        }

        for (int i = 0; i < 4; i++)
            Instantiate(pAbilities.bubble, damageStartPoint.position, Quaternion.identity).GetComponent<BubbleScript>().Initialize(3, BubbleScript.BubbleSize.small);

        yield return new WaitForSeconds(pAbilities.attackDelayTime);

        if(pMovement.isGrounded)
            CheckAndPlayClip(armsAnim, "Arm_Walk");
        else
            CheckAndPlayClip(armsAnim, "Arm_Idle");

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
        if (invisible || pAbilities.attacking || !pMovement.canMove || pAbilities.firingHarpoon || Mathf.Abs(mouseWorldPos.x - transform.position.x) <= 0.5f)
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

    public void GetCrushed()
    {
        invisible = true;
        GetComponent<Collider2D>().enabled = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.velocity = Vector2.zero;
        CheckAndPlayClip(armsAnim, "Arm_Invisible");
        CheckAndPlayClip(bodyAnim, "Mech_Invisible");

        for (int i = 0; i < 20; i++)
            Instantiate(pAbilities.bubble, transform.position, Quaternion.identity).GetComponent<BubbleScript>().Initialize(3, BubbleScript.BubbleSize.random);
        rb.velocity = Vector2.zero;
    }

    public void TakeDamage()
    {
        TakeDamage(1);
    }

    public void TakeDamage(int amount)
    {
        // Short immunity period after getting attacked
        if (pAbilities.damageDelayInProgress || pResources.health <= 0)
            return;

        for (int i = 0; i < 4; i++)
            Instantiate(pAbilities.bubble, damageStartPoint.position, Quaternion.identity).GetComponent<BubbleScript>().Initialize(4, BubbleScript.BubbleSize.random);
        pResources.health = Mathf.Clamp(pResources.health - amount, 0, 20);
        gm.ScreenShake(7);
        if (pResources.health > 0)
            StartCoroutine(DamageFlashCoroutine());

        StartCoroutine(DamageImmuneDelayCoroutine());
    }

    IEnumerator DamageFlashCoroutine()
    {
        gm.PlaySFX(gm.sfx.playerSounds[3]);
        for (int i = 0; i < 3; i++)
        {
            bodySpr.color = Color.red;
            armsSpr.color = Color.red;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            bodySpr.color = Color.white;
            armsSpr.color = Color.white;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        for (int i = 0; i < 2; i++)
        {
            bodySpr.color = Color.red;
            armsSpr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            bodySpr.color = Color.white;
            armsSpr.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator DamageImmuneDelayCoroutine()
    {
        pAbilities.damageDelayInProgress = true;
        yield return new WaitForSeconds(pAbilities.damageImmuneTime);
        pAbilities.damageDelayInProgress = false;
    }

    public void CheckAndPlayClip(Animator anim, string clipName)
    {
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(clipName))
            anim.Play(clipName, 0, 0);
    }

    public IEnumerator Die()
    {
        GetComponent<Collider2D>().enabled = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.velocity = Vector2.zero;
        float timer = 1.75f;
        while (timer > 0)
        {
            bodySpr.color = Color.red;
            armsSpr.color = Color.red;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            bodySpr.color = Color.white;
            armsSpr.color = Color.white;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            timer -= Time.fixedDeltaTime * 4;
        }

        gm.PlaySFX(gm.sfx.playerSounds[4]);
        Instantiate(pAbilities.explosion, transform.position, Quaternion.identity);

        for (int i = 0; i < 6; i++)
            Instantiate(pAbilities.bubble, transform.position, Quaternion.identity).GetComponent<BubbleScript>().Initialize(3, BubbleScript.BubbleSize.random);

        CheckAndPlayClip(armsAnim, "Arm_Invisible");
        CheckAndPlayClip(bodyAnim, "Mech_Invisible");
    }
}