using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BigFishChargeTrigger : MonoBehaviour
{
    bool playerInBounds;

    BossFishScript fish;

    // Start is called before the first frame update
    void Start()
    {
        fish = FindObjectOfType<BossFishScript>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Player")
            playerInBounds = true;

        if (collision.tag == "Boss" && playerInBounds && !fish.charging)
            fish.StartChargeAttack();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
            playerInBounds = false;
    }
}
