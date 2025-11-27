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
    public float levelTimeLimit = 240.0f; // 4분 (초 단위가 불편하면 이전 답변의 분 단위 코드를 쓰셔도 됩니다)
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
    public AudioClip heartDropClip;

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
        StartCoroutine(EnsureTimeRuns());

        currentTimer = levelTimeLimit; // 타이머 리셋

        // (★★★★★ 1. 핵심 수정: 게임 씬에 들어오면 노래 다시 재생 ★★★★★)
        // "MainMenu"가 아닌 씬(게임 씬)이 로드되었을 때, 노래가 꺼져있다면 다시 켭니다.
        if (scene.name != "MainMenu" && audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }

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

    private IEnumerator EnsureTimeRuns()
    {
        yield return null;
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void LinkGameOverButtons()
    {
        if (gameOverPanel == null) return;

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
            mainMenuButton.onClick.AddListener(LoadMainMenu); // GoToMainMenu 대신 통합된 LoadMainMenu 사용
        }
    }

    // (★★★★★ 2. 핵심 수정: 게임 데이터를 초기화하는 헬퍼 함수 ★★★★★)
    private void ResetGameData()
    {
        playerLives = maxPlayerLives;
        currentLevelLength = 60;
        currentLevelWidth = 40;
        currentLevel = 1; // 레벨 1로 초기화
        currentTimer = levelTimeLimit;

        greenChance = 0.9f;
        blueChance = 0.1f;
        purpleChance = 0.0f;
        redChance = 0.0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // 게임 데이터 초기화 함수 사용
        ResetGameData();

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

    // (★★★★★ 3. 핵심 수정: 메인 메뉴로 나갈 때 초기화 및 노래 끄기 ★★★★★)
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;

        // 1. 게임 데이터(레벨 등)를 싹 초기화합니다.
        ResetGameData();

        // 2. 노래를 끕니다.
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // 메인 메뉴 씬 로드
        SceneManager.LoadScene("MainMenu");
    }

    public void GoToMainMenu()
    {
        // LoadMainMenu와 기능이 같으므로 통합
        LoadMainMenu();
    }

    // ... (기타 함수들: PlayerTookDamage, PlayerInstantDeath, HandleGameOver 등 기존 유지) ...

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

        StartCoroutine(SpawnFlamesAfterDelay(new Vector3(pos.x, 0.5f, pos.z)));

        Destroy(currentPlayer);
        currentPlayer = null;
        Debug.Log("GAME OVER");
    }

    private IEnumerator SpawnFlamesAfterDelay(Vector3 position)
    {
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

        // (★★★★★ 1. 핵심 추가: 최고 레벨 저장 로직 ★★★★★)
        // 현재 레벨이 저장된 'HighestLevel'보다 높으면 덮어씁니다.
        if (currentLevel > PlayerPrefs.GetInt("HighestLevel", 1))
        {
            PlayerPrefs.SetInt("HighestLevel", currentLevel);
            PlayerPrefs.Save(); // 저장 확정
        }

        // (이하 기존 난이도 조절 로직)
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

    public void PlayerHeal(int amount)
    {
        if (playerLives <= 0 || playerLives >= maxPlayerLives) return;

        playerLives += amount;
        if (playerLives > maxPlayerLives) playerLives = maxPlayerLives;

        UpdateHeartsUI();
        Debug.Log("체력 회복! 현재 체력: " + playerLives);
    }
}