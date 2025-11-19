using UnityEngine;
using System.Collections;

public class DestructibleBlock : MonoBehaviour
{
    [Header("Block Stats")]
    public int health = 1;

    [Header("Portal Logic")]
    public bool isPortalBlock = false;
    public GameObject portalPrefab;

    [Header("Item Drop")]
    // (★★★★★ 1. 하트 아이템 프리팹 연결 변수 ★★★★★)
    public GameObject heartItemPrefab;
    [Range(0f, 1f)]
    public float dropChance = 0.05f; // 5% 확률

    private Collider col;
    private MeshRenderer rend;
    private bool isDying = false;

    void Awake()
    {
        col = GetComponent<Collider>();
        rend = GetComponent<MeshRenderer>();
    }

    public void SetPortalPrefab(GameObject portalToSpawn)
    {
        portalPrefab = portalToSpawn;
    }

    public void TakeDamage(int damage)
    {
        if (isDying) return;

        health -= damage;

        if (health <= 0)
        {
            isDying = true;
            StartCoroutine(DieCoroutine());
        }
    }

    IEnumerator DieCoroutine()
    {
        yield return new WaitForFixedUpdate();

        // 1. 포탈 블록이면 포탈 생성
        if (isPortalBlock && portalPrefab != null)
        {
            Instantiate(portalPrefab, transform.position, Quaternion.identity);
            GameManager.Instance.PlayPortalSound();
        }
        // 2. 포탈이 아닐 때만 하트 드랍 시도
        else if (heartItemPrefab != null)
        {
            if (Random.Range(0f, 1f) <= dropChance)
            {
                Instantiate(heartItemPrefab, transform.position, Quaternion.identity);

                // (★★★★★ 3. 핵심 추가: 하트 드랍 소리 재생 요청 ★★★★★)
                GameManager.Instance.PlayHeartDropSound();
            }
        }

        if (col != null) col.enabled = false;
        if (rend != null) rend.enabled = false;

        Destroy(gameObject, 1.0f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isDying)
        {
            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();

            if (pc != null && !pc.IsDashing())
            {
                GameManager.Instance.PlayerInstantDeath();
            }
        }
    }
}