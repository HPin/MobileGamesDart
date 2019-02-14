using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/**
 * Contains Button Actions
 */
public class MainMenu : MonoBehaviour
{
    /**
     * Start Button Action
     */
    public void StartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    /**
     * Quit Button Action
     */
    public void QuitGame()
    {
        Application.Quit();
    }
}
