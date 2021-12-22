using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour
{
    public Image blackout;
    public RectTransform[] screens;

    int activeIndex = -1;
    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1;
        blackout.color = Color.black;
        StartCoroutine(FadeInScreen());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartGame()
    {
        StartCoroutine(StartGameCoroutine());
    }

    IEnumerator StartGameCoroutine()
    {
        yield return FadeOutScreen();
        SceneManager.LoadScene(2);
    }

    IEnumerator FadeInScreen()
    {
        while (blackout.color.a > 0)
        {
            blackout.color = new Color(0, 0, 0, blackout.color.a - 0.075f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }
        blackout.rectTransform.anchoredPosition = new Vector2(0, -3000);
    }

    IEnumerator FadeOutScreen()
    {
        blackout.rectTransform.anchoredPosition = new Vector2(0, 0);
        while (blackout.color.a < 1)
        {
            blackout.color = new Color(0, 0, 0, blackout.color.a + 0.075f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }
    }

    public void SwitchScreen(int index)
    {
        activeIndex = index;
    }

    private void FixedUpdate()
    {
        for(int i = 0; i < screens.Length; i++)
        {
            if (i != activeIndex)
                screens[i].anchoredPosition = new Vector2(80, 0);
            else
                screens[i].anchoredPosition = Vector2.Lerp(screens[i].anchoredPosition, new Vector2(-72, 0), 0.25f);
        }
    }
}
