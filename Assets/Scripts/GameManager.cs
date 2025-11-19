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
    public AudioClip heartDropClip;

    private int loadFrames = 0;

    [Header("Spawn Settings")]
    public float playerSpawnZ = 20.0f;
    public float playerSpawnY = 3.0f;

    [Header("Player Stats")]
    public int playerLives = 5;
    public int maxPlayerLives = 5;

    [Header("Time Settings")]
    public float levelTimeLimit = 240.0f; // (★★★★★ 1. 제한 시간: 4분 = 240초 ★★★★★)
    private float currentTimer; // 현재 남은 시간

    [Header("UI Elements")]
    public Image[] hearts;
    public GameObject gameOverPanel;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI timerText; // (★★★★★ 2. 타이머 텍스트 변수 추가 ★★★★★)
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

    [Header("Block Probabilities")]
    public static float greenChance = 0.9f;
    public static float blueChance = 0.1f;
    public static float purpleChance = 0.0f;
    public static float redChance = 0.0f;

    // (게임 오버 상태 확인 속성)
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

        // UI 초기화는 OnSceneLoaded에서 통합 처리됨
    }

    void Update()
    {
        // 로딩 중 입력 무시
        if (loadFrames > 0)
        {
            loadFrames--;
            return;
        }

        // ESC 키 감지 (게임 오버가 아닐 때만)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!IsGameOver)
            {
                TogglePause();
            }
        }

        // (★★★★★ 3. 타이머 로직 추가 ★★★★★)
        // 일시정지 상태가 아니고, 게임 오버가 아니고, 플레이어가 살아있을 때만 시간 감소
        if (!isPaused && !IsGameOver && playerLives > 0)
        {
            currentTimer -= Time.deltaTime;

            // 시간이 다 되면 사망 처리
            if (currentTimer <= 0)
            {
                currentTimer = 0;
                PlayerInstantDeath();
            }

            // UI 업데이트
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
        isPaused = false;
        loadFrames = 2;
        StartCoroutine(EnsureTimeRuns());

        // (★★★★★ 4. 레벨 시작 시 타이머 리셋 ★★★★★)
        currentTimer = levelTimeLimit; // 240초로 초기화

        // 1. 카메라 스크립트 찾기
        if (Camera.main != null)
        {
            mainCameraScript = Camera.main.GetComponent<CameraFollow>();
        }

        // 2. 스폰 지점 계산
        LevelGenerator levelGen = FindObjectOfType<LevelGenerator>();
        float tileSize = (levelGen != null) ? levelGen.tileSize : 1.0f;
        float offset = tileSize / 2f;
        float spawnX = offset;
        currentSpawnPoint = new Vector3(spawnX, playerSpawnY, playerSpawnZ);

        // 3. UI 하트들 새로 찾기
        GameObject heartContainer = GameObject.Find("HeartPanel");
        if (heartContainer != null)
        {
            hearts = heartContainer.GetComponentsInChildren<Image>();
        }
        else { Debug.LogError("HeartPanel을 찾을 수 없습니다!"); }

        // 4. GameOverPanel 새로 찾기
        GameObject panel = GameObject.Find("GameOverPanel");
        if (panel != null)
        {
            gameOverPanel = panel;
            gameOverPanel.SetActive(false);
            LinkGameOverButtons(); // 버튼 연결 함수 호출
        }
        else { Debug.LogError("GameOverPanel을 찾을 수 없습니다!"); }


        // 5. 레벨 텍스트 새로 찾기
        GameObject levelTextObject = GameObject.Find("LevelText");
        if (levelTextObject != null)
        {
            levelText = levelTextObject.GetComponent<TextMeshProUGUI>();
            UpdateLevelUI();
        }
        else { Debug.LogError("LevelText 오브젝트를 찾을 수 없습니다!"); }

        // (★★★★★ 5. 타이머 텍스트 새로 찾기 ★★★★★)
        // 유니티 에디터에서 만든 "TimerText"를 찾아서 연결합니다.
        GameObject timerTextObject = GameObject.Find("TimerText");
        if (timerTextObject != null)
        {
            timerText = timerTextObject.GetComponent<TextMeshProUGUI>();
            UpdateTimerUI(); // 초기 시간 표시
        }
        else
        {
            // 타이머 UI가 없어도 게임은 돌아가도록 에러 대신 경고만 출력
            Debug.LogWarning("TimerText 오브젝트를 찾을 수 없습니다! UI를 추가해주세요.");
        }

        // 6. 일시정지 패널 찾기
        GameObject pausePanelObject = GameObject.Find("PausePanel");
        if (pausePanelObject != null)
        {
            pausePanel = pausePanelObject;

            // 버튼 연결
            Transform resumeBtn = pausePanel.transform.Find("ResumeButton");
            if (resumeBtn) resumeBtn.GetComponent<Button>().onClick.AddListener(TogglePause);

            Transform menuBtn = pausePanel.transform.Find("MainMenuButton");
            if (menuBtn) menuBtn.GetComponent<Button>().onClick.AddListener(LoadMainMenu);

            pausePanel.SetActive(false);
        }
        else { Debug.LogError("PausePanel을 찾을 수 없습니다!"); }

        // 7. 플레이어 스폰 및 UI 업데이트
        SpawnPlayer();
        UpdateHeartsUI();
    }

    private IEnumerator EnsureTimeRuns()
    {
        yield return null;
        Time.timeScale = 1f;
    }

    // 게임오버 버튼 연결 헬퍼 함수
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
            mainMenuButton.onClick.AddListener(LoadMainMenu);
        }
    }

    public void PlayerTookDamage(int damage)
    {
        if (currentPlayer == null || playerLives <= 0) return;
        playerLives -= damage;
        if (playerLives < 0) playerLives = 0;

        UpdateHeartsUI();

        // 피격 효과
        if (hitSoundClip != null && audioSource != null) { audioSource.PlayOneShot(hitSoundClip); }
        // (폭발 효과 등은 생략 가능하거나 기존 유지)

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
            LinkGameOverButtons(); // 패널 뜰 때 버튼 확실히 연결
        }

        // 폭발 효과 및 사운드
        Vector3 pos = currentPlayer.transform.position;
        if (finalExplosionClip != null && audioSource != null) { audioSource.PlayOneShot(finalExplosionClip); }
        if (bigExplosionPrefab != null) { Instantiate(bigExplosionPrefab, pos, Quaternion.identity); }

        Destroy(currentPlayer);
        currentPlayer = null;

        Debug.Log("GAME OVER");
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

    // (★★★★★ 6. 타이머 UI 업데이트 함수 추가 ★★★★★)
    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            // 초 단위를 분:초 (04:00) 형식으로 변환
            int minutes = Mathf.FloorToInt(currentTimer / 60F);
            int seconds = Mathf.FloorToInt(currentTimer % 60F);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            // 시간이 10초 이하로 남으면 빨간색으로 경고 (선택사항)
            if (currentTimer <= 10f) timerText.color = Color.red;
            else timerText.color = Color.white;
        }
    }

    public void PlayPortalSound()
    {
        if (audioSource != null && portalSpawnSound != null)
            audioSource.PlayOneShot(portalSpawnSound);
    }

    public void GoToNextLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;
        currentLevelLength++;
        currentLevel++;

        // (난이도 조절 로직 유지)
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
        // (★★★★★ 7. 재시작 시 타이머도 리셋 ★★★★★)
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
    // (★★★★★ GameManager.cs 맨 아래에 추가할 함수 ★★★★★)
    public void PlayerHeal(int amount)
    {
        // 죽어있거나 이미 풀피면 회복 안 함
        if (playerLives <= 0 || playerLives >= maxPlayerLives) return;

        playerLives += amount;
        if (playerLives > maxPlayerLives)
        {
            playerLives = maxPlayerLives;
        }

        UpdateHeartsUI(); // 하트 UI 갱신
        Debug.Log("체력 회복! 현재 체력: " + playerLives);
    }
    public void PlayHeartDropSound()
    {
        if (audioSource != null && heartDropClip != null)
        {
            audioSource.PlayOneShot(heartDropClip);
        }
    }
}