using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using io.newgrounds;
using io.newgrounds.components;
using io.newgrounds.components.Medal;
using io.newgrounds.objects;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class NewgroundsUtility : MonoBehaviour
{
    public Sprite[] medalIcons;
    core ngCore;

    Animator medalDisplayerAnimator;
    TextMeshProUGUI medalName;
    TextMeshProUGUI medalValue;
    Image medalIcon;

    struct MedalData
    {
        public string name;
        public string value;
        public int id;

        public MedalData(string _name, string _value, int _id)
        {
            name = _name;
            value = _value;
            id = _id;
        }
    }

    List<MedalData> medalQueue; // If medal unlock popup is already being displayed, wait until it finishes to show the next one
    Dictionary<int, bool> unlockedMedals;

    bool displayingMedalPopup;
    

    // Start is called before the first frame update
    void Start()
    {
        ngCore = FindObjectOfType<core>();
        medalQueue = new List<MedalData>();
        unlockedMedals = new Dictionary<int, bool>();

        medalDisplayerAnimator = GameObject.Find("MedalBackground").GetComponent<Animator>();
        medalIcon = medalDisplayerAnimator.transform.GetChild(0).GetComponent<Image>();
        medalName = medalDisplayerAnimator.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        medalValue = medalDisplayerAnimator.transform.GetChild(2).GetComponent<TextMeshProUGUI>();

        // Get list of unlocked medals
        var medalList = new getList();
        medalList.callWith(ngCore, OnGetMedalList);
    }

    public void UnlockMedal(int id)
    {
        if (unlockedMedals[id])
            return;

        unlock medal_unlock = new unlock();
        medal_unlock.id = id;
        unlockedMedals[id] = true;
        medal_unlock.callWith(ngCore, MedalUnlockedCallback);
    }

    public void PostScore(int id, int value)
    {
        io.newgrounds.components.ScoreBoard.postScore scoreToPost = new io.newgrounds.components.ScoreBoard.postScore();
        scoreToPost.id = id;
        scoreToPost.value = value;
        scoreToPost.callWith(ngCore);
    }

    #region Plumbing
    void OnGetMedalList(io.newgrounds.results.Medal.getList result)
    {
        if (!result.success)
            return;
        List<medal> medals = result.medals.Cast<medal>().ToList();
        foreach (medal m in medals)
            unlockedMedals.Add(m.id, m.unlocked);
    }

    void MedalUnlockedCallback(io.newgrounds.results.Medal.unlock result)
    {
        if (!result.success)
            return;

        string medalValue = result.medal.value.ToString();
        string medalName = result.medal.name;
        int medalId = result.medal.id;
        Debug.LogError(result.medal.icon);
        MedalData md = new MedalData(medalName, medalValue, medalId);

        if (displayingMedalPopup)
            medalQueue.Add(md);
        else
            StartCoroutine(DisplayMedalCoroutine(md));
    }

    IEnumerator SetIconFromURL(string url)
    {
        WWW www = new WWW(url);
        yield return www;
        //medalIcon.texture = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0, 0));
    }

    Sprite GetSpriteFromMedalID(int id)
    {
        // Would much rather not have to do this but
        // Unity can't read webp files

        // Varies per project
        switch(id)
        {
            case 68300:
                return medalIcons[0];
            case 68301:
                return medalIcons[1];
            case 68302:
                return medalIcons[2];
            case 68303:
                return medalIcons[3];
            case 68304:
                return medalIcons[4];
        }

        return null;
    }

    IEnumerator DisplayMedalCoroutine(MedalData md)
    {
        displayingMedalPopup = true;
        medalName.text = md.name;
        medalValue.text = md.value + "pts";
        medalIcon.sprite = GetSpriteFromMedalID(md.id);
        //yield return SetIconFromURL(md.icon);
        medalDisplayerAnimator.Play("MedalDisplayer_SlideIn");
        yield return new WaitForSeconds(6);
        medalDisplayerAnimator.Play("MedalDisplayer_SlideOut");
        yield return new WaitForSeconds(0.65f);

        if (medalQueue.Count > 0)
        {
            StartCoroutine(DisplayMedalCoroutine(medalQueue[0]));
            medalQueue.RemoveAt(0);
        }
        else
            displayingMedalPopup = false;
    }
    #endregion
}