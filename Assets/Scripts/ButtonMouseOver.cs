using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonMouseOver : MonoBehaviour
{
    public int offset = 64;
    public int minimumMouseXPos;

    Vector2 startPos;
    RectTransform t;
    bool mouseOver;
    PauseMenuManager pmm;
    Button b;

    // Start is called before the first frame update
    void Start()
    {
        b = GetComponent<Button>();
        pmm = FindObjectOfType<PauseMenuManager>();
        t = GetComponent<RectTransform>();
        startPos = t.anchoredPosition;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 target = Vector2.zero;
        if (mouseOver && Input.mousePosition.x >= minimumMouseXPos)
            target = new Vector2(offset, 0);

        t.anchoredPosition = Vector2.Lerp(t.anchoredPosition, startPos + target, 0.5f);
    }

    private void OnMouseEnter()
    {
        if (b.interactable)
        {
            mouseOver = true;
            int rand = Random.Range(0, 3);
        }
    }

    private void OnMouseExit()
    {
        mouseOver = false;
    }
}
