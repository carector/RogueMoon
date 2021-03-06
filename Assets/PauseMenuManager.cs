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
    GameManager gm;

    RectTransform titleScreenButtons;
    RectTransform pauseMenuButtons;
    RectTransform creditsScreen;
    RectTransform creditsScreenBgImage;

    float scrollPanelYPos = 0;
    int activeIndex = -1;

    int currentEncyclopediaEntry = -1;
    public int currentDescriptionLength;

    // Start is called before the first frame update
    void Start()
    {
        ChangeMenuDepth(-1);

        gm = FindObjectOfType<GameManager>();
        fishNoDataText = GameObject.Find("FishNoDataText").GetComponent<TextMeshProUGUI>();
        fishNameText = GameObject.Find("FishNameText").GetComponent<TextMeshProUGUI>();
        fishDescriptionText = GameObject.Find("FishDescriptionText").GetComponent<TextMeshProUGUI>();
        fishNoDataText.text = "";
        fishNameText.text = "";
        fishDescriptionText.text = "";

        encyclopediaScroll = GameObject.Find("EncyclopediaScrollbar").GetComponent<Scrollbar>();
        encyclopediaScrollPanel = GameObject.Find("EncyclopediaScrollPanel").GetComponent<RectTransform>();
        titleScreenButtons = GameObject.Find("TitleScreenButtons").GetComponent<RectTransform>();
        pauseMenuButtons = GameObject.Find("PauseMenuButtons").GetComponent<RectTransform>();
        creditsScreen = GameObject.Find("CreditsPanel").GetComponent<RectTransform>();
        creditsScreenBgImage = GameObject.Find("CreditsBG").GetComponent<RectTransform>();
        /*encyclopediaButtonTexts = new List<TextMeshProUGUI>();
        unlockedEntries = new bool[encyclopediaEntries.Length];

        for (int i = 0; i < 11; i++)
        {
            encyclopediaButtonTexts.Add(encyclopediaScrollPanel.GetChild(i).GetComponentInChildren<TextMeshProUGUI>());
            if (unlockedEntries[i])
                encyclopediaButtonTexts[i].text = encyclopediaEntries[i].fishName;
            else
                encyclopediaButtonTexts[i].text = "???";
        }*/


        //blackout.color = Color.black;
        //StartCoroutine(FadeInScreen());
    }

    // Update is called once per frame
    void Update()
    {
        if (gm.gameVars.currentScreen != GameManager.GameScreen.inGame)
        {
            // Encyclopedia (depracated)
            if (activeIndex == 1)
            {
                encyclopediaScrollPanel.anchoredPosition = Vector2.Lerp(encyclopediaScrollPanel.anchoredPosition, new Vector2(0, scrollPanelYPos), 0.25f);
                encyclopediaScrollPanel.anchoredPosition = new Vector2(encyclopediaScrollPanel.anchoredPosition.x, Mathf.RoundToInt(encyclopediaScrollPanel.anchoredPosition.y));

                if (Input.mouseScrollDelta.y != 0 && Input.mousePosition.x < 256 && !Input.GetMouseButton(0))
                    encyclopediaScroll.value = Mathf.Clamp(encyclopediaScroll.value - Input.mouseScrollDelta.y * 0.07f, 0, 1);

                if (currentEncyclopediaEntry >= 0 && unlockedEntries[currentEncyclopediaEntry])
                {
                    currentDescriptionLength = Mathf.RoundToInt(Mathf.Lerp(currentDescriptionLength, encyclopediaEntries[currentEncyclopediaEntry].fishDescription.Length, 0.025f));
                    fishDescriptionText.text = encyclopediaEntries[currentEncyclopediaEntry].fishDescription.Substring(0, currentDescriptionLength);
                }
            }
            else if (activeIndex == 2)
            {
                print(Input.mousePosition);
                creditsScreenBgImage.anchoredPosition = Vector2.Lerp(creditsScreenBgImage.anchoredPosition, (Input.mousePosition - new Vector3(512, 224)) * 0.02f, 0.1f);
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

    // Screens:
    // 0: Top menu / pause menu
    // 1: Encyclopedia
    // 2: Options
    // 3: Credits
    // 4: Quit

    public void ChangeMenuDepth(int index)
    {
        activeIndex = index;

        for (int i = 0; i < screens.Length; i++)
        {
            // Only need to hide top menu buttons if we're on the encyclopedia (only screen that covers everything)
            if (i != index && (i != 0 || index != 1))
                screens[i].anchoredPosition = new Vector2(0, -1000);
            else
                screens[i].anchoredPosition = Vector2.zero;
        }

        currentEncyclopediaEntry = -1;

        switch (activeIndex)
        {
            case 0:
                if (gm.gameVars.currentScreen == GameManager.GameScreen.paused)
                {
                    pauseMenuButtons.anchoredPosition = Vector2.zero;
                    titleScreenButtons.anchoredPosition = -Vector2.right * 1000;
                }
                else
                {
                    pauseMenuButtons.anchoredPosition = -Vector2.right * 1000;
                    titleScreenButtons.anchoredPosition = Vector2.zero;
                }
                break;
            case 1:
                encyclopediaScrollPanel.anchoredPosition = new Vector2(encyclopediaScrollPanel.anchoredPosition.x, 0);
                encyclopediaScroll.value = 0;
                fishNameText.text = "";
                fishDescriptionText.text = "";
                fishNoDataText.text = "";
                break;
            case 2:
                creditsScreenBgImage.anchoredPosition = (Input.mousePosition - new Vector3(512, 224)) * 0.02f;
                break;
        }
    }

    public void UpdateEncyclopediaScroll()
    {
        if (encyclopediaScroll == null)
            return;
        scrollPanelYPos = Mathf.Clamp(Mathf.RoundToInt(encyclopediaScroll.value * 186), -2, 184);
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

    }
}
