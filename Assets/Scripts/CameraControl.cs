using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public Transform target;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, new Vector3(target.position.x, target.position.y, -10), 1f);
        transform.position = new Vector3(Mathf.Round(transform.position.x * 16)/16, Mathf.Round(transform.position.y * 16)/16, transform.position.z);
    }

    void ShakeScreen()
    {

    }
}
