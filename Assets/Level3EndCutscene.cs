using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level3EndCutscene : MonoBehaviour
{
    public Animator suctionFX;
    public GameObject harmlessExplosion;

    SpriteRenderer spr;
    PlayerController ply;
    GameManager gm;
    Transform pullTowardsPoint;

    // Start is called before the first frame update
    void Start()
    {
        spr = GetComponent<SpriteRenderer>();
        ply = FindObjectOfType<PlayerController>();
        gm = FindObjectOfType<GameManager>();
        pullTowardsPoint = transform.GetChild(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.tag == "Harpoonable" && other.GetComponent<ExplosiveObject>() != null)
        {
            Instantiate(harmlessExplosion, other.transform.position, Quaternion.identity);
            Destroy(other.gameObject);
            
            StartCoroutine(CutsceneSequence());
        }
    }

    IEnumerator CutsceneSequence()
    {
        gm.ScreenShake(15);
        ply.pMovement.canMove = false;
        Rigidbody2D prb = ply.GetComponent<Rigidbody2D>();
        prb.isKinematic = true;
        prb.velocity = Vector2.zero;
        BoxCollider2D[] cols = GetComponents<BoxCollider2D>();
        foreach (Collider2D col in cols)
            col.enabled = false;

        spr.enabled = false;
        suctionFX.Play("SuctionActive");
        yield return new WaitForSeconds(2);
        yield return gm.DisplayDialog(gm.dialogSettings.JSONSource, "level2_suckedin");

        while(Mathf.Abs(ply.transform.position.x - pullTowardsPoint.position.x) > 1)
        {
            ply.transform.position = Vector2.MoveTowards(ply.transform.position, pullTowardsPoint.position, 0.075f);
            gm.ScreenShake(10);
            yield return new WaitForFixedUpdate();
        }

        while(Mathf.Abs(ply.transform.position.y - pullTowardsPoint.position.y) < 5)
        {
            ply.transform.position = Vector2.MoveTowards(ply.transform.position, pullTowardsPoint.position + Vector3.down*10, 0.075f);
            gm.ScreenShake(10);
            yield return new WaitForFixedUpdate();
        }

        gm.CutToBlackScreen();

        yield return new WaitForSeconds(5);

    }
}
