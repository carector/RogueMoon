using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullscreenSwitch : MonoBehaviour
{
    public uint windowedX = 640;
    public uint windowedY = 480;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            SetFullscreenMode(Screen.fullScreen);
        }
    }

    void SetFullscreenMode(bool isFullscreen)
    {
        if (isFullscreen)
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
        else
            Screen.SetResolution((int)windowedX, (int)windowedY, false);
    }
}