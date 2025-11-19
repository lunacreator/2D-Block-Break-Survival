using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // TextMeshPro 사용

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("시작 버튼을 눌렀을 때 로드할 게임 씬의 이름입니다.")]
    public string gameSceneName = "GameScene"; // (주의: 실제 게임 씬 이름과 같아야 합니다!)

    void Start()
    {
        // 메인 메뉴에 들어오면 시간을 항상 정상 속도로 되돌립니다.
        // (게임에서 죽거나 일시정지 상태로 나왔을 때를 대비함)
        Time.timeScale = 1f;

        // 마우스 커서를 보이게 하고 잠금을 풉니다.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // "START" 버튼 연결용 함수
    // "START" 버튼 연결용 함수
    public void OnStartClicked()
    {
        Debug.Log("게임 시작!");

        // (변수 gameSceneName을 지우고, 여기에 "SampleScene"을 직접 적습니다)
        // 따옴표("") 안에 실제 씬 이름과 대소문자까지 똑같이 적어야 합니다.
        SceneManager.LoadScene("SampleScene");
    }

    // "EXIT" 버튼 연결용 함수
    public void OnExitClicked()
    {
        Debug.Log("게임 종료...");
        Application.Quit(); // 빌드된 게임에서만 작동합니다.
    }
}