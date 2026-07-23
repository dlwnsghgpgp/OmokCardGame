using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면. 메인 메뉴 → 모드 선택 → 테마 선택 순으로 패널을 넘긴다.
/// (10-2에서는 전환 동작만. 테마 목록 등 내용물은 10-5에서 채운다.)
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [Header("패널")]
    public GameObject mainPanel;    // 게임 이름 + 메뉴 버튼들
    public GameObject modePanel;    // AI전 / PvP전
    public GameObject themePanel;   // 테마 목록

    [Header("메인 메뉴 버튼")]
    public Button startButton;      // 게임 시작 → 모드 선택
    public Button deckEditButton;   // 덱 편집 씬으로
    public Button settingsButton;   // 설정(추후)
    public Button quitButton;       // 게임 나가기

    [Header("모드 선택 버튼")]
    public Button versusAIButton;
    public Button versusPlayerButton;   // 아직 미구현 → 비활성 처리
    public Button modeBackButton;

    [Header("테마 선택 버튼")]
    public Button themeStartButton;     // 임시: 테마를 고르고 게임 시작
    public Button themeBackButton;

    void Start()
    {
        // 메인 메뉴
        if (startButton != null)    startButton.onClick.AddListener(() => ShowPanel(modePanel));
        if (deckEditButton != null) deckEditButton.onClick.AddListener(() => GameSession.Instance.LoadDeckEdit());
        if (settingsButton != null) settingsButton.onClick.AddListener(() => Debug.Log("[타이틀] 설정 — 추후 구현"));
        if (quitButton != null)     quitButton.onClick.AddListener(() => GameSession.Instance.QuitGame());

        // 모드 선택
        if (versusAIButton != null)
            versusAIButton.onClick.AddListener(() =>
            {
                GameSession.Instance.Mode = PlayMode.VersusAI;
                ShowPanel(themePanel);
            });
        if (versusPlayerButton != null)
            versusPlayerButton.interactable = false;   // PvP는 아직 미구현
        if (modeBackButton != null)
            modeBackButton.onClick.AddListener(() => ShowPanel(mainPanel));

        // 테마 선택 (10-5에서 실제 테마 목록으로 교체)
        if (themeStartButton != null)
            themeStartButton.onClick.AddListener(() =>
            {
                GameSession.Instance.SelectedThemeId = "default";
                GameSession.Instance.LoadGame();
            });
        if (themeBackButton != null)
            themeBackButton.onClick.AddListener(() => ShowPanel(modePanel));

        ShowPanel(mainPanel);
    }

    // 한 번에 한 패널만 보이게 한다.
    private void ShowPanel(GameObject target)
    {
        if (mainPanel != null)  mainPanel.SetActive(mainPanel == target);
        if (modePanel != null)  modePanel.SetActive(modePanel == target);
        if (themePanel != null) themePanel.SetActive(themePanel == target);
    }
}