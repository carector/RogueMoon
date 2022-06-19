using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateBigFishPath : MonoBehaviour
{
    public Transform newPath;

    BossFishScript fish;

    // Start is called before the first frame update
    void Start()
    {
        fish = FindObjectOfType<BossFishScript>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Boss")
        {
            fish.UpdateNodePath(newPath);
            Destroy(this.gameObject);
        }
    }
}
