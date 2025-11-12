using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Required for TextMeshPro

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text highestLevelText; // TextMeshPro Component
    public GameObject optionsPanel;     // Optional settings panel
    
    [Header("Scene Settings")]
    [Tooltip("The name of the scene to load when 'Game Start' is clicked.")]
    
    public string firstLevelSceneName = "SampleScene";

    // Key for saving the player's highest achieved level
    private const string HIGHEST_LEVEL_KEY = "HighestLevel";

    void Start()
    {
        // 1. Load the highest level and update the UI
        LoadHighestLevel();
        
        // 2. Ensure the options panel is hidden at start
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
        
        // 3. Display and unlock the cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Optional: Play menu BGM here
    }

    /// <summary>
    /// Loads the highest achieved level from PlayerPrefs and updates the UI.
    /// </summary>
    private void LoadHighestLevel()
    {
        // Get the value for "HighestLevel" key (defaulting to 0)
        int highestLevel = PlayerPrefs.GetInt(HIGHEST_LEVEL_KEY, 0); 

        if (highestLevelText != null)
        {
            // Update the UI Text element
            highestLevelText.text = "Highest Level: " + highestLevel.ToString();
        }
    }

    // --- Button Event Handlers ---

    //// <summary>
    /// 「Game Start」ボタンがクリックされた時に呼ばれます。
    /// </summary>
    public void OnStartGameClicked()
    {
        Debug.Log("Game Start!");
        // load main game
        
        SceneManager.LoadScene(firstLevelSceneName);
    }
    /// <summary>
    /// Called when the "Option" button is clicked.
    /// </summary>
    public void OnOptionsClicked()
    {
        // Toggle the visibility of the options panel
        if (optionsPanel != null)
        {
            bool isActive = optionsPanel.activeSelf;
            optionsPanel.SetActive(!isActive);
        }
        else
        {
            Debug.Log("Option Panel clicked. Implement the display logic here.");
        }
    }

    /// <summary>
    /// A static method for other scripts (like GameManager) to update the high score.
    /// </summary>
    public static void UpdateHighestLevel(int newLevel)
    {
        int currentHighest = PlayerPrefs.GetInt(HIGHEST_LEVEL_KEY, 0);
        if (newLevel > currentHighest)
        {
            PlayerPrefs.SetInt(HIGHEST_LEVEL_KEY, newLevel);
            PlayerPrefs.Save(); // Crucial: saves the data to disk
        }
    }
}