using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;
using UnityEngine.EventSystems;

public class GameManager2 : MonoBehaviour
{
    public enum UIScreen
    {
        titleScreen,
        inGame,
        settings,
        cutscene
    }

    [System.Serializable]
    public class GameVariables
    {
        public bool gamePaused;
        public bool isLoadingLevel;
        public UIScreen activeUIScreen;
        public int titleScreenDepth;
        public Vector2 screenSize;
        public int startingLevelBuildIndex;
    }
    [System.Serializable]
    public class GamePublicReferences
    {
        public Transform globalCameraHolderReference;
        public AudioMixer mixer;
        public EventSystem eventSystem;
        
    }
    [System.Serializable]
    public class GameSoundEffects
    {
        public AudioClip[] musicTracks;
        public AudioClip[] generalSfx;
        public AudioClip[] playerSfx;
        public AudioClip[] uiSfx;
    }
    [System.Serializable]
    public class GameSaveData
    {
        public int furthestUnlockedLevel;
        public bool[] playedCutscenes = new bool[3];
    }

    // Main class references
    public GameVariables gm_gameVars;
    public GamePublicReferences gm_gameRefs;
    public GameSoundEffects gm_gameSfx;
    public GameSaveData gm_gameSaveData;

    // Audio references
    AudioSource musicSource;
    AudioSource ambienceSource;
    AudioSource sfxSource;
    AudioSource sfxSourceStoppable;

    // UI references
    Transform titleArt;
    Transform cutsceneArt;
    RectTransform titleScreenPanel;
    RectTransform settingsPanel;
    RectTransform hudPanel;
    RectTransform cutscenePanel;
    TextMeshProUGUI timerText;
    Animator timerAnimator;
    Animator gameOverPopupAnimator;

    Image popupTitle;
    Image popupContinue;

    RectTransform ratArrivalText;

    Image blackScreenOverlay;
    RectTransform titleMenu;
    RectTransform levelSelectMenu;
    RectTransform creditsMenu;
    RectTransform quitMenu;
    RectTransform quitButton;
    TextMeshProUGUI cutsceneText;
    TextMeshProUGUI cutsceneShakeText;
    Slider sfxSlider;
    Slider musicSlider;
    Slider ambSlider;
    Slider scoreSlider;
    TextMeshProUGUI scoreText;
    Image sackFullnessImage;
    public List<Button> levelSelectButtons;

    // Other references
    NewgroundsUtility ng;
    Transform cam;
    CameraFollow camFollow;
    PlayerController ply;

    // Local variables
    bool initialized;

    void Start()
    {
        DontDestroyOnLoad(transform.parent.gameObject);
        GetReferences();
        ReadSaveData();

        /*
        if (PlayerPrefs.HasKey("FURTHEST_UNLOCKED_LEVEL"))
            gm_gameSaveData.furthestUnlockedLevel = PlayerPrefs.GetInt("FURTHEST_UNLOCKED_LEVEL");
        else
        {
            PlayerPrefs.SetInt("FURTHEST_UNLOCKED_LEVEL", 0);
            PlayerPrefs.Save();
        }*/

        if (gm_gameSaveData.playedCutscenes.Length == 0)
            gm_gameSaveData.playedCutscenes = new bool[3];

        UpdateUnlockedLevels();
        LoadAudioLevelsFromPlayerPrefs();

        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            SceneManager.LoadScene(2);
            PlayMusic(gm_gameSfx.musicTracks[1]);
        }
        else
            LevelLoadedUpdates(SceneManager.GetActiveScene().buildIndex);

