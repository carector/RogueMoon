using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiDialogTrigger : MonoBehaviour
{
    public string[] conversationIds;
    GameManager gm;

    private void Start()
    {
        gm = FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            StartCoroutine(PlayDialog());
        }
    }

    IEnumerator PlayDialog()
    {
        for (int i = 0; i < conversationIds.Length; i++)
            yield return gm.DisplayDialog(gm.dialogSettings.JSONSource, conversationIds[i]);

        Destroy(this.gameObject);
    }
}