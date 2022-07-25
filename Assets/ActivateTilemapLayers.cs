using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateTilemapLayers : MonoBehaviour
{
    public GameObject[] layersToActivate;
    public GameObject[] layersToDeactivate;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            for (int i = 0; i < layersToActivate.Length; i++)
                layersToActivate[i].SetActive(true);
            for (int i = 0; i < layersToDeactivate.Length; i++)
                layersToDeactivate[i].SetActive(false);
        }
        Destroy(this.gameObject);
    }
}
