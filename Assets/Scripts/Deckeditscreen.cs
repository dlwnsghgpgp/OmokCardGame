using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 덱 편집 화면. (10-2에서는 씬 전환 확인용 껍데기. 실제 편집 기능은 10-4에서.)
/// </summary>
public class DeckEditScreen : MonoBehaviour
{
    [Header("버튼")]
    public Button backButton;   // 타이틀로 돌아가기

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(() => GameSession.Instance.LoadTitle());

        Debug.Log("[덱 편집] 화면 진입. 편집 기능은 10-4에서 구현됩니다.");
    }
}