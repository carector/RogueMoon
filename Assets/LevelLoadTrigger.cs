using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoadTrigger : MonoBehaviour
{
    public int levelBuildIndex;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            LoadLevel();
            this.enabled = false;
        }
    }

    public void LoadLevel()
    {
        SceneManager.LoadSceneAsync(levelBuildIndex, LoadSceneMode.Additive);
    }
}
