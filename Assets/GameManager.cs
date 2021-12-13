using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class GameSoundEffects
    {
        public AudioClip[] playerSounds;
        public AudioClip[] generalSounds;
        public AudioClip[] dialogVoices;
    }

    [System.Serializable]
    public class GameDialogSettings
    {
        public TextAsset JSONSource;
        public TextData cachedTextData;
        public bool isPrintingDialog;
        public Sprite[] numbers;
        public Sprite[] portraits;
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
        public class Sentence
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
    public GameDialogSettings interactions;

    public bool menuOpen;
    public bool gamePaused;

    AudioSource musicSource;
    AudioSource ambienceSource;

    AudioSource sfxSource;
    AudioSource stoppableSfxSource;
    AudioSource priorityStoppableSfxSource;
    AudioSource dialogAudio;

    Image healthbarFill;
    Image numberIconTens;
    Image numberIconOnes;
    Image currentToolIcon;

    RectTransform textboxHolder;
    RectTransform resourceMenuHolder;
    TextMeshProUGUI dialogText;
    Image leftPortrait;
    Image rightPortrait;
    Image toolbarFill;

    PlayerController ply;

    // used for when we unpause when the mech menu is already
    // open we don't fuck up our timescale
    float pauseStoredTimescale;

    // Start is called before the first frame update
    void Start()
    {
        sfxSource = transform.GetChild(0).GetComponent<AudioSource>();
        stoppableSfxSource = transform.GetChild(0).GetComponent<AudioSource>();
        musicSource = transform.GetChild(2).GetComponent<AudioSource>();
        ambienceSource = transform.GetChild(3).GetComponent<AudioSource>();
        dialogAudio = transform.GetChild(4).GetComponent<AudioSource>();

        Transform toolbar = GameObject.Find("ToolBar").transform;
        toolbarFill = toolbar.GetChild(0).GetComponent<Image>();
        numberIconTens = toolbar.GetChild(1).GetComponent<Image>();
        numberIconOnes = toolbar.GetChild(2).GetComponent<Image>();
        currentToolIcon = toolbar.GetChild(3).GetComponent<Image>();
        textboxHolder = GameObject.Find("TextboxHolder").GetComponent<RectTransform>();
        dialogText = textboxHolder.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
        leftPortrait = textboxHolder.GetChild(1).GetComponent<Image>();
        rightPortrait = textboxHolder.GetChild(2).GetComponent<Image>();
        resourceMenuHolder = GameObject.Find("ResourceMenu").GetComponent<RectTransform>();

        healthbarFill = GameObject.Find("HealthbarFill").GetComponent<Image>();

        ply = FindObjectOfType<PlayerController>();

        // Convert dialog from JSON file to nice, readable dialog
        interactions.cachedTextData = DeserializeDialog(interactions.JSONSource);

    }

    // Update is called once per frame
    void Update()
    {
        UpdateHUD();

        // TEMP TEMP TEMP
        if (Input.GetKeyDown(KeyCode.Alpha1) && !interactions.isPrintingDialog)
        {
            StartCoroutine(DisplayDialog(interactions.JSONSource, "tutorial_1"));
        }

        // Open mech menu
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            menuOpen = !menuOpen;
            if (menuOpen)
            {
                Time.timeScale = 0;
                resourceMenuHolder.anchoredPosition = Vector2.zero;
            }
            else
            {
                Time.timeScale = 1;
                resourceMenuHolder.anchoredPosition = new Vector2(0, -160);
            }
        }

        // Open pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gamePaused = !gamePaused;
            if (gamePaused)
                Time.timeScale = 0;
            else
                Time.timeScale = 1;
        }
    }

    private void FixedUpdate()
    {
        UpdateAttackBar();
    }

    void UpdateHUD()
    {
        // Update healthbar
        healthbarFill.rectTransform.sizeDelta = new Vector2(5 * ply.pResources.health, 6);
        healthbarFill.rectTransform.anchoredPosition = new Vector2(3 + 2.5f * ply.pResources.health, 0);

        UpdateHarpoonNumber();
    }

    public void UpdateAttackBar()
    {
        if (ply.pAbilities.activeAbility == 1)
        {
            toolbarFill.color = Color.white;
            Vector2 fillPos = Vector2.zero;
            Vector2 fillSize = Vector2.zero;

            // 14 units between edges
            if (!ply.pAbilities.attackDelayInProgress)
            {
                switch (ply.pAbilities.attackCharges)
                {
                    case 2:
                        fillPos = new Vector2(-31, -8);
                        fillSize = new Vector2(0, 16);
                        break;
                    case 1:
                        fillPos = new Vector2(-23.5f, -8);
                        fillSize = new Vector2(15, 16);
                        break;
                    case 0:
                        fillPos = new Vector2(-16f, -8);
                        fillSize = new Vector2(30, 16);
                        break;
                }
            }
            else
            {
                float rate = Time.fixedDeltaTime * 16;

                // Lerp towards left to recharge
                fillPos = toolbarFill.rectTransform.anchoredPosition + Vector2.left * rate;
                fillSize = toolbarFill.rectTransform.sizeDelta + Vector2.left * rate * 2;
            }

            toolbarFill.rectTransform.anchoredPosition = Vector2.Lerp(toolbarFill.rectTransform.anchoredPosition, fillPos, 0.5f);
            toolbarFill.rectTransform.sizeDelta = Vector2.Lerp(toolbarFill.rectTransform.sizeDelta, fillSize, 0.5f);

        }
        else
            toolbarFill.color = Color.clear;
    }

    public void SwitchActiveToolHUD()
    {
        if (ply.pAbilities.activeAbility == 0)
            currentToolIcon.sprite = interactions.numbers[10];
        else
            currentToolIcon.sprite = interactions.numbers[11];
    }

    public void UpdateHarpoonNumber()
    {
        if (ply.pAbilities.activeAbility == 0)
        {
            int ones = ply.pResources.harpoons % 10;
            int tens = ply.pResources.harpoons / 10;
            numberIconOnes.sprite = interactions.numbers[ones];
            numberIconTens.sprite = interactions.numbers[tens];
        }
        else
        {
            numberIconOnes.sprite = interactions.numbers[12];
            numberIconTens.sprite = interactions.numbers[12];
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.pitch = 1;
        sfxSource.PlayOneShot(clip);
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

    public IEnumerator DisplayDialog(TextAsset src, string id)
    {
        interactions.isPrintingDialog = true;

        // Get references
        TextData t = DeserializeDialog(src);
        TextData.Conversation c = t.GetConversationFromID(id);

        // Set initial portrait colors and sprites before moving textbox
        leftPortrait.color = new Color(0.5f, 0.5f, 0.5f, 1);
        rightPortrait.color = new Color(0.5f, 0.5f, 0.5f, 1);
        leftPortrait.sprite = interactions.portraits[c.initialPortraits[0]];
        rightPortrait.sprite = interactions.portraits[c.initialPortraits[1]];

        textboxHolder.anchoredPosition = new Vector2(0, -24f);

        // Dialog loop
        for (int i = 0; i < c.dialog.Length; i++)
        {
            // Sentence loop
            int dialogLength = c.dialog[i].sentences.Length;

            for (int j = 0; j < dialogLength; j++)
            {
                TextData.Sentence s = c.dialog[i].sentences[j];
                if (c.dialog[i].portrait == 0)
                {
                    leftPortrait.sprite = interactions.portraits[s.portraitIndex];
                    leftPortrait.color = Color.white;
                    rightPortrait.color = new Color(0.5f, 0.5f, 0.5f, 1);
                }
                else
                {
                    rightPortrait.sprite = interactions.portraits[s.portraitIndex];
                    rightPortrait.color = Color.white;
                    leftPortrait.color = new Color(0.5f, 0.5f, 0.5f, 1);
                }

                // Char loop
                int sentenceLength = s.text.Length;
                string sentence = InsertLineBreaks(s.text);

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
                dialogAudio.Stop();
                dialogAudio.PlayOneShot(sfx.dialogVoices[4]);

                // Wait for key press to continue dialog
                yield return WaitForKeyPress();
            }
        }

        interactions.isPrintingDialog = false;
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
        if (t.name.Equals(interactions.cachedTextData.filename))
        {
            print("TextData already cached");
            return interactions.cachedTextData;
        }

        interactions.cachedTextData = JsonConvert.DeserializeObject<TextData>(t.text);
        return interactions.cachedTextData;
    }

    IEnumerator WaitForKeyPress()
    {
        // update to new input system later
        while (!Input.GetKeyDown(KeyCode.E))
            yield return null;
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
