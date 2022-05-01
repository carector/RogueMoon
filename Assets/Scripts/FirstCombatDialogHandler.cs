using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstCombatDialogHandler : MonoBehaviour
{
    GameManager gm;
    public PufferfishScript pf;

    bool printedDialog;
    bool fishDead;

    // Start is called before the first frame update
    void Start()
    {
        gm = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!printedDialog && pf != null && pf.movementSettings.noticedPlayer)
        {
            StartCoroutine(gm.DisplayDialog(gm.dialogSettings.JSONSource, "pre_combat"));
            printedDialog = true;
        }
        if (pf == null && !fishDead)
        {
            StartCoroutine(PostFishCoroutine());
            fishDead = true;
        }
    }

    IEnumerator PostFishCoroutine()
    {
        yield return new WaitForSeconds(1.5f);
        yield return gm.DisplayDialog(gm.dialogSettings.JSONSource, "post_combat");
        Destroy(this.gameObject);
    }
}