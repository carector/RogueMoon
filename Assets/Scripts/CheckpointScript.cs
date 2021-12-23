using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointScript : MonoBehaviour
{
    public int checkpointIndex;
    GameManager gm;
    PlayerController ply;

    // Start is called before the first frame update
    void Start()
    {
        gm = FindObjectOfType<GameManager>();
        ply = FindObjectOfType<PlayerController>();

        if (PlayerPrefs.GetInt("FLOE_LAST_CHECKPOINT") > checkpointIndex)
            this.enabled = false;
    }

    public void ActivateCheckpoint()
    {
        print("Checkpoint "+checkpointIndex+" activated");
        gm.checkpointRefs.currentCheckpoint = checkpointIndex;
        PlayerPrefs.SetInt("FLOE_LAST_CHECKPOINT", gm.checkpointRefs.currentCheckpoint);
        PlayerPrefs.SetInt("FLOE_LAST_MUSIC", gm.checkpointRefs.lastMusicIndex);
        PlayerPrefs.SetFloat("FLOE_LAST_MUSIC_PITCH", gm.checkpointRefs.lastMusicPitch);
        PlayerPrefs.SetInt("FLOE_LAST_AMBIENCE", gm.checkpointRefs.lastAmbienceIndex);
        PlayerPrefs.Save();
        this.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
            ActivateCheckpoint();
    }
}