using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    [Header("Map Prefab")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;

    [Header("Map Settings")]
    public float tileSize = 1.0f;

    [Header("Prefab Size (중요)")]
    [Tooltip("벽과 블록 프리팹의 실제 가로/세로 크기 (Scale 기준)")]
    public float prefabWorldSize = 5.0f; // 블록과 벽 모두 5.0으로 가정

    [Header("Block Settings")]
    public GameObject portalPrefab;
    [Tooltip("블록이 스폰될 Y축 높이")]
    public float blockSpawnY = 1.0f;

    [Header("Block Prefabs")]
    public GameObject greenBlockPrefab;
    public GameObject blueBlockPrefab;
    public GameObject purpleBlockPrefab;
    public GameObject redBlockPrefab;

    void Start()
    {
        GenerateLevel();
        SpawnBlocks();
    }

    void GenerateLevel()
    {
        int width = GameManager.currentLevelWidth;
        int length = GameManager.currentLevelLength;
        int halfWidth = width / 2;
        Quaternion sideWallRotation = Quaternion.Euler(0, 90, 0);
        float offset = tileSize / 2f;

        // (벽 확장 오프셋)
        float wallOffset = 0f;

        for (int x = -halfWidth; x < halfWidth; x++)
        {
            for (int z = 0; z < length; z++)
            {
                Vector3 position = new Vector3(x * tileSize + offset, 0, z * tileSize + offset);

                if (x == -halfWidth) // 왼쪽 벽
                {
                    position.x -= wallOffset;
                    Instantiate(wallPrefab, position, sideWallRotation, transform);
                }
                else if (x == (halfWidth - 1)) // 오른쪽 벽
                {
                    position.x += wallOffset;
                    Instantiate(wallPrefab, position, sideWallRotation, transform);
                }
                else if (z == 0 || z == (length - 1)) // 앞뒤 벽
                {
                    Instantiate(wallPrefab, position, Quaternion.identity, transform);
                }
                else // 바닥
                {
                    Instantiate(floorPrefab, position, Quaternion.identity, transform);
                }
            }
        }
    }

    void SpawnBlocks()
    {
        if (portalPrefab == null || greenBlockPrefab == null || blueBlockPrefab == null || purpleBlockPrefab == null || redBlockPrefab == null)
        {
            Debug.LogError("LevelGenerator에 Portal 또는 Block 프리팹이 전부 연결되지 않았습니다!");
            return;
        }

        List<DestructibleBlock> spawnedBlocks = new List<DestructibleBlock>();

        int width = GameManager.currentLevelWidth;
        int length = GameManager.currentLevelLength;
        int halfWidth = width / 2;
        float offset = tileSize / 2f;

        if (prefabWorldSize <= 0)
        {
            Debug.LogError("Prefab World Size는 0보다 커야 합니다!");
            return;
        }

        // --- 버퍼 계산 (플레이어 스폰 지점 보호용) ---
        float blockHalfSize = prefabWorldSize / 2.0f; // 2.5
        int playerGridBuffer = Mathf.CeilToInt(blockHalfSize / tileSize) + 1; // 2.5 -> 3칸 + 1칸 = 4칸

        // --- X축 스폰 범위 계산 (그리드 기준) ---
        int xMin = -halfWidth + 1; // -19
        int xMax = halfWidth - 1;  // 19

        // --- Z축 스폰 범위 계산 (그리드 기준) ---
        int zMax = length - 1; // 59
        int zMin_MapStart = (int)(length * 0.4f); // 24
        int zMin_PlayerArea = 1 + playerGridBuffer; // 1 + 4 = 5
        int zMin = Mathf.Max(zMin_MapStart, zMin_PlayerArea); // 24

        if (xMin >= xMax || zMin >= zMax)
        {
            Debug.LogError($"맵이 너무 좁아 블록을 스폰할 수 없습니다! (X: {xMin}to{xMax - 1}, Z: {zMin}to{zMax - 1})");
            return;
        }

        Debug.Log($"블록 스폰 범위 (버퍼 없음): X ({xMin} ~ {xMax - 1}), Z ({zMin} ~ {zMax - 1})");

        // --- 스폰 로직 (모든 타일마다 체크) ---
        for (int x = xMin; x < xMax; x++)
        {
            for (int z = zMin; z < zMax; z++)
            {
                // (★★★★★ 1. 핵심 수정: 30% 스폰 확률 ★★★★★)
                // 0.0 ~ 1.0 사이의 랜덤 숫자를 뽑아서
                // 0.3 (30%)보다 작거나 같을 때만 블록을 스폰합니다.
                if (Random.Range(0f, 1f) <= 0.3f)
                {
                    // (기존 스폰 로직이 이 if문 안으로 들어옴)
                    Vector3 spawnPosition = new Vector3(x * tileSize + offset, blockSpawnY, z * tileSize + offset);

                    GameObject prefabToSpawn = greenBlockPrefab;
                    float rand = Random.Range(0f, 1f); // 블록 '종류'를 정하기 위한 별개의 랜덤
                    float cumulativeChance = 0f;
                    cumulativeChance += GameManager.greenChance;
                    if (rand <= cumulativeChance) { prefabToSpawn = greenBlockPrefab; }
                    else
                    {
                        cumulativeChance += GameManager.blueChance;
                        if (rand <= cumulativeChance) { prefabToSpawn = blueBlockPrefab; }
                        else
                        {
                            cumulativeChance += GameManager.purpleChance;
                            if (rand <= cumulativeChance) { prefabToSpawn = purpleBlockPrefab; }
                            else { prefabToSpawn = redBlockPrefab; }
                        }
                    }
                    if (prefabToSpawn == null) { prefabToSpawn = greenBlockPrefab; }

                    GameObject blockGO = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                    blockGO.transform.parent = this.transform;

                    DestructibleBlock blockScript = blockGO.GetComponent<DestructibleBlock>();
                    if (blockScript == null)
                    {
                        Debug.LogError(prefabToSpawn.name + " 프리팹에 DestructibleBlock.cs 스크립트가 없습니다!");
                        Destroy(blockGO);
                        continue;
                    }

                    blockScript.SetPortalPrefab(portalPrefab);
                    spawnedBlocks.Add(blockScript);
                }
                // (★★★★★ 2. 70%의 확률로는 else문이 실행되어 아무것도 스폰하지 않음 ★★★★★)
            }
        }

        // --- 포탈 숨기기 ---
        if (spawnedBlocks.Count > 0)
        {
            int portalIndex = Random.Range(0, spawnedBlocks.Count);
            spawnedBlocks[portalIndex].isPortalBlock = true;
        }
    }
}