        StartCoroutine(FadeFromBlack());
        initialized = true;
    }
    void Update()
    {
        if (!initialized)
            return;

        UpdateUI();

        // Fullscreen toggle
        if (Input.GetKeyDown(KeyCode.F4))
            SetFullscreenMode(!Screen.fullScreen);

        //if (Input.GetKeyDown(KeyCode.Alpha9)) // TEMP TEMP TEMP
           // SetTitleScreenDepth(5);


        if (gm_gameRefs.currentLevel != null)
        {
            // Pause toggle
            if (Input.GetKeyDown(KeyCode.Escape) && !gm_gameRefs.currentLevel.levelOver && !gm_gameVars.isLoadingLevel && gm_gameVars.activeUIScreen == UIScreen.inGame)
            {
                SetPausedState(!gm_gameVars.gamePaused);
            }

            // Retry level
            if (!gm_gameVars.gamePaused && Input.GetKeyDown(KeyCode.R) && !gm_gameVars.isLoadingLevel)
            {
                ply.p_states.canMove = false;
                LoadLevel(SceneManager.GetActiveScene().buildIndex);
            }
        }

    }

    public void CheckAndPlayClip(string clipName, Animator anim)
    {
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(clipName))
        {
            anim.Play(clipName);
        }
    } // Plays animation clip if it isn't already playing
    public IEnumerator FadeFromBlack()
    {
        blackScreenOverlay.rectTransform.anchoredPosition = Vector2.zero;
        while (blackScreenOverlay.color.a > 0)
        {
            blackScreenOverlay.color = new Color(0, 0, 0, blackScreenOverlay.color.a - 0.075f);
            yield return new WaitForSecondsRealtime(0.025f);
        }
        blackScreenOverlay.rectTransform.anchoredPosition = Vector2.up * 1000;
    }
    public IEnumerator FadeToBlack()
    {
        blackScreenOverlay.rectTransform.anchoredPosition = Vector2.zero;
        while (blackScreenOverlay.color.a < 1)
        {
            blackScreenOverlay.color = new Color(0, 0, 0, blackScreenOverlay.color.a + 0.075f);
            yield return new WaitForSecondsRealtime(0.025f);
        }
    }
    void GetReferences()
    {
        cam = gm_gameRefs.globalCameraHolderReference.GetChild(0);
        camFollow = FindObjectOfType<CameraFollow>();

        // UI references
        titleArt = GameObject.Find("TitleArt").transform;
        cutsceneArt = GameObject.Find("CutsceneBG").transform;
        blackScreenOverlay = GameObject.Find("BlackScreenOverlay").GetComponent<Image>();
        sfxSlider = GameObject.Find("SFXVolumeSlider").GetComponent<Slider>();
        ambSlider = GameObject.Find("AmbienceVolumeSlider").GetComponent<Slider>();
        musicSlider = GameObject.Find("MusicVolumeSlider").GetComponent<Slider>();
        popupTitle = GameObject.Find("PopupTitle").GetComponent<Image>();
        popupContinue = GameObject.Find("PopupButtonContinue").GetComponent<Image>();
        hudPanel = GameObject.Find("HUDPanel").GetComponent<RectTransform>();
        settingsPanel = GameObject.Find("SettingsPanel").GetComponent<RectTransform>();
        titleScreenPanel = GameObject.Find("TitleScreenPanel").GetComponent<RectTransform>();
        timerText = GameObject.Find("TimerText").GetComponent<TextMeshProUGUI>();
        timerAnimator = timerText.GetComponent<Animator>();
        gameOverPopupAnimator = GameObject.Find("GameOverPopupHolder").GetComponent<Animator>();
        titleMenu = GameObject.Find("TopMenuPanel").GetComponent<RectTransform>();
        levelSelectMenu = GameObject.Find("LevelSelectPanel").GetComponent<RectTransform>();
        scoreSlider = GameObject.Find("ScoreSlider").GetComponent<Slider>();
        scoreText = GameObject.Find("ScoreText").GetComponent<TextMeshProUGUI>();
        creditsMenu = GameObject.Find("CreditsPanel").GetComponent<RectTransform>();
        cutscenePanel = GameObject.Find("CutscenePanel").GetComponent<RectTransform>();
        quitMenu = GameObject.Find("QuitPanel").GetComponent<RectTransform>();
        ratArrivalText = GameObject.Find("RatsArrivedWarning").GetComponent<RectTransform>();
        quitButton = GameObject.Find("QuitButton").GetComponent<RectTransform>();
        sackFullnessImage = GameObject.Find("SackFullnessNumber").GetComponent<Image>();
        cutsceneText = GameObject.Find("CutsceneText").GetComponent<TextMeshProUGUI>();
        cutsceneShakeText = cutsceneText.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            quitButton.anchoredPosition = new Vector2(0, -282.5f);

        // Audio references
        musicSource = GameObject.Find("GameMusicSource").GetComponent<AudioSource>();
        sfxSource = GameObject.Find("GameSFXSource").GetComponent<AudioSource>();
        sfxSourceStoppable = GameObject.Find("GameSFXSourceStoppable").GetComponent<AudioSource>();
        ambienceSource = GameObject.Find("GameAmbienceSource").GetComponent<AudioSource>();

        ng = FindObjectOfType<NewgroundsUtility>();
    } // Obtain UI + GameObject references. Called by Start() and probably nowhere else
    public void InitializePlayer() { } // Readies / unfreezes player gameobject in-game
    public void SetPausedState(bool paused)
    {
        if (gm_gameVars.gamePaused)
        {
            CheckAndPlayClip("GameOverPopup_Default", gameOverPopupAnimator);
            Time.timeScale = 1;
        }
        else
        {
            ShowGameOverPopup(2);
        }

        gm_gameVars.gamePaused = paused;
    } // Pauses / unpauses game and performs necessary UI stuff
    public void UnlockMedal(int id)
    {
        ng.UnlockMedal(id);
    } // Newgrounds API, self-explanatory
    public void LoadLevel(int buildIndex)
    {
        if (gm_gameVars.isLoadingLevel)
            return;

        gm_gameVars.isLoadingLevel = true;
        StartCoroutine(LoadLevelCoroutine(buildIndex));
    }
    IEnumerator LoadLevelCoroutine(int buildIndex)
    {
        yield return FadeToBlack();
        Time.timeScale = 1;
        AsyncOperation asyncLoadLevel = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        while (!asyncLoadLevel.isDone)
            yield return null;
        LevelLoadedUpdates(buildIndex);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return FadeFromBlack();
        gm_gameVars.isLoadingLevel = false;
    }

    void LevelLoadedUpdates(int buildIndex)
    {
        gm_gameRefs.eventSystem.SetSelectedGameObject(null); // Deselect buttons
        gm_gameVars.gamePaused = false;
        camFollow.DestroyParallax();

        if (buildIndex == 2)
        {
            UpdateUnlockedLevels();
            gm_gameVars.activeUIScreen = UIScreen.titleScreen;
            PlayMusic(gm_gameSfx.musicTracks[1]);
        }
        else if (FindObjectOfType<CutsceneHandler>() != null)
        {
            gm_gameVars.activeUIScreen = UIScreen.cutscene;
        }
        else
        {
            if (musicSource.clip != gm_gameSfx.musicTracks[0] && musicSource.clip != gm_gameSfx.musicTracks[5])
                musicSource.Stop();

            gameOverPopupAnimator.Play("GameOverPopup_Default", 0, 0);
            gm_gameRefs.currentLevel = FindObjectOfType<LevelManager>();
            ply = FindObjectOfType<PlayerController>();
            gm_gameVars.activeUIScreen = UIScreen.inGame;
        }
    }

    public void SetScoreValueLerp(int score, int maxScore)
    {
        scoreSlider.maxValue = maxScore;
        scoreSlider.value = Mathf.Lerp(scoreSlider.value, score, 0.25f);
        scoreText.text = score + " / " + maxScore;
    }

    public void SetScoreValue(int score, int maxScore)
    {
        scoreSlider.maxValue = maxScore;
        scoreSlider.value = score;
        scoreText.text = score + " / " + maxScore;
    }

    void LoadAudioLevelsFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey("AMB_VOLUME"))
        {
            musicSlider.value = PlayerPrefs.GetInt("MUS_VOLUME") / 4;
            sfxSlider.value = PlayerPrefs.GetInt("SFX_VOLUME") / 4;
            ambSlider.value = PlayerPrefs.GetInt("AMB_VOLUME") / 4;
        }

        UpdateMusicVolume();
        UpdateSFXVolume();
        UpdateAmbienceVolume();

    } // Sets audio levels to match stored values in PlayerPrefs
    public void PlaySFX(AudioClip sfx)
    {
        sfxSource.PlayOneShot(sfx);
    }

    public void PlaySFXStoppable(AudioClip sfx)
    {
        sfxSourceStoppable.Stop();
        sfxSourceStoppable.PlayOneShot(sfx);
    }

    public void PlayMusic(AudioClip track)
    {
        if (musicSource.clip == track && musicSource.isPlaying)
            return;
        musicSource.clip = track;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void UnlockNextLevel()
    {
        if (SceneManager.GetActiveScene().buildIndex-3 == gm_gameSaveData.furthestUnlockedLevel)
        {
            gm_gameSaveData.furthestUnlockedLevel++;
            WriteSaveData();
            //PlayerPrefs.SetInt("FURTHEST_UNLOCKED_LEVEL", gm_gameSaveData.furthestUnlockedLevel);
            //PlayerPrefs.Save();
        }
    }

    void ReadSaveData()
    {
        string prefix = @"idbfs/RatKing";

        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            prefix = Application.persistentDataPath;

        if (!File.Exists(prefix + @"/savedata.json"))
        {
            gm_gameSaveData = new GameSaveData();
            return;
        }
        string json = File.ReadAllText(prefix + @"/savedata.json");
        gm_gameSaveData = JsonUtility.FromJson<GameSaveData>(json);
    }
    void SetFullscreenMode(bool isFullscreen)
    {
        int width = (int)gm_gameVars.screenSize.x;
        int height = (int)gm_gameVars.screenSize.y;
        if (isFullscreen)
        {
            width = Screen.currentResolution.width;
            height = Screen.currentResolution.height;
        }

        Screen.SetResolution(width, height, isFullscreen);
    } // Should work fine without AspectRatioController
    public void ScreenShake()
    {
        StartCoroutine(ScreenShakeCoroutine(5));
    }
    public void ScreenShake(float intensity)
    {
        StartCoroutine(ScreenShakeCoroutine(intensity));
    }
    IEnumerator ScreenShakeCoroutine(float intensity)
    {
        for (int i = 0; i < 10; i++)
        {
            cam.localPosition = new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f)) * intensity;
            intensity /= 1.25f;
            yield return new WaitForFixedUpdate();
        }
        cam.localPosition = Vector2.zero;
    }

    void UpdateUI()
    {
        switch (gm_gameVars.activeUIScreen)
        {
            case UIScreen.titleScreen:
                cutscenePanel.anchoredPosition = Vector2.up * 1000;
                hudPanel.anchoredPosition = Vector2.up * 1000;
                settingsPanel.anchoredPosition = Vector2.up * 1000;
                titleScreenPanel.anchoredPosition = Vector2.zero;

                Vector2 right = Vector2.right * 2000;
                titleMenu.anchoredPosition = right;
                levelSelectMenu.anchoredPosition = right;
                creditsMenu.anchoredPosition = right;
                quitMenu.anchoredPosition = right;

                switch (gm_gameVars.titleScreenDepth)
                {
                    case 0:
                        titleMenu.anchoredPosition = Vector2.zero;
                        break;
                    case 1:
                        levelSelectMenu.anchoredPosition = Vector2.zero;
                        break;
                    case 2:
                        gm_gameVars.activeUIScreen = UIScreen.settings;
                        break;
                    case 3:
                        creditsMenu.anchoredPosition = Vector2.zero;
                        break;
                    case 4:
                        quitMenu.anchoredPosition = Vector2.zero;
                        break;
                }
                cutsceneArt.localPosition = new Vector3(-100, 0, 20);
                if (gm_gameVars.titleScreenDepth == 0 || gm_gameVars.titleScreenDepth == 5)
                    titleArt.localPosition = new Vector3(0, 0, 20);
                else
                    titleArt.localPosition = new Vector3(-100, 0, 20);

                break;

            case UIScreen.settings:
                titleArt.localPosition = new Vector3(-100, 0, 20);
                cutsceneArt.localPosition = new Vector3(-100, 0, 20);
                cutscenePanel.anchoredPosition = Vector2.up * 1000;
                settingsPanel.anchoredPosition = Vector2.zero;
                hudPanel.anchoredPosition = Vector2.up * 1000;
                titleScreenPanel.anchoredPosition = Vector2.up * 1000;
                break;

            case UIScreen.cutscene:
                titleArt.localPosition = new Vector3(-100, 0, 20);
                cutsceneArt.localPosition = new Vector3(0, 0, 20);
                cutscenePanel.anchoredPosition = Vector2.zero;
                settingsPanel.anchoredPosition = Vector2.up * 1000;
                hudPanel.anchoredPosition = Vector2.up * 1000;
                titleScreenPanel.anchoredPosition = Vector2.up * 1000;
                break;

            case UIScreen.inGame:
                titleArt.localPosition = new Vector3(-100, 0, 20);
                cutsceneArt.localPosition = new Vector3(-100, 0, 20);
                cutscenePanel.anchoredPosition = Vector2.up * 1000;
                hudPanel.anchoredPosition = Vector2.zero;
                settingsPanel.anchoredPosition = Vector2.up * 1000;
                titleScreenPanel.anchoredPosition = Vector2.up * 1000;
                break;
        }
    }

    public void SetCutsceneDialog(string text)
    {
        cutsceneText.text = text;
        cutsceneShakeText.text = text;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void SetTitleScreenDepth(int depth)
    {
        gm_gameVars.titleScreenDepth = depth;
    }

    public void SetActiveUIScreen(int index)
    {
        UIScreen screen = default;
        switch (index)
        {
            case 0:
                screen = UIScreen.inGame;
                break;
            case 1:
                screen = UIScreen.settings;
                break;
            case 2:
                screen = UIScreen.titleScreen;
                break;
            case 3:
                screen = UIScreen.cutscene;
                break;
        }
        gm_gameVars.activeUIScreen = screen;
    }

    public void SettingsBackButton()
    {
        if (SceneManager.GetActiveScene().buildIndex == 2)
        {
            SetTitleScreenDepth(0);
            SetActiveUIScreen(2);
        }
        else
        {
            gm_gameRefs.eventSystem.SetSelectedGameObject(null); // Deselect buttons
            SetActiveUIScreen(0);
        }
    }

    public void RetryLevel()
    {
        if (ply.dea || gm_gameVars.gamePaused)
            LoadLevel(SceneManager.GetActiveScene().buildIndex);
    }
    public void QuitToTitle()
    {
        LoadLevel(2);
    }

    public void UpdateAmbienceVolume()
    {
        if (ambSlider == null)
            return;

        int volume = (int)ambSlider.value * 4;

        if (volume == -40)
            volume = -80;

        PlayerPrefs.SetInt("AMB_VOLUME", volume);
        gm_gameRefs.mixer.SetFloat("AmbienceVolume", volume);
        PlayerPrefs.Save();
    }
    public void UpdateMusicVolume()
    {
        if (musicSlider == null)
            return;

        int volume = (int)musicSlider.value * 4;

        if (volume == -40)
            volume = -80;

        PlayerPrefs.SetInt("MUS_VOLUME", volume);
        gm_gameRefs.mixer.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }
    public void UpdateSFXVolume()
    {
        if (sfxSlider == null)
            return;

        int volume = (int)sfxSlider.value * 4;

        if (volume == -40)
            volume = -80;

        PlayerPrefs.SetInt("SFX_VOLUME", volume);
        gm_gameRefs.mixer.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();

        if (initialized)
        {
            int rand = Random.Range(0, 3);
            PlaySFX(gm_gameSfx.uiSfx[rand]);
        }
    }

    public void UpdateUnlockedLevels()
    {
        int count = Mathf.Min(gm_gameSaveData.furthestUnlockedLevel, levelSelectButtons.Count - 1);

        for (int i = 0; i <= count; i++)
        {
            levelSelectButtons[i].interactable = true;

            levelSelectButtons[i].transform.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
        }
    }

    void WriteSaveData()
    {
        // Reason for idbfs prefix: https://itch.io/t/140214/persistent-data-in-updatable-webgl-games (don't question it) (it does not work anymore)
        string prefix = @"idbfs/RatKing";

        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            prefix = Application.persistentDataPath;
        else if (!Directory.Exists(prefix))
            Directory.CreateDirectory(prefix);

        string json = JsonUtility.ToJson(gm_gameSaveData);
        File.WriteAllText(prefix + @"/savedata.json", json);
    }
}