using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum GameScreen
    {
        titleScreen,
        inGame,
        paused
    }

    [System.Serializable]
    public class GameSoundEffects
    {
        public AudioClip[] playerSounds;
        public AudioClip[] generalSounds;
        public AudioClip[] dialogVoices;
        public AudioClip[] music;
        public AudioClip[] ambience;
    }

    [System.Serializable]
    public class GameDialogSettings
    {
        public TextAsset JSONSource; // Source file containing all dialog / dialog in this level
        public TextData cachedTextData; // Deserialized version of JSONSource
        public bool isPrintingDialog;
        public Sprite[] portraits;
        public TMP_FontAsset normalFont;
        public TMP_FontAsset fuckedFont;
    }

    [System.Serializable]
    public class GameSpriteReferences
    {
        public Sprite[] numbers;
    }

    [System.Serializable]
    public class GameCheckpointReferences
    {
        public bool dontLoad;
        public Transform[] checkpoints;
        public Collider2D[] blockers;
        public int currentCheckpoint = 0;
        public int lastMusicIndex = -1;
        public int lastAmbienceIndex = -1;
        public float lastMusicPitch = 1;
    }

    [System.Serializable]
    public class GameGlobalVariables
    {
        public GameScreen currentScreen;
        public bool isPaused;
    }

    [System.Serializable]
    public class GamePrefabReferences
    {
        public GameObject smoochMark;
    }

    // Stores data used in NPC interactions
    [System.Serializable]
    public class TextData
    {
        public string filename;
        public Conversation[] conversations;

        [System.Serializable]
        public class Conversation // Stores multiple Dialogs. Used if more than one NPC talks in a dialog exchange
        {
            public string conversationId;
            public int[] initialPortraits;
            public Dialog[] dialog;
        }

        [System.Serializable]
        public class Dialog // Stores each line of text from the perspective of a single character
        {
            public int portrait; // 0 for left, 1 for right
            public Sentence[] sentences;
        }

        [System.Serializable]
        public class Sentence // Stores single line of text and its associated portrait sprite
        {
            public string text;
            public int portraitIndex;
        }

        public Conversation GetConversationFromID(string id)
        {
            for (int i = 0; i < conversations.Length; i++)
                if (conversations[i].conversationId == id)
                    return conversations[i];

            Debug.LogError("Conversation not found");
            return null;
        }
    }

    public GameGlobalVariables gameVars;
    public GameSoundEffects sfx;
    public GameDialogSettings dialogSettings;
    public GameSpriteReferences spriteRefs;
    public GameCheckpointReferences checkpointRefs;
    public GamePrefabReferences prefabRefs;

    CameraControl camControl;
    PauseMenuManager pmm;
    AudioSource musicSource;
    AudioSource ambienceSource;

    AudioSource sfxSource;
    AudioSource stoppableSfxSource;
    AudioSource priorityStoppableSfxSource;
    AudioSource dialogAudio;

    Image healthbarFill;
    Image darknessOverlay;
    RectTransform darknessMask;
    TextMeshProUGUI gameOverText;
    RectTransform[] gameOverButtons;
    RectTransform gameOverPanel;

    RectTransform hudHolder;
    RectTransform textboxHolder;
    RectTransform depthArrow;
    TextMeshProUGUI depthText;
    TextMeshProUGUI dialogText;
    Image leftPortrait;
    Image rightPortrait;
    Image toolbarFill;
    Image dialogAdvanceIcon;
    Image screenBlackout;
    Animator warningScreenAnimator;
    Animator cutsceneCam;

    PlayerController ply;
    Transform cam;
    Camera shadowCamera;

    NewgroundsUtility ngUtility;

    bool gameOverInProgress;
    bool skippedText;
    IEnumerator textSkipCoroutine;

    // used for when we unpause when the mech menu is already
    // open we don't fuck up our timescale
    float actionMenuStoredTimeScale;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this.transform.parent);

        Time.timeScale = 1;
        sfxSource = transform.GetChild(0).GetComponent<AudioSource>();
        stoppableSfxSource = transform.GetChild(0).GetComponent<AudioSource>();
        musicSource = transform.GetChild(2).GetComponent<AudioSource>();
        ambienceSource = transform.GetChild(3).GetComponent<AudioSource>();
        dialogAudio = transform.GetChild(4).GetComponent<AudioSource>();
        ngUtility = FindObjectOfType<NewgroundsUtility>();
        pmm = FindObjectOfType<PauseMenuManager>();
        cutsceneCam = GameObject.Find("CutsceneCamera").GetComponent<Animator>();
        camControl = FindObjectOfType<CameraControl>();

        Transform toolbar = GameObject.Find("ToolBar").transform;
        toolbarFill = toolbar.GetChild(2).GetComponent<Image>();
        textboxHolder = GameObject.Find("TextboxHolder").GetComponent<RectTransform>();
        dialogText = textboxHolder.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
        dialogAdvanceIcon = textboxHolder.GetChild(0).GetChild(1).GetComponent<Image>();
        dialogAdvanceIcon.color = Color.clear;
        leftPortrait = textboxHolder.GetChild(1).GetComponent<Image>();
        rightPortrait = textboxHolder.GetChild(2).GetComponent<Image>();

        darknessOverlay = GameObject.Find("DarknessOverlay").GetComponent<Image>();
        darknessMask = GameObject.Find("DarknessMask").GetComponent<RectTransform>();

        warningScreenAnimator = GameObject.Find("WarningScreen").GetComponent<Animator>();
        healthbarFill = GameObject.Find("HealthbarFill").GetComponent<Image>();
        ply = FindObjectOfType<PlayerController>();
        cam = FindObjectOfType<CameraControl>().transform.GetChild(0);
        shadowCamera = GameObject.Find("LightCamera").GetComponent<Camera>();
        screenBlackout = GameObject.Find("ScreenBlackout").GetComponent<Image>();
        screenBlackout.color = Color.black;
        depthArrow = GameObject.Find("DepthArrow").GetComponent<RectTransform>();
        depthText = depthArrow.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        hudHolder = GameObject.Find("HUDHolder").GetComponent<RectTransform>();
        gameOverText = GameObject.Find("GameOverText").GetComponent<TextMeshProUGUI>();
        gameOverPanel = gameOverText.transform.parent.GetComponent<RectTransform>();
        gameOverButtons = new RectTransform[2];
        for (int i = 0; i < 2; i++)
            gameOverButtons[i] = gameOverPanel.transform.GetChild(i + 1).GetComponent<RectTransform>();

        // Convert dialog from JSON file to nice, readable dialog
        dialogSettings.cachedTextData = DeserializeDialog(dialogSettings.JSONSource);

        if (SceneManager.GetActiveScene().buildIndex == 1)
            StartCoroutine(LoadSceneCoroutine(2));
        else
            StartCoroutine(LoadSceneCoroutine(-1));

        if (ply != null && ply.transform.position.y < -700)
            EnableDarknessCamera();
    }

    public void EnableDarknessCamera()
    {
        float val = 135 / 255f;
        shadowCamera.backgroundColor = new Color(val, val, val, 1);
    }

    public void LoadScene(int buildIndex)
    {
        StartCoroutine(LoadSceneCoroutine(buildIndex));
    }
    IEnumerator LoadSceneCoroutine(int buildIndex)
    {
        yield return FadeOutScreen();
        Time.timeScale = 1;

        if (buildIndex != -1)
        {
            AsyncOperation asyncLoadLevel = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            while (!asyncLoadLevel.isDone)
                yield return null;

            yield return PostLoadChecks(buildIndex);
        }
        else
            yield return PostLoadChecks(SceneManager.GetActiveScene().buildIndex);


        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return FadeInScreen();
    }

    // Performs any necessary changes when loading levels
    // ex: finding player reference, switching to proper game screen
    IEnumerator PostLoadChecks(int buildIndex)
    {
        // Determine UI screen based on which level we're loading
        // 0: logo
        // 1: singletonsLoader
        // 2: titlescreen
        // 3: levels
        if (buildIndex == 2)
        {
            gameVars.currentScreen = GameScreen.titleScreen;
            StartCoroutine(PlayMusic(4, 1));
            cutsceneCam.Play("CutsceneCameraTitleScreen");
            pmm.ChangeMenuDepth(0);
        }
        if (buildIndex >= 3)
        {
            // Load other in-game levels
            if (buildIndex == 3)
            {
                for (int i = 3; i <= 5; i++)
                    if (i != buildIndex)
                    {
                        AsyncOperation asyncLoadLevel = SceneManager.LoadSceneAsync(i, LoadSceneMode.Additive);
                        while (!asyncLoadLevel.isDone)
                            yield return null;
                    }
            }
            ply = FindObjectOfType<PlayerController>();
            camControl.target = ply.transform;
            pmm.ChangeMenuDepth(-1);
            cutsceneCam.Play("CutsceneCameraIdle");
            gameVars.currentScreen = GameScreen.inGame;
            gameVars.isPaused = false;
        }
    }

    public IEnumerator FadeInScreen()
    {
        while (screenBlackout.color.a > 0)
        {
            screenBlackout.color = new Color(0, 0, 0, screenBlackout.color.a - 0.075f);
            yield return new WaitForSecondsRealtime(Time.fixedDeltaTime);
            yield return new WaitForSecondsRealtime(Time.fixedDeltaTime);
        }
        screenBlackout.rectTransform.anchoredPosition = new Vector2(0, -3000);

    }

    public void CutToBlackScreen()
    {
        screenBlackout.rectTransform.anchoredPosition = new Vector2(0, 0);
        screenBlackout.color = Color.black;
    }

    IEnumerator FadeOutScreen()
    {
        screenBlackout.rectTransform.anchoredPosition = new Vector2(0, 0);
        while (screenBlackout.color.a < 1)
        {
            screenBlackout.color = new Color(0, 0, 0, screenBlackout.color.a + 0.075f);
            yield return new WaitForSecondsRealtime(Time.fixedDeltaTime);
            yield return new WaitForSecondsRealtime(Time.fixedDeltaTime);
        }
    }

    // Update is called once per frame
    void Update() // TODO
    {
        if (gameVars.currentScreen != GameScreen.inGame)
            return;

        UpdateHUD();

        if (ply.pResources.health > 0)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePausedState();
        }

        // Game over if health < 0
        if (ply.pResources.health <= 0 && !gameOverInProgress)
        {
            gameOverInProgress = true;
            StartCoroutine(GameOverSequence());
        }
    }

    public void TogglePausedState()
    {
        if (dialogSettings.isPrintingDialog)
            return;

        if (gameVars.isPaused && gameVars.currentScreen == GameScreen.paused)
        {
            gameVars.isPaused = false;
            gameVars.currentScreen = GameScreen.inGame;
            pmm.ChangeMenuDepth(-1);
            Time.timeScale = 1;
        }
        else if (!gameVars.isPaused && gameVars.currentScreen == GameScreen.inGame)
        {
            gameVars.isPaused = true;
            gameVars.currentScreen = GameScreen.paused;
            pmm.ChangeMenuDepth(0);
            Time.timeScale = 0;
        }
    }

    IEnumerator GameOverSequence()
    {
        musicSource.Stop();
        hudHolder.anchoredPosition = new Vector2(0, 1000);
        StartCoroutine(ply.Die());
        yield return new WaitForSeconds(4);
        Time.timeScale = 0;

        gameOverPanel.anchoredPosition = Vector2.zero;
        // Show game over text
        string t = "Mission failure";
        int sentenceLength = t.Length;

        for (int k = 0; k < sentenceLength; k++)
        {
            if (k % 2 == 0)
            {
                dialogAudio.Stop();
                dialogAudio.PlayOneShot(sfx.dialogVoices[Random.Range(0, sfx.dialogVoices.Length - 1)]);
            }
            gameOverText.text = t.Substring(0, k);
            yield return new WaitForSecondsRealtime(0.025f);
        }

        gameOverText.text = t;
        dialogAudio.Stop();
        //dialogAudio.PlayOneShot(sfx.dialogVoices[4]);
        yield return new WaitForSecondsRealtime(0.5f);

        for (int i = 0; i < 2; i++)
            gameOverButtons[i].anchoredPosition = new Vector2(0, gameOverButtons[i].anchoredPosition.y);

    }

    public void QuitToTitle() // TODO
    {
        Application.LoadLevel(1);
    }

    public void RestartFromCheckpoint() // TODO
    {
        PlayerPrefs.SetInt("FLOE_LAST_CHECKPOINT", checkpointRefs.currentCheckpoint);
        PlayerPrefs.Save();
        SceneManager.LoadScene(2);
    }

    public void UnlockMedal(int medal)
    {
        ngUtility.UnlockMedal(medal);
    }

    private void FixedUpdate()
    {
        if (gameVars.currentScreen != GameScreen.inGame)
            return;

        UpdateThrustBar();

        // Flash screen red based on health
        if (ply.pResources.health > 3 || ply.pResources.health == 0)
            CheckAndPlayClip(warningScreenAnimator, "RedScreenIdle");
        else
        {
            CheckAndPlayClip(warningScreenAnimator, "RedScreenFlash");

            if (ply.pResources.health > 1)
                warningScreenAnimator.SetFloat("FlashSpeed", 1);
            else
                warningScreenAnimator.SetFloat("FlashSpeed", 2);
        }
    }

    void UpdateHUD()
    {
        // Update healthbar
        healthbarFill.rectTransform.sizeDelta = new Vector2(5 * ply.pResources.health, 6);
        healthbarFill.rectTransform.anchoredPosition = new Vector2(3 + 2.5f * ply.pResources.health, 0);

        // Update depth arrow + depth text
        float lowestDepth = 640;
        float depthMultiplier = 2.75f;
        if (ply.transform.position.y < -800)
            depthText.text = "9999 m";
        else
            depthText.text = Mathf.Clamp(-Mathf.RoundToInt((ply.transform.position.y - 35) * depthMultiplier), 0, lowestDepth * depthMultiplier).ToString() + " m";
        depthArrow.anchoredPosition = new Vector2(4, Mathf.Round(80 - (Mathf.Clamp(Mathf.RoundToInt(-ply.transform.position.y), 0, lowestDepth) / lowestDepth) * 150));

        // Update darkness overlay
        if (ply.transform.position.y < 400 && ply.transform.position.y > 625)
        {
            darknessOverlay.color = Color.Lerp(darknessOverlay.color, new Color(0, 0, 0, Mathf.Clamp(Mathf.Abs(ply.transform.position.y + 400) / 100, 0, 0.98f)), 0.05f);
        }
        else
        {
            darknessOverlay.color = Color.Lerp(darknessOverlay.color, Color.clear, 0.05f);
        }

        if (Time.timeScale != 0)
        {
            Vector2 mouseDir = (Vector3)Camera.main.ScreenToWorldPoint(Input.mousePosition) - ply.transform.position;
            float angle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
            darknessMask.transform.rotation = Quaternion.Lerp(darknessMask.transform.rotation, Quaternion.Euler(new Vector3(0, 0, angle)), 0.5f);
        }
    }

    public void UpdateThrustBar()
    {
        toolbarFill.color = Color.white;
        float val = (ply.pMovement.hoverTime - ply.storedHoverTime) / ply.pMovement.hoverTime;
        Vector2 fillPos = new Vector2(2 - (val * 14), -1);
        Vector2 fillSize = new Vector2(val * 28, 10);

        toolbarFill.rectTransform.anchoredPosition = Vector2.Lerp(toolbarFill.rectTransform.anchoredPosition, fillPos, 0.5f);
        toolbarFill.rectTransform.sizeDelta = Vector2.Lerp(toolbarFill.rectTransform.sizeDelta, fillSize, 0.5f);
    }

    public void SpawnSmoochMark()
    {
        GameObject g = Instantiate(prefabRefs.smoochMark, cam);
        float randX = Random.Range(-3, 3f);
        g.transform.localPosition = new Vector3(3 * Mathf.Sign(randX) + randX, Random.Range(-4, 4f), 1);
        g.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-15f, 30f));
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.pitch = 1;
        sfxSource.PlayOneShot(clip);
    }

    public void StopSFX()
    {
        sfxSource.Stop();
    }

    public void PlaySFX(AudioClip clip, float pitch)
    {
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySFXStoppable(AudioClip clip, float pitch)
    {
        stoppableSfxSource.Stop();
        stoppableSfxSource.pitch = pitch;
        stoppableSfxSource.PlayOneShot(clip);
    }

    public void PlaySFXStoppablePriority(AudioClip clip, float pitch)
    {
        priorityStoppableSfxSource.Stop();
        priorityStoppableSfxSource.pitch = pitch;
        priorityStoppableSfxSource.PlayOneShot(clip);
    }

    public void StopPrioritySFX()
    {
        priorityStoppableSfxSource.Stop();
    }

    public void PlayAmbience(int ambienceIndex)
    {
        checkpointRefs.lastAmbienceIndex = ambienceIndex;
        ambienceSource.clip = sfx.ambience[ambienceIndex];
        ambienceSource.Play();
    }

    public void StopAmbience()
    {
        ambienceSource.Stop();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public IEnumerator TransitionMusic(int musicIndex, float pitch)
    {
        while (musicSource.volume > 0)
        {
            musicSource.volume -= 0.01f;
            yield return new WaitForEndOfFrame();
        }

        musicSource.Stop();
        if (musicIndex > -1)
            StartCoroutine(PlayMusic(musicIndex, pitch));
    }

    public IEnumerator PlayMusic(int musicIndex, float pitch)
    {
        checkpointRefs.lastMusicIndex = musicIndex;
        checkpointRefs.lastMusicPitch = pitch;
        musicSource.volume = 0;
        musicSource.pitch = pitch;
        musicSource.clip = sfx.music[musicIndex];
        musicSource.Play();

        while (musicSource.volume < 1)
        {
            musicSource.volume += 0.01f;
            yield return new WaitForEndOfFrame();
        }
    }

    public IEnumerator DisplayDialog(TextAsset src, string id)
    {
        if (dialogSettings.isPrintingDialog)
            yield break;
        dialogSettings.isPrintingDialog = true;
        actionMenuStoredTimeScale = Time.timeScale;
        Time.timeScale = 0;
        bool harpoonPlaying = ply.harpoonLoopingAudio.isPlaying;
        ply.harpoonLoopingAudio.Stop();


        // Get references
        TextData t = DeserializeDialog(src);
        TextData.Conversation c = t.GetConversationFromID(id);

        // Set initial portrait colors and sprites before moving textbox
        leftPortrait.color = new Color(0.5f, 0.5f, 0.5f, 1);
        rightPortrait.color = new Color(0.5f, 0.5f, 0.5f, 1);
        leftPortrait.sprite = dialogSettings.portraits[c.initialPortraits[0]];
        rightPortrait.sprite = dialogSettings.portraits[c.initialPortraits[1]];

        textboxHolder.anchoredPosition = new Vector2(0, -24f);

        // Dialog loop
        for (int i = 0; i < c.dialog.Length; i++)
        {
            // Sentence loop
            int dialogLength = c.dialog[i].sentences.Length;

            for (int j = 0; j < dialogLength; j++)
            {
                TextData.Sentence s = c.dialog[i].sentences[j];
                bool creepyPortrait = false;
                if (c.dialog[i].portrait == 0)
                {
                    if (s.portraitIndex == 5)
                        creepyPortrait = true;

                    leftPortrait.sprite = dialogSettings.portraits[s.portraitIndex];
                    leftPortrait.color = Color.white;
                    rightPortrait.color = new Color(0.5f, 0.5f, 0.5f, 1);
                }
                else
                {
                    if (s.portraitIndex == 5)
                        creepyPortrait = true;

                    rightPortrait.sprite = dialogSettings.portraits[s.portraitIndex];
                    rightPortrait.color = Color.white;
                    leftPortrait.color = new Color(0.5f, 0.5f, 0.5f, 1);
                }

                if (s.portraitIndex == 2)
                    dialogText.font = dialogSettings.fuckedFont;
                else
                    dialogText.font = dialogSettings.normalFont;


                // Char loop
                int sentenceLength = s.text.Length;
                string sentence = InsertLineBreaks(s.text);
                if (textSkipCoroutine != null)
                    StopCoroutine(textSkipCoroutine);
                textSkipCoroutine = WaitForTextSkip();
                StartCoroutine(textSkipCoroutine);
                if (!creepyPortrait)
                {
                    int k = 0;
                    while (k < sentenceLength && !skippedText)
                    {
                        if (k % 2 == 0)
                        {
                            dialogAudio.Stop();
                            dialogAudio.PlayOneShot(sfx.dialogVoices[Random.Range(0, sfx.dialogVoices.Length - 1)]);
                        }
                        dialogText.text = sentence.Substring(0, k);
                        yield return new WaitForSecondsRealtime(0.025f);
                        k++;
                    }

                    dialogText.text = sentence;
                    //dialogAudio.Stop();
                    //dialogAudio.PlayOneShot(sfx.dialogVoices[4]);
                }
                else
                {
                    dialogText.text = "";
                    musicSource.Stop();
                    dialogAudio.Play();
                    yield return new WaitForSecondsRealtime(2f);
                }

                dialogAdvanceIcon.color = Color.white;

                // Wait for key press to continue dialog
                yield return WaitForKeyPress();
                dialogAdvanceIcon.color = Color.clear;
                dialogAudio.Stop();
            }
        }

        if (harpoonPlaying)
            ply.harpoonLoopingAudio.Play();
        Time.timeScale = actionMenuStoredTimeScale;
        dialogSettings.isPrintingDialog = false;
        dialogText.text = "";
        textboxHolder.anchoredPosition = new Vector2(0, 64f);
    }

    IEnumerator WaitForTextSkip()
    {
        skippedText = false;
        while (Input.GetKey(KeyCode.E))
            yield return null;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return WaitForKeyPress();
        skippedText = true;
    }

    public IEnumerator DisplayDialogAutoAdvance(TextAsset src, string id)
    {
        dialogSettings.isPrintingDialog = true;
        bool harpoonPlaying = ply.harpoonLoopingAudio.isPlaying;
        ply.harpoonLoopingAudio.Stop();

        // Get references
        TextData t = DeserializeDialog(src);
        TextData.Conversation c = t.GetConversationFromID(id);

        // Set initial portrait colors and sprites before moving textbox
        leftPortrait.color = new Color(0.5f, 0.5f, 0.5f, 1);
        rightPortrait.color = new Color(0.5f, 0.5f, 0.5f, 1);
        leftPortrait.sprite = dialogSettings.portraits[c.initialPortraits[0]];
        rightPortrait.sprite = dialogSettings.portraits[c.initialPortraits[1]];

        textboxHolder.anchoredPosition = new Vector2(0, -24f);

        // Dialog loop
        for (int i = 0; i < c.dialog.Length; i++)
        {
            // Sentence loop
            int dialogLength = c.dialog[i].sentences.Length;

            for (int j = 0; j < dialogLength; j++)
            {
                TextData.Sentence s = c.dialog[i].sentences[j];
                bool creepyPortrait = false;
                if (c.dialog[i].portrait == 0)
                {
                    if (s.portraitIndex == 5)
                        creepyPortrait = true;

                    leftPortrait.sprite = dialogSettings.portraits[s.portraitIndex];
                    leftPortrait.color = Color.white;
                    rightPortrait.color = new Color(0.5f, 0.5f, 0.5f, 1);
                }
                else
                {
                    if (s.portraitIndex == 5)
                        creepyPortrait = true;

                    rightPortrait.sprite = dialogSettings.portraits[s.portraitIndex];
                    rightPortrait.color = Color.white;
                    leftPortrait.color = new Color(0.5f, 0.5f, 0.5f, 1);
                }

                // Char loop
                int sentenceLength = s.text.Length;
                string sentence = InsertLineBreaks(s.text);

                if (!creepyPortrait)
                {
                    for (int k = 0; k < sentenceLength; k++)
                    {
                        if (k % 2 == 0)
                        {
                            dialogAudio.Stop();
                            dialogAudio.PlayOneShot(sfx.dialogVoices[Random.Range(0, sfx.dialogVoices.Length - 1)]);
                        }
                        dialogText.text = sentence.Substring(0, k);
                        yield return new WaitForSecondsRealtime(0.025f);
                    }

                    dialogText.text = sentence;
                    //dialogAudio.Stop();
                    //dialogAudio.PlayOneShot(sfx.dialogVoices[4]);
                }
                else
                {
                    dialogText.text = "";
                    musicSource.Stop();
                    dialogAudio.Play();
                    yield return new WaitForSecondsRealtime(2f);
                }

                dialogAdvanceIcon.color = Color.white;

                // Wait for key press to continue dialog
                dialogAdvanceIcon.color = Color.clear;
                dialogAudio.Stop();
            }
        }

        dialogSettings.isPrintingDialog = false;
        dialogText.text = "";
        textboxHolder.anchoredPosition = new Vector2(0, 40f);
    }

    public TextData DeserializeDialog(TextAsset t)
    {
        if (t == null)
        {
            Debug.LogError("No JSON file provided");
            return null;
        }

        // No need to deserialize again if we've already cached it
        if (t.name.Equals(dialogSettings.cachedTextData.filename))
        {
            print("TextData already cached");
            return dialogSettings.cachedTextData;
        }

        dialogSettings.cachedTextData = JsonConvert.DeserializeObject<TextData>(t.text);
        return dialogSettings.cachedTextData;
    }

    IEnumerator WaitForKeyPress()
    {
        // update to new input system later
        while (!Input.GetKeyDown(KeyCode.E))
            yield return null;
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

    public void CheckAndPlayClip(Animator anim, string clipName)
    {
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(clipName))
            anim.Play(clipName, 0, 0);
    }

    string InsertLineBreaks(string s)
    {
        string t = "";

        for (int i = 0; i < s.Length; i++)
        {
            // Add line break
            if (s[i] == '\\')
                t += "\n";
            else
                t += s[i];
        }
        return t;
    }
}
