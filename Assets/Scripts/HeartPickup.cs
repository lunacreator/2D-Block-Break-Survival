using UnityEngine;

public class HeartPickup : MonoBehaviour
{
    [Header("Settings")]
    public int healAmount = 1;
    public float rotateSpeed = 100f; // 빙글빙글 도는 속도

    void Update()
    {
        // 아이템이 제자리에서 빙글빙글 돌게 만듦 (시각 효과)
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어와 닿으면
        if (other.CompareTag("Player"))
        {
            // 게임 매니저에게 회복 요청
            GameManager.Instance.PlayerHeal(healAmount);

            // (선택사항) 획득 사운드 재생 (GameManager에 사운드 추가 필요)
            // GameManager.Instance.PlayHealSound(); 

            // 아이템 삭제
            Destroy(gameObject);
        }
    }
}