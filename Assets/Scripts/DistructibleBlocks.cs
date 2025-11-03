using UnityEngine;
using System.Collections;

public class DestructibleBlock : MonoBehaviour
{
    [Header("Block Stats")]
    public int health = 1;

    [Header("Portal Logic")]
    public bool isPortalBlock = false;
    public GameObject portalPrefab;

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

    // (★★★★★ 이 함수가 수정되었습니다 ★★★★★)
    IEnumerator DieCoroutine()
    {
        // 총알이 튕겨나갈 시간을 1프레임 벌어줍니다.
        yield return new WaitForFixedUpdate();

        if (isPortalBlock && portalPrefab != null)
        {
            Instantiate(portalPrefab, transform.position, Quaternion.identity);

            // (★★★★★ 1. 핵심 추가: 포탈 사운드 ★★★★★)
            GameManager.Instance.PlayPortalSound();
        }

        // 콜라이더와 렌더러를 끔
        if (col != null) col.enabled = false;
        if (rend != null) rend.enabled = false;

        // 1초 뒤 오브젝트를 완전히 파괴
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