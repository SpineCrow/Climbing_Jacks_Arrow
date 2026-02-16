using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Simple main menu controller. Wire PlayGame() and QuitGame()
// to UI Button onClick events in the Inspector.
public class MainMenu : MonoBehaviour
{
    // Loads the next scene in the build order (the first gameplay level).
    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Exits the application. Has no effect in the editor.
    public void QuitGame()
    {
        Application.Quit();
    }
}
