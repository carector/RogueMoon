using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunfishGib : GibScript
{
    SpriteRenderer[] tips = new SpriteRenderer[3];

    public void InitializeGib(bool flipX)
    {
        spr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        for (int i = 0; i < tips.Length; i++)
        {
            tips[i] = transform.GetChild(i).GetComponent<SpriteRenderer>();
            if (flipX)
                tips[i].flipX = true;
        }
        if(flipX)
            spr.flipX = true;

        rb.angularVelocity = Random.Range(-25, 25);
        
        StartCoroutine(FadeOutAfterTime2());
    }

    IEnumerator FadeOutAfterTime2()
    {
        yield return new WaitForSeconds(3);
        ParticleSystem[] ps = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem p in ps)
            p.Stop();
        yield return new WaitForSeconds(5);
        while (spr.color.a > 0)
        {
            spr.color -= new Color(0, 0, 0, 0.01f);
            for(int i = 0; i < 3; i++)
                tips[i].color -= new Color(0, 0, 0, 0.01f);
            yield return new WaitForFixedUpdate();
        }

        Destroy(this.gameObject);
    }
}
