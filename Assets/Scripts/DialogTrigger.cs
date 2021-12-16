using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogTrigger : MonoBehaviour
{
    public string conversationId;
    GameManager gm;

    private void Start()
    {
        gm = FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            gm.StartCoroutine(gm.DisplayDialog(gm.dialogSettings.JSONSource, conversationId));
            Destroy(this.gameObject);
        }
    }
}
