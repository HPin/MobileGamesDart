using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/**
 * Contains Button Actions
 */
public class MainMenu : MonoBehaviour
{
    public TMP_Dropdown pointsDropdown;
    
    void Update()
    {
 
        switch (this.pointsDropdown.value)
        {
            case 0:
                PlayerPrefs.SetInt("StartPoints", 301);
                break;
            case 1:
                PlayerPrefs.SetInt("StartPoints", 501);
                break;
            case 2:
                PlayerPrefs.SetInt("StartPoints", 701);
                break;
            default:
                PlayerPrefs.SetInt("StartPoints", 501);
                break;
        }

    }

    public void ToggleChanged(bool isActive)
    {
        if (isActive)
        {
            PlayerPrefs.SetInt("UseDoubleOut", 1);
        }
        else
        {
            PlayerPrefs.SetInt("UseDoubleOut", 0);
        }
    }

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
