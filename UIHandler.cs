using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIHandler : MonoBehaviour
{
    public GameObject[] objects;
    public GameObject pauseMenu;
    public bool ads;
    public bool startingScreen;

    private void Awake()
    {
       /* GameDistribution.OnRewardGame += revive;
        GameDistribution.OnPauseGame += Gdpause;
        GameDistribution.OnResumeGame += GdResume;*/
    }
    void Start()
    {
       /* GameDistribution.Instance.PreloadRewardedAd();*/
        // Deactivate pauseMenu and homeMenu initially
        // pauseMenu.SetActive(false);

    }
    public void RewardReviveAd()
    {
        /*GameDistribution.Instance.ShowRewardedAd()*/;
    }
    public void revive()
    {
        //objects[1].SetActive(false);
        /*ScoreManager.Instance.saveKills();*/
        SceneManager.LoadScene(1);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (!Cursor.visible)
            {
                startingScreen = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                startingScreen = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        bool isAnyObjectActive = false;
        foreach (GameObject obj in objects)
        {
            if (obj.activeSelf)
            {
                isAnyObjectActive = true;
                break;
            }
        }

        if (isAnyObjectActive || !Application.isFocused || ads)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            PauseGame();
            muteGame();
        }
        else
        {
            if (startingScreen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            ResumeGame();
            UnmuteGame();
        }
    }




    // Function to pause the game
    public void PauseGame()
    {

        Time.timeScale = 0f;    // Pause the game


    }
    public void Gdpause()
    {
        ads = true;
        PauseGame();
        muteGame();
    }
    public void GdResume()
    {
        ads = false;
        ResumeGame();
        UnmuteGame();
    }
    // Function to resume the game
    public void ResumeGame()
    {

        Time.timeScale = 1f;    // Resume the game

    }
    public void muteGame()
    {
        AudioListener.pause = true;
        AudioListener.volume = 0f;
    }
    public void UnmuteGame()
    {
        AudioListener.pause = false;
        AudioListener.volume = 1f;
    }

    public void ShowAds()
    {
        /*if (GameDistribution.Instance != null)
        {
            GameDistribution.Instance.ShowAd();

        }*/
    }
    // Function to show the pause menu
    void ShowPauseMenu()
    {
        pauseMenu.SetActive(true);
    }

    public void loadScene(string scenename)
    {
        SceneManager.LoadScene(scenename);
    }
}
