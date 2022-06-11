using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public Transform target;
    public float maxY;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(target.position.x, target.position.y, -10), 1f);
            transform.position = new Vector3(Mathf.Round(transform.position.x * 16) / 16, Mathf.Round(Mathf.Min(transform.position.y, maxY) * 16) / 16, transform.position.z);
        }
        else
            transform.position = new Vector3(0, 0, -10f);
    }

    void ShakeScreen()
    {

    }
}
