using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    public GameObject pauseMenuUI;

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Resume()
    {
        this.pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;    // set time back to normal
        GameIsPaused = false;
    }

    public void Pause()
    {
        this.pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;    // stop time for the game
        GameIsPaused = true;
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;    // set time back to normal
        PlayerPrefs.SetInt("UseDoubleOut", 1);  // reset value to match the toggle
        SceneManager.LoadScene("Menu");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
