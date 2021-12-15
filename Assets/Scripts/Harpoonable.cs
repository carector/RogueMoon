using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Harpoonable : MonoBehaviour
{
    public float enableRadius = 4;
    public PathDarkness[] darknessBlockers;
    public GameObject gib;


    Rigidbody2D[] rocksToEnable;

    // Start is called before the first frame update
    void Start()
    {

    }

    public void GetPulledByHarpoon()
    {
        BreakNearbyRocks();
        FadeDarkness();
    }

    public void BreakApart()
    {
        // Get nearby rocks, break them apart if possible
        BreakNearbyRocks();
        FadeDarkness();
        SpawnGibsAtPoint(transform, 3);
    }

    void EnableNearbyRocks()
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, enableRadius);
        foreach (Collider2D c in cols)
        {
            if (c.gameObject != gameObject && (c.tag == "Gib" || c.tag == "Harpoonable"))
            {
                c.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            }
        }
    }

    void BreakNearbyRocks()
    {
        FindObjectOfType<GameManager>().ScreenShake(5);
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (Collider2D c in cols)
        {
            if (c.gameObject != gameObject && c.tag == "Harpoonable")
            {
                SpawnGibsAtPoint(c.transform, 3);
            }
        }
    }

    public void SpawnGibsAtPoint(Transform point, int numGibs)
    {
        for (int i = 0; i < numGibs; i++)
        {
            Instantiate(gib, point.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f)), Quaternion.identity);
        }
        Destroy(point.gameObject);
    }

    void FadeDarkness()
    {
        if (darknessBlockers[0] == null)
            return;
        foreach (PathDarkness p in darknessBlockers)
            p.FadeOut();
    }
}
