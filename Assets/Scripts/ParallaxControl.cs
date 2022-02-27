using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxControl : MonoBehaviour
{
    public bool scrollX;
    public Vector2 offset;
    public float minimumY = -1000;
    public float scrollMultiplier = 0.95f;

    CameraControl playerCam;
    Vector3 storedCamPos;
    float minimumJumpDistance = 3; // Distance camera needs to jump in a frame for us to jump with it

    // Start is called before the first frame update
    void Start()
    {
        playerCam = FindObjectOfType<CameraControl>();
        storedCamPos = playerCam.transform.position;
        transform.position = new Vector2(playerCam.transform.position.x + offset.x, playerCam.transform.position.y + offset.y);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (playerCam.transform.position != storedCamPos)
        {
            Vector3 difference = playerCam.transform.position - storedCamPos;
            if (!scrollX)
                difference.x = 0;
            transform.position += new Vector3(difference.x, difference.y, 0) * scrollMultiplier;
            storedCamPos = playerCam.transform.position;
        }
    }
}
