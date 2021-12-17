using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleSpawner : MonoBehaviour
{
    public GameObject bubble;
    public float xIntensity;
    public BubbleScript.BubbleSize size;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DelayBetweenBubbles());
    }

    IEnumerator DelayBetweenBubbles()
    {
        while(true)
        {
            yield return new WaitForSeconds(Random.Range(0.25f, 3));
            Instantiate(bubble, transform.position, transform.rotation).GetComponent<BubbleScript>().Initialize(xIntensity, size);
        }
    }
}
