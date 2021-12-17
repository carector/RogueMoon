using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxControl : MonoBehaviour
{
    
    public Vector2 offset;
    public float scrollMultiplier = 0.95f;

    Transform playerCam;
    Vector3 storedCamPos;
    float minimumJumpDistance = 3; // Distance camera needs to jump in a frame for us to jump with it

    // Start is called before the first frame update
    void Start()
    {
        playerCam = FindObjectOfType<CameraControl>().transform;
        storedCamPos = playerCam.position;
        transform.position = new Vector2(playerCam.transform.position.x + offset.x, playerCam.transform.position.y + offset.y);

    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (playerCam.transform.position != storedCamPos)
        {
            if (Vector2.Distance(storedCamPos, playerCam.transform.position) > minimumJumpDistance)
            {
                transform.position = new Vector2(playerCam.transform.position.x + offset.x, playerCam.transform.position.y + offset.y);
                transform.position = new Vector2((transform.position.x * 32) / 32, (transform.position.y * 32) / 32);
                storedCamPos = playerCam.transform.position;
                return;
            }

            Vector3 difference = playerCam.transform.position - storedCamPos;
            transform.position += new Vector3(difference.x, difference.y, 0) * scrollMultiplier;
            storedCamPos = playerCam.transform.position;
        }
    }
}
