using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndCutsceneScript : MonoBehaviour
{
    bool started;
    bool crushed;

    PlayerController ply;
    GameManager gm;
    Rigidbody2D prb;
    Animator hand;

    // Start is called before the first frame update
    void Start()
    {
        gm = FindObjectOfType<GameManager>();
        ply = FindObjectOfType<PlayerController>();
        prb = ply.GetComponent<Rigidbody2D>();
        hand = GameObject.Find("HandAnimator").GetComponent<Animator>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (started && !crushed)
        {
            prb.velocity = new Vector2(Mathf.MoveTowards(prb.velocity.x, 0, 0.025f), prb.velocity.y);
            ply.transform.position = Vector2.MoveTowards(prb.transform.position, new Vector2(transform.position.x, Mathf.Clamp(prb.transform.position.y, -8, 8)), 0.03f);
        }
    }

    private void Update()
    {
        if (started && !crushed)
        {
            hand.transform.position = ply.transform.position;
        }
    }

    IEnumerator EndSequence()
    {
        yield return new WaitForSeconds(10);
        hand.Play("HandAppear");
        yield return new WaitForSeconds(2);
        gm.PlaySFX(gm.sfx.generalSounds[1]);
        yield return new WaitForSeconds(4);
        gm.StopSFX();
        hand.Play("HandCrush");
        gm.ScreenShake(20);
        ply.GetCrushed();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player" && !started)
        {
            ply.pMovement.canMove = false;
            started = true;
            StartCoroutine(EndSequence());
        }
    }
}
