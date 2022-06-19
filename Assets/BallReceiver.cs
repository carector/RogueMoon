using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallReceiver : MonoBehaviour
{
    public BlockerScript[] blockersToDisable;
    public GrabbableObject objectToReceive;

    Transform child;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (child != null)
        {
            child.transform.localPosition = Vector2.Lerp(child.transform.localPosition, Vector2.zero, 0.1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == objectToReceive.gameObject)
        {
            objectToReceive.transform.parent = transform;
            objectToReceive.DisableCollider();
            child = objectToReceive.transform;

            for (int i = 0; i < blockersToDisable.Length; i++)
                blockersToDisable[i].DisableSelf();
        }
    }
}
