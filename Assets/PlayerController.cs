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
        public int health;
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
        public bool aiming;
        public float harpoonRange = 10;
        public bool firingHarpoon;
        public bool beingPulledTowardsHarpoon;
        public float impactDelayTime = 0.35f;
    }

    public PlayerMovementSettings pMovement;
    public PlayerResources pResources;
    public PlayerAbilitySettings pAbilities;

    SpriteRenderer harpoonEndpoint;
    SpriteRenderer harpoonChain;
    Rigidbody2D rb;
    SpriteRenderer bodySpr;
    SpriteRenderer armsSpr;
    Animator bodyAnim;
    Animator armsAnim;
    Transform harpoonStartPoint;
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
        harpoonStartPoint = GameObject.Find("HarpoonStartPoint").transform;
        harpoonChain = GameObject.Find("HarpoonChain").GetComponent<SpriteRenderer>();
        harpoonEndpoint.color = Color.clear;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        bool lastGroundedState = pMovement.isGrounded;
        pMovement.isGrounded = CheckForGround();

        // Play impact animation if we just hit the ground
        if (lastGroundedState != pMovement.isGrounded)
        {
            if (pMovement.isGrounded)
            {
                pMovement.canMove = false;
                StartCoroutine(ImpactDelayCoroutine());
            }
            else if (rb.velocity.y <= 1f)
            {
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
        if (pAbilities.beingPulledTowardsHarpoon || !pMovement.canMove)
            return;

        bodyAnim.SetFloat("WalkSpeed", rb.velocity.magnitude / 3);
        armsAnim.SetFloat("WalkSpeed", rb.velocity.magnitude / 3);

        float horiz = Input.GetAxis("Horizontal");
        float vert = Input.GetAxis("Vertical");
        Vector2 vel = Vector2.zero;

        // Can move up and down only if not grounded
        if (pMovement.isGrounded)
        {
            vel = new Vector2(horiz, 0) * pMovement.acceleration * rb.mass;
            rb.gravityScale = gravityScale;
        }
        else
        {
            rb.gravityScale = 0.15f;
            vel = new Vector2(horiz, vert) * pMovement.acceleration * rb.mass;
            vel += vel = Vector2.down * pMovement.acceleration * rb.mass / 4;
        }

        if ((pMovement.isGrounded && rb.velocity.magnitude < pMovement.groundSpeed) || (!pMovement.isGrounded && rb.velocity.magnitude < pMovement.airSpeed))
            rb.AddForce(vel);
    }

    void RotateArm()
    {
        if (Input.GetMouseButton(1) && !pAbilities.firingHarpoon)
        {
            pAbilities.aiming = true;
            if (!pAbilities.firingHarpoon)
                CheckAndPlayClip(armsAnim, "Arm_Ready");
            Vector2 mouseDir = (Vector3)mouseWorldPos - transform.position;
            float angle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
            armsSpr.transform.rotation = Quaternion.Lerp(armsSpr.transform.rotation, Quaternion.Euler(new Vector3(0, 0, angle)), 0.15f);
            hasRetractedArm = false;
        }
        else
        {
            pAbilities.aiming = false;
            if (pAbilities.firingHarpoon)
                return;

            if (!hasRetractedArm)
                CheckAndPlayClip(armsAnim, "Arm_Unready");
            armsSpr.transform.rotation = Quaternion.Lerp(armsSpr.transform.rotation, Quaternion.identity, 0.15f);
            hasRetractedArm = true;
        }
    }

    void CheckButtonInputs()
    {
        if (Input.GetMouseButtonDown(0) && !pAbilities.firingHarpoon && pAbilities.aiming)
        {
            pAbilities.firingHarpoon = true;
            StartCoroutine(FireHarpoon());
        }

        if (Input.GetKeyDown(KeyCode.W) && pMovement.isGrounded)
        {
            CheckAndPlayClip(bodyAnim, "Mech_Jump");
            rb.velocity = new Vector2(rb.velocity.x, 4);
        }
    }

    IEnumerator FireHarpoon()
    {
        harpoonEndpoint.color = Color.white;
        CheckAndPlayClip(armsAnim, "Arm_FireHarpoon");

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
        Transform hitObject;

        Vector2 previousPointPos = harpoonStartPoint.position;
        while (pAbilities.firingHarpoon && Vector2.Distance(harpoonEndpoint.transform.position, pos) > 1 && !pAbilities.beingPulledTowardsHarpoon)
        {
            harpoonEndpoint.transform.position = Vector2.MoveTowards(harpoonEndpoint.transform.position, pos, 0.5f);
            harpoonChain.size = new Vector2(Vector2.Distance(harpoonEndpoint.transform.position, harpoonStartPoint.position), 0.375f);
            harpoonChain.transform.position = (harpoonEndpoint.transform.position + harpoonStartPoint.position) / 2;
            harpoonChain.transform.right = (harpoonEndpoint.transform.position - harpoonStartPoint.position).normalized;

            Collider2D[] cols = Physics2D.OverlapCircleAll(harpoonEndpoint.transform.position, 0.5f);
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
                }
            }
            if (hitGroundPoint != Vector3.zero)
                break;

            yield return new WaitForFixedUpdate();
        }

        if (hitGroundPoint != Vector3.zero)
        {
            if (pAbilities.beingPulledTowardsHarpoon)
            {
                float timePassed = 0;
                while (timePassed < 1.75f && Vector2.Distance(transform.position, hitGroundPoint) > 3)
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
            else
            {

            }
        }

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
        Debug.DrawRay(transform.position + Vector3.down * 2, Vector2.down, Color.red, Time.fixedDeltaTime);
        if (Physics2D.Raycast(transform.position + Vector3.down * 2, Vector2.down, 0.25f, mask))
            grounded = true;

        return grounded;
    }

    IEnumerator ImpactDelayCoroutine()
    {
        CheckAndPlayClip(bodyAnim, "Mech_Impact");
        yield return new WaitForSeconds(0.25f);
        pMovement.canMove = true;
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
            if (pAbilities.aiming)
            {
                armsSpr.flipY = true;
                armsSpr.flipX = false;
            }
            else
            {
                armsSpr.flipY = false;
                armsSpr.flipX = true;
            }
        }
        else
        {
            bodySpr.flipX = false;
            armsSpr.flipY = false;
            armsSpr.flipX = false;
        }
    }

    public void CheckAndPlayClip(Animator anim, string clipName)
    {
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(clipName))
            anim.Play(clipName);
    }
}