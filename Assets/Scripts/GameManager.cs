using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static int currentLevelLength = 60;
    public static int currentLevelWidth = 40;
    public static int currentLevel = 1;
    public static Vector3 currentSpawnPoint;

    public GameObject playerPrefab;
    private GameObject currentPlayer;
    private CameraFollow mainCameraScript;
    private AudioSource audioSource;

    // 로딩 중 입력 무시 카운터
    private int loadFrames = 0;

    [Header("Spawn Settings")]
    public float playerSpawnZ = 20.0f;
    public float playerSpawnY = 3.0f;

    [Header("Player Stats")]
    public int playerLives = 5;
    public int maxPlayerLives = 5;

    [Header("Time Settings")]
    public float levelTimeLimit = 240.0f; // 4분
    private float currentTimer;

    [Header("UI Elements")]
    public Image[] hearts;
    public GameObject gameOverPanel;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI timerText;
    public GameObject pausePanel;
    private bool isPaused = false;

    [Header("VFX")]
    public GameObject smallExplosionPrefab;
    public GameObject bigExplosionPrefab;
    public GameObject mediumFlamesPrefab;

    [Header("Audio Clips")]
    public AudioClip hitSoundClip;
    public AudioClip finalExplosionClip;
    public AudioClip portalSpawnSound;
    public AudioClip bgmClip;
    public AudioClip heartDropClip; // 하트 드랍 사운드

    [Header("Block Probabilities")]
    public static float greenChance = 0.9f;
    public static float blueChance = 0.1f;
    public static float purpleChance = 0.0f;
    public static float redChance = 0.0f;

    public bool IsGameOver
    {
        get { return gameOverPanel != null && gameOverPanel.activeInHierarchy; }
    }

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
        if (audioSource == null) { Debug.LogError("GameManager에 AudioSource 컴포넌트를 추가해야 합니다."); }

        if (bgmClip != null)
        {
            audioSource.clip = bgmClip;
            audioSource.loop = true;
            audioSource.Play();
        }
        // UI 초기화는 OnSceneLoaded에서 처리
    }

    void Update()
    {
        // 로딩 중 입력 무시
        if (loadFrames > 0)
        {
            loadFrames--;
            return;
        }

        // ESC 키 감지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!IsGameOver)
            {
                TogglePause();
            }
        }

        // 타이머 로직
        if (!isPaused && !IsGameOver && playerLives > 0)
        {
            currentTimer -= Time.deltaTime;

            if (currentTimer <= 0)
            {
                currentTimer = 0;
                PlayerInstantDeath();
            }

            UpdateTimerUI();
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
        // 초기화 시 시간 정지 해제 보장
        isPaused = false;
        loadFrames = 2;
        StartCoroutine(EnsureTimeRuns()); // 지연된 시간 복구 실행

        currentTimer = levelTimeLimit; // 타이머 리셋

        // 1. 카메라
        if (Camera.main != null)
        {
            mainCameraScript = Camera.main.GetComponent<CameraFollow>();
        }

        // 2. 스폰 지점
        LevelGenerator levelGen = FindObjectOfType<LevelGenerator>();
        float tileSize = (levelGen != null) ? levelGen.tileSize : 1.0f;
        float offset = tileSize / 2f;
        float spawnX = offset;
        currentSpawnPoint = new Vector3(spawnX, playerSpawnY, playerSpawnZ);

        // 3. 하트 UI
        GameObject heartContainer = GameObject.Find("HeartPanel");
        if (heartContainer != null)
        {
            hearts = heartContainer.GetComponentsInChildren<Image>();
        }

        // 4. GameOverPanel
        GameObject panel = GameObject.Find("GameOverPanel");
        if (panel != null)
        {
            gameOverPanel = panel;
            gameOverPanel.SetActive(false);
            LinkGameOverButtons();
        }

        // 5. 레벨 텍스트
        GameObject levelTextObject = GameObject.Find("LevelText");
        if (levelTextObject != null)
        {
            levelText = levelTextObject.GetComponent<TextMeshProUGUI>();
            UpdateLevelUI();
        }

        // 6. 타이머 텍스트
        GameObject timerTextObject = GameObject.Find("TimerText");
        if (timerTextObject != null)
        {
            timerText = timerTextObject.GetComponent<TextMeshProUGUI>();
            UpdateTimerUI();
        }

        // 7. 일시정지 패널
        GameObject pausePanelObject = GameObject.Find("PausePanel");
        if (pausePanelObject != null)
        {
            pausePanel = pausePanelObject;

            Transform resumeBtn = pausePanel.transform.Find("ResumeButton");
            if (resumeBtn) resumeBtn.GetComponent<Button>().onClick.AddListener(TogglePause);

            Transform menuBtn = pausePanel.transform.Find("MainMenuButton");
            if (menuBtn) menuBtn.GetComponent<Button>().onClick.AddListener(LoadMainMenu);

            pausePanel.SetActive(false);
        }

        // 8. 플레이어 스폰 & UI 갱신
        SpawnPlayer();
        UpdateHeartsUI();
    }

    // (지연된 시간 복구 코루틴 - 시작 시 멈춤 방지)
    private IEnumerator EnsureTimeRuns()
    {
        yield return null; // 1프레임 대기
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void LinkGameOverButtons()
    {
        if (gameOverPanel == null) return;

        // "Restart" 라는 이름의 버튼을 찾음
        Button restartButton = gameOverPanel.transform.Find("Restart")?.GetComponent<Button>();
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }

        Button mainMenuButton = gameOverPanel.transform.Find("MainMenuButton")?.GetComponent<Button>();
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(LoadMainMenu);
        }
    }

    public void PlayerTookDamage(int damage)
    {
        if (currentPlayer == null || playerLives <= 0) return;
        playerLives -= damage;
        if (playerLives < 0) playerLives = 0;

        UpdateHeartsUI();

        if (hitSoundClip != null && audioSource != null) { audioSource.PlayOneShot(hitSoundClip); }
        if (smallExplosionPrefab != null)
        {
            Vector3 playerXZPos = new Vector3(currentPlayer.transform.position.x, 0, currentPlayer.transform.position.z);
            Vector3 explosionPos = new Vector3(playerXZPos.x, 3f, playerXZPos.z);
            Instantiate(smallExplosionPrefab, explosionPos, Quaternion.identity);
        }

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
        if (currentPlayer == null) return;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            LinkGameOverButtons();
        }

        Vector3 pos = currentPlayer.transform.position;
        if (finalExplosionClip != null && audioSource != null) { audioSource.PlayOneShot(finalExplosionClip); }
        if (bigExplosionPrefab != null) { Instantiate(bigExplosionPrefab, pos, Quaternion.identity); }

        // 불길 생성 (시간 정지여도 나오게 수정됨)
        StartCoroutine(SpawnFlamesAfterDelay(new Vector3(pos.x, 0.5f, pos.z)));

        Destroy(currentPlayer);
        currentPlayer = null;
        Debug.Log("GAME OVER");
    }

    private IEnumerator SpawnFlamesAfterDelay(Vector3 position)
    {
        // 시간 무시하고 0.5초 대기 (WaitForSecondsRealtime 사용)
        yield return new WaitForSecondsRealtime(0.5f);
        if (mediumFlamesPrefab != null) { Instantiate(mediumFlamesPrefab, position, Quaternion.identity); }
    }

    void UpdateHeartsUI()
    {
        if (hearts == null) return;
        for (int i = 0; i < maxPlayerLives; i++)
        {
            if (i < hearts.Length && hearts[i] != null)
                hearts[i].gameObject.SetActive(i < playerLives);
        }
    }

    void UpdateLevelUI()
    {
        if (levelText != null) levelText.text = "LEVEL " + currentLevel;
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTimer / 60F);
            int seconds = Mathf.FloorToInt(currentTimer % 60F);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (currentTimer <= 10f) timerText.color = Color.red;
            else timerText.color = Color.white;
        }
    }

    public void PlayPortalSound()
    {
        if (audioSource != null && portalSpawnSound != null)
            audioSource.PlayOneShot(portalSpawnSound);
    }

    public void PlayHeartDropSound()
    {
        if (audioSource != null && heartDropClip != null)
            audioSource.PlayOneShot(heartDropClip);
    }

    public void GoToNextLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;
        currentLevelLength++;
        currentLevel++;

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
        if (playerPrefab != null)
        {
            currentPlayer = Instantiate(playerPrefab, currentSpawnPoint, Quaternion.identity);
            if (mainCameraScript != null) mainCameraScript.InitializeTarget(currentPlayer.transform);
            else if (Camera.main != null)
            {
                mainCameraScript = Camera.main.GetComponent<CameraFollow>();
                if (mainCameraScript != null) mainCameraScript.InitializeTarget(currentPlayer.transform);
            }
        }
        else { Debug.LogError("Player Prefab이 없습니다!"); }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        playerLives = maxPlayerLives;
        currentLevelLength = 60;
        currentLevelWidth = 40;
        currentLevel = 1;
        currentTimer = levelTimeLimit;

        greenChance = 0.9f; blueChance = 0.1f; purpleChance = 0.0f; redChance = 0.0f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);
        }
        else
        {
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
        }
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("MainMenu");
    }

    // (사라졌던 GoToMainMenu 복구)
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("MainMenu");
    }

    public void PlayerHeal(int amount)
    {
        if (playerLives <= 0 || playerLives >= maxPlayerLives) return;

        playerLives += amount;
        if (playerLives > maxPlayerLives) playerLives = maxPlayerLives;

        UpdateHeartsUI();
        Debug.Log("체력 회복! 현재 체력: " + playerLives);
    }
}