using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossFishScript : MonoBehaviour
{
    public float segmentOffset = 3f;
    public float minXBeforeRotating;
    public Vector2[] positionPoints;
    Transform head;
    List<Transform> bodySegments;
    Vector2 startingPos;
    PlayerController ply;
    float time;

    // Start is called before the first frame update
    void Start()
    {
        ply = FindObjectOfType<PlayerController>();
        startingPos = transform.position;
        positionPoints = new Vector2[300];
        positionPoints[0] = transform.position;
        head = transform.GetChild(0);
        bodySegments = new List<Transform>();
        for (int i = 1; i < transform.childCount; i++)
        {
            positionPoints[i] = transform.position + Vector3.right * 3 * i;

            bodySegments.Add(transform.GetChild(i));
        }

        StartCoroutine(UpdateSegmentPositions());
    }

    IEnumerator UpdateSegmentPositions()
    {
        while (true)
        {
            while (Vector2.Distance(head.transform.position, positionPoints[0]) < 0.075f)
                yield return null;

            for (int i = positionPoints.Length - 1; i > 0; i--)
                positionPoints[i] = positionPoints[i - 1];

            positionPoints[0] = head.position;
            yield return new WaitForEndOfFrame();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        head.position += head.right*0.1185f;

        // Update segment positions + rotate them towards next point
        if(head.transform.position.x < minXBeforeRotating)
            head.right = Vector2.MoveTowards(head.right, (ply.transform.position - head.transform.position).normalized, 0.01f);

        int currentPoint = 0;
        Vector3 lastSegmentPosition = head.position;
        for (int i = 0; i < bodySegments.Count; i++)
        {
            // Distance here is the sum of the distances between each point
            // If we just used vector2.distance we'd have problems if the fish looped back on itself
            float distance = 0;
            while (currentPoint < positionPoints.Length - 1 && distance < segmentOffset)
            {
                currentPoint++;
                distance += Vector2.Distance(positionPoints[currentPoint-1], positionPoints[currentPoint]);
            }

            if (currentPoint < positionPoints.Length && distance >= segmentOffset)
            {
                bodySegments[i].position = Vector2.Lerp(bodySegments[i].position, positionPoints[currentPoint], 0.33f);
                bodySegments[i].transform.right = lastSegmentPosition - (Vector3)positionPoints[currentPoint];
                lastSegmentPosition = positionPoints[currentPoint];
            }
        }
    }
}
