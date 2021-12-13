using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoralScript : MonoBehaviour
{
    public SpriteRenderer[] spriteBits;
    public SpriteRenderer coralBottom;
    public Sprite[] bottomSpritePool;
    public GameObject gib;

    Collider2D col;

    // Start is called before the first frame update
    void Start()
    {
        col = GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BreakApart()
    {
        col.enabled = false;
        for(int i = 0; i < spriteBits.Length; i++)
        {
            for (int j = 0; j < 6; j++)
                Instantiate(gib, spriteBits[i].transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f)), Quaternion.identity);
        }

        coralBottom.sprite = bottomSpritePool[Random.Range(0, bottomSpritePool.Length)];
        foreach (SpriteRenderer s in spriteBits)
            Destroy(s.gameObject);
    }
}
