using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsManager : MonoBehaviour
{
    // Key for saving the name of the previous scene in PlayerPrefs
    private const string PREVIOUS_SCENE_KEY = "PreviousScene";

    // The name of the main menu scene
    private const string MAIN_MENU_SCENE_NAME = "MainMenu";

    void Start()
    {
        // Display the cursor in the options screen
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Handler for the "Return to Game" button.
    /// Loads the previous scene stored in PlayerPrefs.
    /// </summary>
    public void OnResumeClicked()
    {
        // Retrieve the saved scene name. If not found, default to the main menu.
        string previousSceneName = PlayerPrefs.GetString(PREVIOUS_SCENE_KEY, MAIN_MENU_SCENE_NAME);

        // 1. Restore the time scale (unpause)
        Time.timeScale = 1f;

        Debug.Log("Returning to previous scene: " + previousSceneName);
        SceneManager.LoadScene(previousSceneName);
    }

    /// <summary>
    /// Handler for the "Return to Main Menu" button.
    /// </summary>
    public void OnReturnToMainMenuClicked()
    {
        // 1. Restore the time scale (Crucial for the main menu scene)
        Time.timeScale = 1f;

        Debug.Log("Returning to Main Menu.");
        SceneManager.LoadScene(MAIN_MENU_SCENE_NAME);
    }
}