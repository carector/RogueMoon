using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroCutsceneManager : MonoBehaviour
{
    PlayerController ply;
    GameManager gm;
    bool showedMetalDialog;

    // Start is called before the first frame update
    void Start()
    {
        ply = FindObjectOfType<PlayerController>();
        gm = FindObjectOfType<GameManager>();
        StartCoroutine(Intro());
    }

    // Update is called once per frame
    void Update()
    {
        if(ply.pResources.metal > 0 && !showedMetalDialog)
        {
            showedMetalDialog = true;
            StartCoroutine(MetalPickupDialog());
        }
    }

    IEnumerator MetalPickupDialog()
    {
        yield return new WaitForSeconds(1);
        StartCoroutine(gm.DisplayDialog(gm.dialogSettings.JSONSource, "tutorial_6"));
    }

    IEnumerator Intro()
    {
        while (!ply.pMovement.isGrounded)
            yield return null;

        yield return new WaitForSecondsRealtime(0.75f);
        yield return gm.DisplayDialog(gm.dialogSettings.JSONSource, "tutorial_1");
        ply.pMovement.canMove = true;
    }
}
