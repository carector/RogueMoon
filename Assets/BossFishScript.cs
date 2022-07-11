using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossFishScript : MonoBehaviour
{
    public Transform startingNodePath; // Nodes the boss must follow to prevent getting stuck
    public float segmentOffset = 3f;
    public float minXBeforeRotating;
    public Vector2[] positionPoints;
    Transform head;
    Transform headFocusPoint;
    List<Transform> bodySegments;
    Vector2 startingPos;
    PlayerController ply;
    float time;
    public int currentPathNode = 0;

    GameManager gm;
    float elapsedTime;
    Transform[] currentNodePath;
    public bool charging;
    Animator headAnim;
    // Start is called before the first frame update
    void Start()
    {
        ply = FindObjectOfType<PlayerController>();
        gm = FindObjectOfType<GameManager>();
        startingPos = transform.position;
        positionPoints = new Vector2[300];
        positionPoints[0] = transform.position;
        head = transform.GetChild(0);
        headAnim = transform.GetChild(1).GetComponent<Animator>();
        headFocusPoint = head.GetChild(0);
        bodySegments = new List<Transform>();

        for (int i = 1; i < transform.childCount; i++)
        {
            positionPoints[i] = transform.position - (Vector3.right * (2 * i));
            bodySegments.Add(transform.GetChild(i));
            bodySegments[i - 1].position = positionPoints[i];
            if (i > 2 && (i - 1) % 2 == 0 && i < transform.childCount - 1)
                bodySegments[i - 1].GetComponent<Animator>().Play("Boss_BodySegment", 0, (float)i / transform.childCount);
        }

        UpdateNodePath(startingNodePath);

        StartCoroutine(UpdateSegmentPositions());
    }

    IEnumerator UpdateSegmentPositions()
    {
        while (true)
        {
            while (Vector2.Distance(headFocusPoint.transform.position, positionPoints[0]) < 0.075f)
                yield return null;

            for (int i = positionPoints.Length - 1; i > 0; i--)
                positionPoints[i] = positionPoints[i - 1];

            positionPoints[0] = headFocusPoint.position;
            yield return new WaitForEndOfFrame();
        }
    }

    public void UpdateNodePath(Transform parent)
    {
        currentNodePath = new Transform[parent.childCount];
        for (int i = 0; i < currentNodePath.Length; i++)
            currentNodePath[i] = parent.GetChild(i);

        currentPathNode = 0;
    }

    public void StartChargeAttack()
    {
        charging = true;
        StartCoroutine(ChargeAttackCoroutine());
    }

    IEnumerator ChargeAttackCoroutine()
    {
        headAnim.Play("Boss_HeadLunge", 0, 0);
        float amount = 0.1125f;
        while (amount > 0)
        {
            head.position += head.right * amount;
            amount -= 0.005f;
            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < 40; i++)
        {
            head.position += head.right * 0.5f;
            yield return new WaitForFixedUpdate();
        }

        charging = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (gm.gameVars.isPaused)
            return;

        if (!charging)
        {
            elapsedTime += Time.fixedDeltaTime;
            head.position += head.right * 0.1125f;
            headFocusPoint.localPosition = Vector2.up * Mathf.Sin(elapsedTime * 3) * 0.1f;
        }

        // Update eye positions (scrapped)
        //for (int i = 0; i < 2; i++)
        //eyes[i].position = eyePositions[i].position + (ply.transform.position - eyePositions[i].position).normalized * 0.25f;

        // Update segment positions + rotate them towards next point
        // - Rotate towards nearest node if we're close to it
        if (currentPathNode < currentNodePath.Length)// && Vector2.Distance(head.transform.position, nodePath[currentPathNode].position) < 9)
        {
            float delta = 0.04f;
            if (Vector2.Angle(head.right, currentNodePath[currentPathNode].position - head.transform.position) > 20)
                delta = 0.1f;

            head.right = Vector3.Lerp(head.right, Vector2.MoveTowards(head.right, (currentNodePath[currentPathNode].position - head.transform.position).normalized, delta), 0.2f);
            if (Vector2.Distance(head.transform.position, currentNodePath[currentPathNode].position) < 2.5f)
                currentPathNode++;

            if (currentPathNode >= currentNodePath.Length)
                currentPathNode = 0;
        }
        /*
        else
        {
            // Also rotate away from a wall if we get too close (scan above and below)
            float distance = 8;
            Vector2 startPosition = head.position + head.right * 4;
            Debug.DrawRay(startPosition, Vector2.up * distance, Color.red, Time.fixedDeltaTime);
            Debug.DrawRay(startPosition, Vector2.down * distance, Color.red, Time.fixedDeltaTime);

            Vector3 target = ply.transform.position;
            Vector3 initialTarget = target;
            RaycastHit2D topHit = Physics2D.Raycast(startPosition, Vector2.up, distance, ~(1 << 11));
            RaycastHit2D bottomHit = Physics2D.Raycast(startPosition, Vector2.down, distance, ~(1 << 11));
            if (topHit.transform != null && topHit.transform.tag == "Ground")
            {
                print(topHit.transform.name);
                target += Vector3.down * 10 / Vector2.Distance(head.position, topHit.point);
            }
            if (bottomHit.transform != null && bottomHit.transform.tag == "Ground")
            {
                target += Vector3.up * 10 / Vector2.Distance(head.position, bottomHit.point);
            }

            head.right = Vector2.MoveTowards(head.right, (target - head.transform.position).normalized, 0.03f);
        }
        */
        int currentPoint = 0;
        float firstPointOffset = 5;
        Vector3 lastSegmentPosition = head.position;
        for (int i = 0; i < bodySegments.Count; i++)
        {
            // Distance here is the sum of the distances between each point
            // If we just used vector2.distance we'd have problems if the fish looped back on itself
            float distance = 0;
            while (currentPoint < positionPoints.Length - 1 && distance < segmentOffset + firstPointOffset)
            {
                currentPoint++;
                distance += Vector2.Distance(positionPoints[currentPoint - 1], positionPoints[currentPoint]);
            }

            // Offset head node a little bit to align properly
            if (i == 0)
                firstPointOffset = -1.75f;
            else
                firstPointOffset = 0;

            if (currentPoint < positionPoints.Length) // && distance >= segmentOffset
            {
                bodySegments[i].position = Vector2.Lerp(bodySegments[i].position, positionPoints[currentPoint], 0.33f);
                bodySegments[i].transform.right = lastSegmentPosition - (Vector3)positionPoints[currentPoint];
                lastSegmentPosition = positionPoints[currentPoint];
            }
        }
    }
}
