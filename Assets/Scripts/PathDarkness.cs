using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathDarkness : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Renderer>().sharedMaterial = new Material(GetComponent<Renderer>().sharedMaterial);
        Material mat = GetComponent<Renderer>().sharedMaterial;
        mat.color = Color.black;
    }

    public void FadeOut()
    {
        StartCoroutine(FadeOutCoroutine());
    }

    IEnumerator FadeOutCoroutine()
    {
        Material mat = GetComponent<Renderer>().sharedMaterial;
        while(mat.color.a > 0)
        {
            Color col = mat.color;
            col -= new Color(0, 0, 0, 0.02f);
            mat.color = col;
            yield return new WaitForFixedUpdate();
        }
        Destroy(this.gameObject);
    }
}
