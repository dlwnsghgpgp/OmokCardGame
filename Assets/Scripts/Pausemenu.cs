using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 게임 중 Tab 키로 여는 메뉴. 설정과 게임 포기(타이틀 복귀)를 제공한다.
/// 메뉴가 열린 동안엔 보드 입력을 잠가 실수로 착수되지 않게 한다.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("참조")]
    public GameObject menuPanel;
    public BoardView boardView;     // 열려 있는 동안 착수 금지

    [Header("버튼")]
    public Button resumeButton;     // 계속하기
    public Button settingsButton;   // 설정(추후)
    public Button giveUpButton;     // 게임 포기 → 타이틀로

    private bool _open;

    void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);

        if (resumeButton != null)   resumeButton.onClick.AddListener(Close);
        if (settingsButton != null) settingsButton.onClick.AddListener(() => Debug.Log("[일시정지] 설정 — 추후 구현"));
        if (giveUpButton != null)   giveUpButton.onClick.AddListener(() => GameSession.Instance.LoadTitle());
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (_open) Close();
            else Open();
        }
    }

    public void Open()
    {
        _open = true;
        if (menuPanel != null) menuPanel.SetActive(true);
        if (boardView != null) boardView.InputLocked = true;
    }

    public void Close()
    {
        _open = false;
        if (menuPanel != null) menuPanel.SetActive(false);
        if (boardView != null) boardView.InputLocked = false;
    }
}