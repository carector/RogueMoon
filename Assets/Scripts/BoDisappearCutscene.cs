using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BoDisappearCutscene : MonoBehaviour
{
    public Sprite reference;
    public Sprite reference2;
    Image portrait;
    TextMeshProUGUI dialogText;
    Animator phantasiaAnim;
    GameManager gm;
    public AudioClip ambience;

    // Start is called before the first frame update
    void Start()
    {
        gm = FindObjectOfType<GameManager>();
        portrait = GameObject.Find("LeftPortrait").GetComponent<Image>();
        dialogText = GameObject.Find("DialogText").GetComponent<TextMeshProUGUI>();
        phantasiaAnim = GameObject.Find("FantasiaCutsceneAnimator").GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            gm.StartCoroutine(gm.DisplayDialog(gm.dialogSettings.JSONSource, "level3_bo_gone"));
            StartCoroutine(WaitAndPlayAnimation());
        }
    }

    IEnumerator WaitAndPlayAnimation()
    {
        while (portrait.sprite != reference)
            yield return null;

        print("Here");
        phantasiaAnim.Play("FantasiaSilhouetteAttack");

        while (portrait.sprite != reference2)
            yield return null;

        gm.PlayAmbience(0);
        this.enabled = false;
    }
}
