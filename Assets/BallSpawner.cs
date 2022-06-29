using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    public GameObject prefabToSpawn;
    public BallReceiver receiver;
    GameObject spawnedObject;
    Transform spawnPosition;
    bool spawningObject;

    // Start is called before the first frame update
    void Start()
    {
        spawnPosition = transform.GetChild(0);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!spawningObject && spawnedObject == null)
        {
            StartCoroutine(SpawnTimer());
            spawningObject = true;
        }
    }

    IEnumerator SpawnTimer()
    {
        yield return new WaitForSeconds(2);
        SpawnObject();
        spawningObject = false;
    }

    public void SpawnObject()
    {
        spawnedObject = Instantiate(prefabToSpawn, spawnPosition.position, Quaternion.identity);
        spawnedObject.GetComponent<Rigidbody2D>().velocity = -transform.up * 3;
        if (receiver != null)
            receiver.objectToReceive = spawnedObject.GetComponent<GrabbableObject>();
    }
}
