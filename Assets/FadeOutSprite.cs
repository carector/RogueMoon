using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeOutSprite : MonoBehaviour
{
    public float initialDelay;
    SpriteRenderer spr;

    // Start is called before the first frame update
    void Start()
    {
        spr = GetComponent<SpriteRenderer>();
        StartCoroutine(FadeOutAfterTime());
    }

    IEnumerator FadeOutAfterTime()
    {
        yield return new WaitForSeconds(initialDelay);
        while (spr.color.a > 0)
        {
            spr.color -= new Color(0, 0, 0, 0.01f);
            yield return new WaitForFixedUpdate();
        }

        Destroy(this.gameObject);
    }
}
