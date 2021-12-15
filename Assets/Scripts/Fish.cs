using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fish : MonoBehaviour
{
    [System.Serializable]
    public class FishMovementSettings
    {
        public float acceleration = 10;
        public float swimSpeed = 3;
        public bool inSwimBounds = true;
    }

    public FishMovementSettings movementSettings;
    public Collider2D swimAreaBounds;
    public GameObject gib;
    public Sprite[] goreBits;
    public string animationPrefix = "Fish";
    public Vector2 direction;

    Rigidbody2D rb;
    Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        GetReferences();
        StartCoroutine(MovementCycle());
    }

    // Called by any subclasses
    public void GetReferences()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void BreakApart()
    {
        foreach (Sprite s in goreBits)
            Instantiate(gib, transform.position, Quaternion.identity).GetComponent<GibScript>().InitializeGib(s, Random.Range(-50, 50));

        Destroy(this.gameObject);
    }

    // Can be overriden by any subclasses
    public IEnumerator MovementCycle()
    {
        // Default movement example
        direction = (Vector3.right * Random.Range(-1, 1f) + (Vector3.up*Random.Range(-1, 1))).normalized;
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
            if (!movementSettings.inSwimBounds)
            {
                rb.velocity = Vector2.zero;

                anim.SetFloat("FlipSpeed", Mathf.Sign(direction.x));
                if (direction.x > 0)
                    CheckAndPlayClip(animationPrefix + "_Flip");
                else
                    CheckAndPlayClip(animationPrefix + "_FlipReverse");

                yield return new WaitForSeconds(anim.GetCurrentAnimatorClipInfo(0)[0].clip.length);

                // Flip direction and add randomness
                direction = (swimAreaBounds.bounds.center - transform.position);
                direction += new Vector2(Random.Range(-1, 1), Random.Range(-1, 1)*3);
                direction.Normalize();

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

    public void CheckAndPlayClip(string clipName)
    {
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(clipName))
            anim.Play(clipName, 0, 0);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision == swimAreaBounds)
            movementSettings.inSwimBounds = false;
    }
}
