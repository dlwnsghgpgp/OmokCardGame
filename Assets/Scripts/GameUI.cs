using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면(UI)만 담당. GameManager가 쏘는 이벤트를 듣고 점수·턴·게임오버를 갱신한다.
/// 게임 로직은 전혀 모른다 — 재시작 버튼만 GameManager.StartGame()을 호출한다.
/// Awake에서 구독하므로(모든 Awake는 모든 Start보다 먼저 실행),
/// GameManager.Start가 쏘는 초기 이벤트도 놓치지 않는다.
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("참조")]
    public GameManager gameManager;

    [Header("텍스트")]
    public TMP_Text scoreText;   // "흑 0 : 백 0"
    public TMP_Text turnText;    // "흑 차례 (당신)" 등

    [Header("게임오버")]
    public GameObject gameOverPanel;
    public TMP_Text resultText;
    public Button restartButton;

    void Awake()
    {
        if (gameManager != null)
        {
            gameManager.ScoreChanged += OnScoreChanged;
            gameManager.TurnChanged += OnTurnChanged;
            gameManager.GameOver += OnGameOver;
        }
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.ScoreChanged -= OnScoreChanged;
            gameManager.TurnChanged -= OnTurnChanged;
            gameManager.GameOver -= OnGameOver;
        }
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
    }

    private void OnScoreChanged(int black, int white)
    {
        if (scoreText != null) scoreText.text = $"흑 {black} : 백 {white}";
    }

    private void OnTurnChanged(CellState color)
    {
        if (turnText == null) return;
        turnText.text = (color == CellState.Black) ? "흑 차례 (당신)" : "백 차례 (AI)";
    }

    private void OnGameOver(string result)
    {
        if (resultText != null) resultText.text = result;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    private void OnRestartClicked()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (gameManager != null) gameManager.StartGame();
    }
}