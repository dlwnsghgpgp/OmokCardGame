using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면(UI)만 담당. GameManager 이벤트를 듣고 점수·턴·게임오버·손패를 갱신한다.
/// (8a-1) 손패는 아직 글자로만 표시. 클릭 가능한 카드는 8a-2에서.
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("참조")]
    public GameManager gameManager;

    [Header("텍스트")]
    public TMP_Text scoreText;
    public TMP_Text turnText;
    public TMP_Text handText;   // "손패: 돌 1개 제거, 추가 돌 두기"

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
            gameManager.HumanHandChanged += OnHumanHandChanged;
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
            gameManager.HumanHandChanged -= OnHumanHandChanged;
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

    private void OnHumanHandChanged(IReadOnlyList<CardData> cards)
    {
        if (handText == null) return;
        if (cards == null || cards.Count == 0)
        {
            handText.text = "손패: (없음)";
            return;
        }
        var sb = new StringBuilder("손패: ");
        for (int i = 0; i < cards.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(cards[i].cardName);
        }
        handText.text = sb.ToString();
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