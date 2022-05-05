using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    public Image blackout;
    public RectTransform[] screens;
    public FishInfo[] encyclopediaEntries;

    Scrollbar encyclopediaScroll;
    RectTransform encyclopediaScrollPanel;
    List<TextMeshProUGUI> encyclopediaButtonTexts;
    bool[] unlockedEntries;
    TextMeshProUGUI fishNoDataText;
    TextMeshProUGUI fishNameText;
    TextMeshProUGUI fishDescriptionText;

    float scrollPanelYPos = 0;
    int activeIndex = 2;

    int currentEncyclopediaEntry = -1;
    public int currentDescriptionLength;

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 0;

        fishNoDataText = GameObject.Find("FishNoDataText").GetComponent<TextMeshProUGUI>();
        fishNameText = GameObject.Find("FishNameText").GetComponent<TextMeshProUGUI>();
        fishDescriptionText = GameObject.Find("FishDescriptionText").GetComponent<TextMeshProUGUI>();
        fishNoDataText.text = "";
        fishNameText.text = "";
        fishDescriptionText.text = "";

        encyclopediaScroll = GameObject.Find("EncyclopediaScrollbar").GetComponent<Scrollbar>();
        encyclopediaScrollPanel = GameObject.Find("EncyclopediaScrollPanel").GetComponent<RectTransform>();
        encyclopediaButtonTexts = new List<TextMeshProUGUI>();
        unlockedEntries = new bool[encyclopediaEntries.Length];
        unlockedEntries[0] = true;
        for (int i = 0; i < 11; i++)
        {
            encyclopediaButtonTexts.Add(encyclopediaScrollPanel.GetChild(i).GetComponentInChildren<TextMeshProUGUI>());
            if (unlockedEntries[i])
                encyclopediaButtonTexts[i].text = encyclopediaEntries[i].fishName;
            else
                encyclopediaButtonTexts[i].text = "???";
        }
        

        //blackout.color = Color.black;
        //StartCoroutine(FadeInScreen());
    }

    // Update is called once per frame
    void Update()
    {
        if (activeIndex == 2)
        {
            encyclopediaScrollPanel.anchoredPosition = Vector2.Lerp(encyclopediaScrollPanel.anchoredPosition, new Vector2(0, scrollPanelYPos), 0.25f);
            encyclopediaScrollPanel.anchoredPosition = new Vector2(encyclopediaScrollPanel.anchoredPosition.x, Mathf.RoundToInt(encyclopediaScrollPanel.anchoredPosition.y));

            if(currentEncyclopediaEntry >= 0 && unlockedEntries[currentEncyclopediaEntry])
            {
                currentDescriptionLength = Mathf.RoundToInt(Mathf.Lerp(currentDescriptionLength, encyclopediaEntries[currentEncyclopediaEntry].fishDescription.Length, 0.025f));
                fishDescriptionText.text = encyclopediaEntries[currentEncyclopediaEntry].fishDescription.Substring(0, currentDescriptionLength);
            }
        }
    }

    public void StartGame()
    {
        StartCoroutine(StartGameCoroutine());
    }

    IEnumerator StartGameCoroutine()
    {
        yield return FadeOutScreen();
        PlayerPrefs.SetInt("FLOE_LAST_CHECKPOINT", -1);
        PlayerPrefs.SetInt("FLOE_LAST_MUSIC", 0);
        PlayerPrefs.SetFloat("FLOE_LAST_MUSIC_PITCH", 1);
        PlayerPrefs.SetInt("FLOE_LAST_AMBIENCE", -1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(6);
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

    public void UpdateEncyclopediaScroll()
    {
        if (encyclopediaScroll == null)
            return;
        scrollPanelYPos = Mathf.Clamp(Mathf.RoundToInt(encyclopediaScroll.value * 100), 0, 100);
    }

    public void DisplayEncyclopediaEntry(int index)
    {
        if (!unlockedEntries[index])
        {
            fishNoDataText.text = "No data";
            fishNameText.text = "";
            fishDescriptionText.text = "";
            currentDescriptionLength = 0;
        }
        else
        {
            fishNoDataText.text = "";
            fishNameText.text = encyclopediaEntries[index].fishName;
        }
        currentEncyclopediaEntry = index;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < screens.Length; i++)
        {
            if (i != activeIndex)
                screens[i].anchoredPosition = new Vector2(80, 0);
            else
                screens[i].anchoredPosition = Vector2.Lerp(screens[i].anchoredPosition, new Vector2(-72, 0), 0.25f);
        }
    }
}
