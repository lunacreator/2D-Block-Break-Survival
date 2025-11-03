using UnityEngine;
using System.Collections.Generic;

public class BackgroundGenerator : MonoBehaviour
{
    [Header("Building Prefabs")]
    // (★필수★) 여기에 에셋 스토어에서 받은 '빌딩 프리팹'들을 모두 끌어다 놓으세요.
    public List<GameObject> buildingPrefabs;

    [Header("Placement Settings")]
    [Tooltip("생성할 총 빌딩 수")]
    public int numberOfBuildings = 100;

    [Tooltip("Z축(복도 길이 방향) 최소 배치 좌표")]
    public float zMin = -20f;
    [Tooltip("Z축(복도 길이 방향) 최대 배치 좌표")]
    public float zMax = 80f; // 기존 복도 길이(60)보다 넉넉하게

    [Tooltip("복도 중심(X=0)에서 '최소' 이만큼 떨어진 곳부터 배치")]
    public float minDistanceFromCenter = 30f;
    [Tooltip("복도 중심(X=0)에서 '최대' 이만큼 떨어진 곳까지 배치")]
    public float maxDistanceFromCenter = 150f;

    [Header("Level")]
    // (★★★★★ 여기 핵심 수정 ★★★★★)
    [Tooltip("빌딩이 배치될 '땅'의 Y 높이 (복도 Y=0 보다 훨씬 낮게 설정)")]
    public float groundLevelY = -500f; // 기본값을 -500으로 설정

    [Header("Randomization")]
    [Tooltip("빌딩의 최소 크기 배율")]
    public float minScale = 0.8f;
    [Tooltip("빌딩의 최대 크기 배율")]
    public float maxScale = 1.5f;

    void Start()
    {
        if (buildingPrefabs == null || buildingPrefabs.Count == 0)
        {
            Debug.LogError("BackgroundGenerator: 'Building Prefabs' 리스트에 빌딩 프리팹을 연결해야 합니다!");
            return;
        }

        GenerateBackground();
    }

    void GenerateBackground()
    {
        for (int i = 0; i < numberOfBuildings; i++)
        {
            GameObject prefabToSpawn = buildingPrefabs[Random.Range(0, buildingPrefabs.Count)];

            // X, Z 위치 계산
            float zPos = Random.Range(zMin, zMax);
            float xDistance = Random.Range(minDistanceFromCenter, maxDistanceFromCenter);
            float xPos = (Random.Range(0, 2) == 0) ? -xDistance : xDistance;

            // (★★★★★ Y축 수정 ★★★★★)
            // Y 위치를 0이 아닌, 설정된 'groundLevelY' 값으로 지정
            Vector3 spawnPosition = new Vector3(xPos, groundLevelY, zPos);

            Quaternion spawnRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            float randomScale = Random.Range(minScale, maxScale);
            Vector3 spawnScale = new Vector3(randomScale, randomScale, randomScale);

            GameObject building = Instantiate(prefabToSpawn, spawnPosition, spawnRotation, this.transform);
            building.transform.localScale = spawnScale;
        }
    }
}