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
            prb.velocity = new Vector2(Mathf.MoveTowards(prb.velocity.x, 0, 0.1f), Mathf.Lerp(prb.velocity.y, -2.25f, 0.1f));
            ply.transform.position = Vector2.MoveTowards(prb.transform.position, new Vector2(transform.position.x, prb.transform.position.y), 0.025f);
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
        float offset = -15;
        while (ply.transform.position.y > -615.25f + offset)
            yield return null;

        gm.StartCoroutine(gm.TransitionMusic(5, 2));
        while (ply.transform.position.y > -630.25f + offset)
            yield return null;

        yield return gm.DisplayDialog(gm.dialogSettings.JSONSource, "endcutscene_1");

        while (ply.transform.position.y > -635f + offset)
            yield return null;
        gm.StartCoroutine(gm.TransitionMusic(-1, 1));
        hand.Play("HandAppear");
        yield return new WaitForSeconds(2);
        gm.PlaySFX(gm.sfx.generalSounds[1]);
        yield return new WaitForSeconds(3.5f);
        yield return gm.DisplayDialogAutoAdvance(gm.dialogSettings.JSONSource, "endcutscene_2");
        gm.StopSFX();
        crushed = true;
        hand.Play("HandCrush");
        ply.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        gm.ScreenShake(20);
        ply.GetCrushed();
        yield return new WaitForSeconds(6);
        Application.LoadLevel(1);
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
