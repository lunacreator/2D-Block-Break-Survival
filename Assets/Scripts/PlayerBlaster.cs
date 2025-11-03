using UnityEngine;

public class PlayerBlaster : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject blasterPrefab; // "BlueBlaster.prefab" 연결
    public Transform firePoint;      // 전투기 총구 위치

    [Header("Settings")]
    public float cooldownTime = 0.5f; // 0.5초 쿨타임
    private float _cooldownTimer = 0f;

    [Header("Audio")]
    public AudioSource shootingAudioSource;
    public AudioClip shootingSound;

    void Update()
    {
        // 쿨다운 타이머
        if (_cooldownTimer > 0)
        {
            _cooldownTimer -= Time.deltaTime;
        }

        // (★핵심 수정★) 좌클릭을 '누르고 있는 동안' (GetMouseButton)
        // 그리고 쿨다운이 0 이하면 발사
        if (Input.GetMouseButton(0) && _cooldownTimer <= 0)
        {
            _cooldownTimer = cooldownTime;
            Fire();
        }
    }

    void Fire()
    {
        // 발사 전에 Null 체크
        if (blasterPrefab == null || firePoint == null)
        {
            Debug.LogError("PlayerBlaster: Prefab 또는 Fire Point가 연결되지 않았습니다!");
            return;
        }

        // 1. 총알을 생성
        Vector3 spawnPosition = new Vector3(firePoint.position.x, 3f, firePoint.position.z);
        GameObject bolt = Instantiate(blasterPrefab, spawnPosition, firePoint.rotation);

        // 2. 총알의 BouncingBlaster 스크립트를 찾음
        BouncingBlaster blasterScript = bolt.GetComponent<BouncingBlaster>();
        if (blasterScript != null)
        {
            // 3. 총알에게 "내가(전투기) 네 주인이다"라고 알려줌
            blasterScript.owner = this.gameObject;
        }

        // 4. 사운드 1회 재생
        if (shootingAudioSource != null && shootingSound != null)
        {
            shootingAudioSource.PlayOneShot(shootingSound);
        }
    }
}