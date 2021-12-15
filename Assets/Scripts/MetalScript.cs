using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetalScript : MonoBehaviour
{
    public int metalAmount;
    public GameObject metalGib;
    public GameObject twinkleGib;
    public float xRange;
    public float yRange;
    private void Start()
    {
        StartCoroutine(Twinkle());
    }

    IEnumerator Twinkle()
    {
        Instantiate(twinkleGib, transform.position + new Vector3(Random.Range(-1, 1f)*xRange, Random.Range(-1, 1f)*yRange), Quaternion.identity);
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
        StartCoroutine(Twinkle());
    }

    public void BreakApart()
    {
        SpawnGibsAtPoint(transform, metalAmount);
    }

    public void SpawnGibsAtPoint(Transform point, int numGibs)
    {
        for (int i = 0; i < numGibs; i++)
        {
            Instantiate(metalGib, point.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f)), Quaternion.identity);
        }
        Destroy(point.gameObject);
    }
}
