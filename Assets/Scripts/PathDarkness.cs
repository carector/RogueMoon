using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathDarkness : MonoBehaviour
{
    public bool invertFade;

    private void Start()
    {
        GetComponent<Renderer>().sharedMaterial = new Material(GetComponent<Renderer>().sharedMaterial);
        Material mat = GetComponent<Renderer>().sharedMaterial;
        if (!invertFade)
            mat.color = Color.black;
        else
            mat.color = Color.clear;
    }

    public void Fade()
    {
        StartCoroutine(FadeCoroutine());
    }

    IEnumerator FadeCoroutine()
    {
        Material mat = GetComponent<Renderer>().sharedMaterial;

        if (!invertFade)
        {
            while (mat.color.a > 0)
            {
                Color col = mat.color;
                col -= new Color(0, 0, 0, 0.02f);
                mat.color = col;
                yield return new WaitForFixedUpdate();
            }
            Destroy(this.gameObject);
        }
        else
        {
            while (mat.color.a < 1)
            {
                Color col = mat.color;
                col += new Color(0, 0, 0, 0.02f);
                mat.color = col;
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
