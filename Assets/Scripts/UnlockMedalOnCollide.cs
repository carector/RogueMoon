using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnlockMedalOnCollide : MonoBehaviour
{
    public int medalId;

    GameManager gm;
    // Start is called before the first frame update
    void Start()
    {
        gm = FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player" && enabled)
        {
            gm.UnlockMedal(medalId);
            enabled = false;
        }
    }
}
