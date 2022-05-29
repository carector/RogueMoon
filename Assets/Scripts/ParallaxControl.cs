using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxControl : MonoBehaviour
{
    public bool scrollX;
    public bool scrollY = true;
    public Vector2 offset;
    public float minimumY = -1000;
    public float scrollMultiplier = 0.95f;
    public float highestPlayerYPosToTrack; // Won't follow player above this Y-position
    public bool useCutsceneCamera;
    Transform transformOverride;

    CameraControl playerCam;
    Vector3 storedCamPos;
    float minimumJumpDistance = 3; // Distance camera needs to jump in a frame for us to jump with it

    // Start is called before the first frame update
    void Start()
    {
        if (useCutsceneCamera)
            transformOverride = GameObject.Find("CutsceneCamera").transform;

        playerCam = FindObjectOfType<CameraControl>();
        if (playerCam == null && transformOverride != null)
            storedCamPos = transformOverride.position;
        else
            storedCamPos = playerCam.transform.position;
        //transform.position = new Vector2(playerCam.transform.position.x + offset.x, playerCam.transform.position.y + offset.y);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (playerCam.transform.position != storedCamPos && playerCam.transform.position.y < highestPlayerYPosToTrack)
        {
            Vector3 difference = playerCam.transform.position - storedCamPos;
            if(transformOverride != null)
                difference = transformOverride.position - storedCamPos;

            if (!scrollX)
                difference.x = 0;
            if (!scrollY)
                difference.y = 0;
            transform.position += new Vector3(difference.x, difference.y, 0) * scrollMultiplier;
        }
        storedCamPos = playerCam.transform.position;
    }
}
