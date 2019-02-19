using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/**
 * Lets the user go back to the main menu
 */
public class GameOverMenu : MonoBehaviour
{

    public TextMeshProUGUI winnerIndicatorLabel;

    void Update()
    {
        this.winnerIndicatorLabel.text = PlayerPrefs.GetString("Winner", "Game Over");
    }

    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 2);
    }
}
