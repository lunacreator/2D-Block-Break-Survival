using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static int currentLevelLength = 60;
    public static int currentLevelWidth = 40;
    public static Vector3 currentSpawnPoint;

    public GameObject playerPrefab;
    private GameObject currentPlayer;
    private CameraFollow mainCameraScript;
    private AudioSource audioSource;
    private float elapsedTime = 0f;          
    private const float timeLimit = 300f;    

    [Header("Spawn Settings")]
    public float playerSpawnZ = 20.0f;
    public float playerSpawnY = 3.0f;

    [Header("Player Stats")]
    public int playerLives = 5;
    public int maxPlayerLives = 5;

    [Header("UI Elements")]
    public Image[] hearts;
    public GameObject gameOverPanel;
    public TextMeshProUGUI timeText;        

    [Header("VFX")]
    public GameObject smallExplosionPrefab;
    public GameObject bigExplosionPrefab;
    public GameObject mediumFlamesPrefab;

    [Header("Audio Clips")]
    public AudioClip hitSoundClip;
    public AudioClip finalExplosionClip;
    public AudioClip portalSpawnSound;
    public AudioClip bgmClip;

    [Header("Block Probabilities")]
    public static float greenChance = 0.9f;
    public static float blueChance = 0.1f;
    public static float purpleChance = 0.0f;
    public static float redChance = 0.0f;

    [Header("Scene Management")]
    public string mainMenuSceneName = "MainMenu";
    // NOTE: isPaused/pausePanel removed as per user request to exclude pause functionality

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) { Debug.LogError("GameManager needs an AudioSource component."); }

        if (bgmClip != null)
        {
            audioSource.clip = bgmClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void Update()
    {
        // Only advance time if TimeScale is > 0 (game is running, not game over)
        if (Time.timeScale > 0f) 
        {
            elapsedTime += Time.deltaTime;
            UpdateTimeDisplay();
            TimerLimitCheck(); 
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Find CameraFollow script
        if (Camera.main != null)
        {
            mainCameraScript = Camera.main.GetComponent<CameraFollow>();
        }

        // 2. Calculate Level bounds
        LevelGenerator levelGen = FindObjectOfType<LevelGenerator>();
        float tileSize = (levelGen != null) ? levelGen.tileSize : 1.0f;
        float offset = tileSize / 2f;
        float spawnX = offset;
        currentSpawnPoint = new Vector3(spawnX, playerSpawnY, playerSpawnZ);

        // 3. Find Heart UI container
        GameObject heartContainer = GameObject.Find("HeartPanel");
        if (heartContainer != null)
        {
            hearts = heartContainer.GetComponentsInChildren<Image>();
        }
        else
        {
            Debug.LogError("HeartPanel was not found! Please name the UI heart parent 'HeartPanel'.");
        }

        // 4. Initialize GameOverPanel and register button events
        GameObject panel = GameObject.Find("GameOverPanel");
        if (panel != null)
        {
            gameOverPanel = panel;
            gameOverPanel.SetActive(false); 

            Transform restartButtonChild = panel.transform.Find("RestartButton");
            Transform mainMenuButtonChild = panel.transform.Find("MainMenuButton"); // ★ ADDED: Find MainMenuButton

            // Connect Restart Button
            if (restartButtonChild != null)
            {
                UnityEngine.UI.Button restartButton = restartButtonChild.GetComponent<UnityEngine.UI.Button>();

                if (restartButton != null)
                {
                    restartButton.onClick.RemoveAllListeners();
                    restartButton.onClick.AddListener(RestartGame);
                }
                else
                {
                    Debug.LogError("'RestartButton' GameObject requires a Button component!");
                }
            }
            else
            {
                Debug.LogError("Could not find 'RestartButton' GameObject under GameOverPanel!");
            }
            
            // ★ Connect Main Menu Button
            if (mainMenuButtonChild != null)
            {
                UnityEngine.UI.Button mainMenuButton = mainMenuButtonChild.GetComponent<UnityEngine.UI.Button>();

                if (mainMenuButton != null)
                {
                    mainMenuButton.onClick.RemoveAllListeners();
                    mainMenuButton.onClick.AddListener(LoadMainMenu); // ★ Hook up LoadMainMenu
                }
                else
                {
                    Debug.LogError("'MainMenuButton' GameObject requires a Button component!");
                }
            }
            else
            {
                Debug.LogWarning("Could not find 'MainMenuButton' GameObject under GameOverPanel."); 
            }
        }
        else
        {
            Debug.LogError("GameOverPanel was not found! Please name the UI panel 'GameOverPanel'.");
        }

        // 5. Spawn player
        SpawnPlayer();

        // 6. Update UI
        UpdateHeartsUI();
        UpdateTimeDisplay(); 
    }

    private void UpdateTimeDisplay()
    {
        if (timeText == null) return;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    private void TimerLimitCheck()
    {
        if (elapsedTime >= timeLimit)
        {
            Debug.Log("Time Limit Reached! Game Over.");
            HandleGameOver();
        }
    }

    // ★ ADDED: Load Main Menu Function
    public void LoadMainMenu()
    {
        // Reset level and game state variables
        playerLives = maxPlayerLives;
        currentLevelLength = 60;
        currentLevelWidth = 40;

        greenChance = 0.9f;
        blueChance = 0.1f;
        purpleChance = 0.0f;
        redChance = 0.0f;
        
        // Reset time tracker and ensure time is running before load
        elapsedTime = 0f; 
        Time.timeScale = 1f; 

        // Load the main menu scene using the variable
        SceneManager.LoadScene(mainMenuSceneName);
    }


    public void PlayerTookDamage(int damage)
    {
        if (currentPlayer == null || playerLives <= 0) return;
        playerLives -= damage;
        if (playerLives < 0) playerLives = 0;

        UpdateHeartsUI();

        Vector3 playerXZPos = new Vector3(currentPlayer.transform.position.x, 0, currentPlayer.transform.position.z);
        Vector3 explosionPos = new Vector3(playerXZPos.x, 3f, playerXZPos.z);
        if (hitSoundClip != null && audioSource != null) { audioSource.PlayOneShot(hitSoundClip); }
        if (smallExplosionPrefab != null) { Instantiate(smallExplosionPrefab, explosionPos, Quaternion.identity); }
        if (playerLives <= 0) { HandleGameOver(); }
    }

    public void PlayerInstantDeath()
    {
        if (currentPlayer == null) return;
        playerLives = 0;
        UpdateHeartsUI();
        HandleGameOver();
    }

    private void HandleGameOver()
    {
        if (currentPlayer == null && Time.timeScale == 0f) return; 

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        Time.timeScale = 0f; 

        if (currentPlayer != null)
        {
            Vector3 playerXZPos = new Vector3(currentPlayer.transform.position.x, 0, currentPlayer.transform.position.z);
            Vector3 explosionPos = new Vector3(playerXZPos.x, 3f, playerXZPos.z);
            if (finalExplosionClip != null && audioSource != null) { audioSource.PlayOneShot(finalExplosionClip); }
            if (bigExplosionPrefab != null) { Instantiate(bigExplosionPrefab, explosionPos, Quaternion.identity); }
            Destroy(currentPlayer);
            currentPlayer = null;
            Vector3 flamePos = new Vector3(playerXZPos.x, 0.5f, playerXZPos.z);
            StartCoroutine(SpawnFlamesAfterDelay(flamePos));
        }
        
        Debug.Log("GAME OVER");
    }

    private IEnumerator SpawnFlamesAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(0.5f);
        if (mediumFlamesPrefab != null) { Instantiate(mediumFlamesPrefab, position, Quaternion.identity); }
    }

    void UpdateHeartsUI()
    {
        if (hearts == null || hearts.Length == 0) { return; }
        for (int i = 0; i < maxPlayerLives; i++)
        {
            if (i < hearts.Length && hearts[i] != null)
            {
                if (i < playerLives) { hearts[i].gameObject.SetActive(true); }
                else { hearts[i].gameObject.SetActive(false); }
            }
        }
    }

    public void PlayPortalSound()
    {
        if (audioSource != null && portalSpawnSound != null)
        {
            audioSource.PlayOneShot(portalSpawnSound);
        }
    }

    public void GoToNextLevel()
    {
        currentLevelLength++;
        if (greenChance > 0.1f) { greenChance -= 0.1f; blueChance += 0.05f; if (blueChance > 0.4f) { purpleChance += 0.03f; redChance += 0.02f; } else { purpleChance += 0.05f; } } else if (blueChance > 0.1f) { blueChance -= 0.1f; purpleChance += 0.07f; redChance += 0.03f; } else { purpleChance -= 0.1f; redChance += 0.1f; }
        greenChance = Mathf.Clamp(greenChance, 0.0f, 1.0f); blueChance = Mathf.Clamp(blueChance, 0.0f, 1.0f); purpleChance = Mathf.Clamp(purpleChance, 0.0f, 1.0f); redChance = Mathf.Clamp(redChance, 0.0f, 1.0f);
        float total = greenChance + blueChance + purpleChance + redChance; greenChance /= total; blueChance /= total; purpleChance /= total; redChance /= total;
        BouncingBlaster[] allBullets = FindObjectsOfType<BouncingBlaster>();
        foreach (BouncingBlaster bullet in allBullets) { Destroy(bullet.gameObject); }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SpawnPlayer()
    {
        if (currentPlayer != null) { Destroy(currentPlayer); }
        if (playerPrefab != null) { currentPlayer = Instantiate(playerPrefab, currentSpawnPoint, Quaternion.identity); if (mainCameraScript != null) { mainCameraScript.InitializeTarget(currentPlayer.transform); } else if (Camera.main != null) { mainCameraScript = Camera.main.GetComponent<CameraFollow>(); if (mainCameraScript != null) { mainCameraScript.InitializeTarget(currentPlayer.transform); } } } else { Debug.LogError("Player Prefab is not assigned in GameManager!"); }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        // isPaused logic removed as per user request to exclude pause functionality

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    
        // Reset timer
        elapsedTime = 0f;
        UpdateTimeDisplay();

        playerLives = maxPlayerLives;
        currentLevelLength = 60;
        currentLevelWidth = 40;

        greenChance = 0.9f;
        blueChance = 0.1f;
        purpleChance = 0.0f;
        redChance = 0.0f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}