using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
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

    public GameSoundEffects sfx;
    public GameDialogSettings dialogSettings;
    public GameSpriteReferences spriteRefs;
    public GameCheckpointReferences checkpointRefs;
    public GamePrefabReferences prefabRefs;

    public bool menuOpen;
    public bool gamePaused;

    AudioSource musicSource;
    AudioSource ambienceSource;

    AudioSource sfxSource;
    AudioSource stoppableSfxSource;
    AudioSource priorityStoppableSfxSource;
    AudioSource dialogAudio;
    AudioSource harpoonLoopingAudio;

    Image healthbarFill;
    Image darknessOverlay;
    RectTransform darknessMask;

    TextMeshProUGUI metalNumberText;
    TextMeshProUGUI harpoonNumberText;
    TextMeshProUGUI depthChargeNumberText;
    TextMeshProUGUI gameOverText;
    RectTransform[] gameOverButtons;
    RectTransform gameOverPanel;

    RectTransform hudHolder;
    RectTransform textboxHolder;
    RectTransform actionMenuHolder;
    RectTransform[] actionMenuCategories = new RectTransform[3];
    RectTransform depthArrow;
    TextMeshProUGUI depthText;
    TextMeshProUGUI dialogText;
    Image leftPortrait;
    Image rightPortrait;
    Image toolbarFill;
    Image dialogAdvanceIcon;
    Image screenBlackout;
    Animator warningScreenAnimator;

    PlayerController ply;
    Transform cam;

    bool gameOverInProgress;
    bool skippedText;
    IEnumerator textSkipCoroutine;

    // used for when we unpause when the mech menu is already
    // open we don't fuck up our timescale
    float actionMenuStoredTimeScale;

    /* PlayerPrefs reference
     * Last checkpoint position index: FLOE_LAST_CHECKPOINT
     * Last played music clip index: FLOE_LAST_MUSIC
     * Last played ambience clip index: FLOE_LAST_AMBIENCE
     * 
     * 
     */

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1;
        sfxSource = transform.GetChild(0).GetComponent<AudioSource>();
        stoppableSfxSource = transform.GetChild(0).GetComponent<AudioSource>();
        musicSource = transform.GetChild(2).GetComponent<AudioSource>();
        ambienceSource = transform.GetChild(3).GetComponent<AudioSource>();
        dialogAudio = transform.GetChild(4).GetComponent<AudioSource>();

        Transform toolbar = GameObject.Find("ToolBar").transform;
        toolbarFill = toolbar.GetChild(2).GetComponent<Image>();
        textboxHolder = GameObject.Find("TextboxHolder").GetComponent<RectTransform>();
        dialogText = textboxHolder.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
        dialogAdvanceIcon = textboxHolder.GetChild(0).GetChild(1).GetComponent<Image>();
        dialogAdvanceIcon.color = Color.clear;
        leftPortrait = textboxHolder.GetChild(1).GetComponent<Image>();
        rightPortrait = textboxHolder.GetChild(2).GetComponent<Image>();
        actionMenuHolder = GameObject.Find("ActionMenu").GetComponent<RectTransform>();
        for (int i = 0; i < 3; i++)
            actionMenuCategories[i] = actionMenuHolder.GetChild(i).GetComponent<RectTransform>();

        metalNumberText = actionMenuHolder.GetChild(3).GetComponent<TextMeshProUGUI>();
        harpoonNumberText = actionMenuHolder.GetChild(4).GetComponent<TextMeshProUGUI>();
        depthChargeNumberText = actionMenuHolder.GetChild(5).GetComponent<TextMeshProUGUI>();
        darknessOverlay = GameObject.Find("DarknessOverlay").GetComponent<Image>();
        darknessMask = GameObject.Find("DarknessMask").GetComponent<RectTransform>();

        warningScreenAnimator = GameObject.Find("WarningScreen").GetComponent<Animator>();
        healthbarFill = GameObject.Find("HealthbarFill").GetComponent<Image>();
        harpoonLoopingAudio = GameObject.Find("HarpoonLoopingAudio").GetComponent<AudioSource>();
        ply = FindObjectOfType<PlayerController>();
        cam = FindObjectOfType<CameraControl>().transform.GetChild(0);

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

        StartCoroutine(LoadIntoLevel());
    }

    IEnumerator LoadIntoLevel()
    {
        if (!checkpointRefs.dontLoad)
        {
            if (!PlayerPrefs.HasKey("FLOE_LAST_CHECKPOINT"))
            {
                print("Playerprefs weren't found");
                PlayerPrefs.SetInt("FLOE_LAST_CHECKPOINT", -1);
                PlayerPrefs.SetInt("FLOE_LAST_MUSIC", 0);
                PlayerPrefs.SetFloat("FLOE_LAST_MUSIC_PITCH", 1);
                PlayerPrefs.SetInt("FLOE_LAST_AMBIENCE", -1);
                PlayerPrefs.Save();
            }

            int checkpoint = PlayerPrefs.GetInt("FLOE_LAST_CHECKPOINT");
            int lastMusic = PlayerPrefs.GetInt("FLOE_LAST_MUSIC");
            float lastPitch = PlayerPrefs.GetFloat("FLOE_LAST_MUSIC_PITCH");
            int lastAmbience = PlayerPrefs.GetInt("FLOE_LAST_AMBIENCE");
            int loadedLevelIndex = 3;

            if (checkpoint >= 1 && checkpoint < 3)
            {
                checkpointRefs.blockers[0].enabled = true;
                loadedLevelIndex = 4;
            }
            else if (checkpoint >= 3)
            {
                checkpointRefs.blockers[1].enabled = true;
                loadedLevelIndex = 5;
            }

            if (checkpoint >= 0)
            {
                ply.transform.position = checkpointRefs.checkpoints[checkpoint].position;
                checkpointRefs.currentCheckpoint = checkpoint;
            }

            SceneManager.LoadSceneAsync(loadedLevelIndex, LoadSceneMode.Additive);
            while (!SceneManager.GetActiveScene().isLoaded)
                yield return null;

            if (checkpoint == 1)
                Destroy(GameObject.Find("Level2Dither"));

            // Fade in black screen
            yield return FadeInScreen();

            if (lastMusic > -1)
                StartCoroutine(PlayMusic(lastMusic, lastPitch));
            if (lastAmbience > -1)
                PlayAmbience(lastAmbience);

            if (checkpoint > -1)
                ply.pMovement.canMove = true;
        }
        else
        {
            // Fade in black screen
            yield return FadeInScreen();
            //ply.pMovement.canMove = true;
        }
    }

    IEnumerator FadeInScreen()
    {
        while (screenBlackout.color.a > 0)
        {
            screenBlackout.color = new Color(0, 0, 0, screenBlackout.color.a - 0.075f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }
        screenBlackout.rectTransform.anchoredPosition = new Vector2(0, -3000);

    }

    // Update is called once per frame
    void Update()
    {
        UpdateHUD();

        if (ply.pResources.health > 0)
        {
            /*
            // Open mech menu
            if (Input.GetKeyDown(KeyCode.Tab) && !dialogSettings.isPrintingDialog)
            {
                menuOpen = !menuOpen;
                if (menuOpen)
                {
                    Time.timeScale = 0;
                    actionMenuHolder.anchoredPosition = Vector2.zero;
                }
                else
                {
                    Time.timeScale = 1;
                    actionMenuHolder.anchoredPosition = new Vector2(0, -160);
                    SwitchMenuScreen(4);
                }
            }*/

            // Open pause menu
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                gamePaused = !gamePaused;
                if (gamePaused)
                    Time.timeScale = 0;
                else
                    Time.timeScale = 1;
            }

            // TEMP
            if (Input.GetKeyDown(KeyCode.Alpha1))
                StartCoroutine(DisplayDialog(dialogSettings.JSONSource, "bones_area"));
        }

        // Game over if health < 0
        if (ply.pResources.health <= 0 && !gameOverInProgress)
        {
            gameOverInProgress = true;
            StartCoroutine(GameOverSequence());
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

    public void QuitToTitle()
    {
        Application.LoadLevel(1);
    }

    public void RestartFromCheckpoint()
    {
        PlayerPrefs.SetInt("FLOE_LAST_CHECKPOINT", checkpointRefs.currentCheckpoint);
        PlayerPrefs.Save();
        SceneManager.LoadScene(2);
    }

    private void FixedUpdate()
    {
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
        depthText.text = Mathf.Clamp(-Mathf.RoundToInt((ply.transform.position.y - 35) * depthMultiplier), 0, lowestDepth * depthMultiplier).ToString() + " m";
        depthArrow.anchoredPosition = new Vector2(4, Mathf.Round(80 - (Mathf.Clamp(Mathf.RoundToInt(-ply.transform.position.y), 0, lowestDepth) / lowestDepth) * 150));

        UpdateHarpoonNumber();
        //UpdateActionMenuNumbers();

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

    void UpdateActionMenuNumbers()
    {
        harpoonNumberText.text = ply.pResources.harpoons.ToString();
        metalNumberText.text = ply.pResources.metal.ToString();
        depthChargeNumberText.text = ply.pResources.depthCharges.ToString();
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

    public void SwitchMenuScreen(int screen)
    {
        // 0 = Synthesis
        // 1 = Talk
        // 2 = Map
        // 3 = None
        for (int i = 0; i < 3; i++)
        {
            if (i != screen)
                actionMenuCategories[i].anchoredPosition = new Vector2(17, -250);
            else
                actionMenuCategories[i].anchoredPosition = new Vector2(17, -48);
        }
    }

    public void CraftItem(int item)
    {
        if (ply.pResources.metal <= 0)
            return;

        // 0: Repair ship
        // 1: Craft harpoon
        // 2: Craft depth charge
        PlaySFX(sfx.generalSounds[0]);
        switch (item)
        {
            case (0):
                ply.pResources.health = 8;
                break;
            case (1):
                if (ply.pResources.harpoons < 99)
                    ply.pResources.harpoons++;
                break;
            case (2):
                ply.pResources.depthCharges++;
                break;
        }
    }

    public void SpawnSmoochMark()
    {
        GameObject g = Instantiate(prefabRefs.smoochMark, cam);
        float randX = Random.Range(-3, 3f);
        g.transform.localPosition = new Vector3(3 * Mathf.Sign(randX) + randX, Random.Range(-4, 4f), 1);
        g.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-15f, 30f));
    }

    public void SwitchActiveToolHUD()
    {
        /*if (ply.pAbilities.activeAbility == 0)
            currentToolIcon.sprite = spriteRefs.numbers[10];
        else
            currentToolIcon.sprite = spriteRefs.numbers[11];*/
    }

    public void UpdateHarpoonNumber()
    {
        /*if (ply.pAbilities.activeAbility == 0)
        {
            int ones = ply.pResources.harpoons % 10;
            int tens = ply.pResources.harpoons / 10;
            numberIconOnes.sprite = spriteRefs.numbers[ones];
            numberIconTens.sprite = spriteRefs.numbers[tens];
        }
        else
        {*/
        //numberIconOnes.sprite = spriteRefs.numbers[12];
        //numberIconTens.sprite = spriteRefs.numbers[12];
        //}
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
        dialogSettings.isPrintingDialog = true;
        actionMenuStoredTimeScale = Time.timeScale;
        Time.timeScale = 0;
        bool harpoonPlaying = harpoonLoopingAudio.isPlaying;
        harpoonLoopingAudio.Stop();


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
            harpoonLoopingAudio.Play();
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
        bool harpoonPlaying = harpoonLoopingAudio.isPlaying;
        harpoonLoopingAudio.Stop();

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
