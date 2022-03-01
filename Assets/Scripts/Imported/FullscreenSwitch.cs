using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullscreenSwitch : MonoBehaviour
{
    public int windowedX = 640;
    public int windowedY = 480;

    AspectRatioController arc;

    // Start is called before the first frame update
    void Start()
    {
        arc = FindObjectOfType<AspectRatioController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            StartCoroutine(SetFullscreenMode(Screen.fullScreen));
        }
    }

    IEnumerator SetFullscreenMode(bool isFullscreen)
    {
        int w;
        int h;
        if (isFullscreen)
        {
            w = Screen.width;
            h = Screen.height;
            arc.SetDefaultAspectRatio();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }
        else
        {
            w = windowedX;
            h = windowedY;
        }

        Screen.SetResolution(w, h, !isFullscreen);
        
        yield return null;
    }
}