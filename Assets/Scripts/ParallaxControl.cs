using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxControl : MonoBehaviour
{

    public Vector2 offset;
    public float minimumY = -1000;
    public float scrollMultiplier = 0.95f;

    Transform playerCam;
    Vector3 storedCamPos;
    float minimumJumpDistance = 3; // Distance camera needs to jump in a frame for us to jump with it

    Material mat;

    // Start is called before the first frame update
    void Start()
    {
        mat = GetComponent<Renderer>().material;
        playerCam = FindObjectOfType<CameraControl>().transform;
        storedCamPos = playerCam.position;
        //transform.position = new Vector2(playerCam.transform.position.x + offset.x, playerCam.transform.position.y + offset.y);

    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (playerCam.transform.position != storedCamPos)
        {
            Vector3 scrollOffset = Vector2.zero;
            if (Vector2.Distance(storedCamPos, playerCam.transform.position) > minimumJumpDistance)
            {
                scrollOffset = new Vector3(playerCam.transform.position.x + offset.x, playerCam.transform.position.y + offset.y);
                scrollOffset = new Vector3((transform.position.x * 32) / 32, (transform.position.y * 32) / 32);
                storedCamPos = playerCam.transform.position;
                return;
            }

            Vector3 difference = playerCam.transform.position - storedCamPos;
            scrollOffset += new Vector3(difference.x, difference.y, 0) * scrollMultiplier;
            mat.mainTextureOffset = scrollOffset;
            storedCamPos = playerCam.transform.position;
        }
    }
}
