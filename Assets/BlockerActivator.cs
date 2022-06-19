using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockerActivator : MonoBehaviour
{
    public BlockerScript[] blockers;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            for (int i = 0; i < blockers.Length; i++)
                blockers[i].ActivateSelf();

            Destroy(this.gameObject);
        }
    }
}
