using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // TextMeshPro 사용

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("시작 버튼을 눌렀을 때 로드할 게임 씬의 이름입니다.")]
    public string gameSceneName = "SampleScene"; // (주의: 실제 씬 이름 확인!)

    [Header("UI Elements")]
    // (★★★★★ 2. 최고 레벨 텍스트 변수 추가 ★★★★★)
    public TextMeshProUGUI highestLevelText;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // (★★★★★ 3. 저장된 최고 레벨 불러오기 ★★★★★)
        // 저장된 값이 없으면 기본값 1을 가져옵니다.
        int highestLevel = PlayerPrefs.GetInt("HighestLevel", 1);

        if (highestLevelText != null)
        {
            highestLevelText.text = "HIGHEST LEVEL: " + highestLevel;
        }
    }

    public void OnStartClicked()
    {
        Debug.Log("게임 시작!");
        SceneManager.LoadScene("SampleScene");
    }

    public void OnExitClicked()
    {
        Debug.Log("게임 종료...");
        Application.Quit();
    }

    // (선택 사항: 기록 초기화 버튼을 만들고 싶다면 연결하세요)
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey("HighestLevel");
        if (highestLevelText != null) highestLevelText.text = "HIGHEST LEVEL: 1";
    }
}