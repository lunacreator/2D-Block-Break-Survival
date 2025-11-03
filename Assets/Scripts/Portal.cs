using UnityEngine;

public class Portal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 포탈에 닿으면
        if (other.CompareTag("Player"))
        {
            // 여러 번 실행되지 않도록 콜라이더를 끔
            GetComponent<Collider>().enabled = false;

            // 게임 매니저에게 다음 레벨로 가라고 알림
            GameManager.Instance.GoToNextLevel();
        }
    }
}   