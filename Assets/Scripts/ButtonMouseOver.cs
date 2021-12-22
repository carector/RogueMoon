using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonMouseOver : MonoBehaviour
{
    public int offset = 64;

    Vector2 startPos;
    RectTransform t;
    bool mouseOver;
    TitleScreenManager tsm;
    Button b;

    // Start is called before the first frame update
    void Start()
    {
        b = GetComponent<Button>();
        tsm = FindObjectOfType<TitleScreenManager>();
        t = GetComponent<RectTransform>();
        startPos = t.anchoredPosition;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector2 target = Vector2.zero;
        if (mouseOver)
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
