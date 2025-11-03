using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Spawn Settings")]
    public float playerSpawnZ = 20.0f;
    public float playerSpawnY = 3.0f;

    [Header("Player Stats")]
    public int playerLives = 5;
    public int maxPlayerLives = 5;

    [Header("UI Elements")]
    public Image[] hearts;
    // (★★★★★ 1. 이 변수는 이제 코드가 자동으로 채웁니다 ★★★★★)
    public GameObject gameOverPanel;

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

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // (GameManager.cs)

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
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
        else
        {
            Debug.LogError("HeartPanel을 찾을 수 없습니다! UI 하트 부모의 이름을 'HeartPanel'로 지정해주세요.");
        }

        // (★★★★★ 4. 이 부분이 수정되었습니다 ★★★★★)
        // 씬이 로드될 때마다 'GameOverPanel' 오브젝트를 이름으로 다시 찾습니다.
        GameObject panel = GameObject.Find("GameOverPanel");
        if (panel != null)
        {
            gameOverPanel = panel;
            gameOverPanel.SetActive(false); // 씬이 로드되었으니 패널을 비활성화합니다.

            // (★★★★★ 5. 핵심 추가: 버튼을 찾아서 이벤트 강제 연결 ★★★★★)
            // GameOverPanel의 자식 중에서 "RestartButton"을 이름으로 찾습니다.
            // (주의: 버튼 오브젝트의 이름이 "RestartButton"이어야 합니다!)
            Transform buttonChild = panel.transform.Find("RestartButton");

            if (buttonChild != null)
            {
                // 찾은 오브젝트에서 Button 컴포넌트를 가져옵니다.
                UnityEngine.UI.Button restartButton = buttonChild.GetComponent<UnityEngine.UI.Button>();

                if (restartButton != null)
                {
                    // 1. (중요) 씬이 로드될 때마다 혹시 모를 기존 리스너를 모두 제거합니다.
                    restartButton.onClick.RemoveAllListeners();

                    // 2. 현재 살아있는 이 GameManager의 RestartGame 함수를 리스너로 등록합니다.
                    restartButton.onClick.AddListener(RestartGame);
                }
                else
                {
                    Debug.LogError("'RestartButton' 오브젝트에 Button 컴포넌트가 없습니다!");
                }
            }
            else
            {
                Debug.LogError("GameOverPanel 내부에 'RestartButton' 오브젝트를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogError("GameOverPanel을 찾을 수 없습니다! UI 패널의 이름을 'GameOverPanel'로 지정해주세요.");
        }

        // 6. 플레이어 스폰
        SpawnPlayer();

        // 7. UI 업데이트
        UpdateHeartsUI();
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
        if (currentPlayer == null) return;

        // (이제 gameOverPanel 참조가 유효하므로 정상 작동)
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Vector3 playerXZPos = new Vector3(currentPlayer.transform.position.x, 0, currentPlayer.transform.position.z);
        Vector3 explosionPos = new Vector3(playerXZPos.x, 3f, playerXZPos.z);
        if (finalExplosionClip != null && audioSource != null) { audioSource.PlayOneShot(finalExplosionClip); }
        if (bigExplosionPrefab != null) { Instantiate(bigExplosionPrefab, explosionPos, Quaternion.identity); }
        Destroy(currentPlayer);
        currentPlayer = null;
        Vector3 flamePos = new Vector3(playerXZPos.x, 0.5f, playerXZPos.z);
        StartCoroutine(SpawnFlamesAfterDelay(flamePos));
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
        if (playerPrefab != null) { currentPlayer = Instantiate(playerPrefab, currentSpawnPoint, Quaternion.identity); if (mainCameraScript != null) { mainCameraScript.InitializeTarget(currentPlayer.transform); } else if (Camera.main != null) { mainCameraScript = Camera.main.GetComponent<CameraFollow>(); if (mainCameraScript != null) { mainCameraScript.InitializeTarget(currentPlayer.transform); } } } else { Debug.LogError("Player Prefab이 GameManager에 연결되지 않았습니다!"); }
    }

    public void RestartGame()
    {
        // (이제 gameOverPanel 참조가 유효하므로 정상 작동)
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

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