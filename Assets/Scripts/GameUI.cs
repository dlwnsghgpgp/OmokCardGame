using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면(UI) 담당. 점수·턴·게임오버·손패를 GameManager 이벤트로 갱신한다.
/// (8a-2) 손패는 클릭 가능한 카드 버튼으로 표시. 클릭 시 CardClicked(인덱스)를 쏜다.
/// 이미지·호버 툴팁은 8a-2b에서 이 버튼을 확장한다.
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("참조")]
    public GameManager gameManager;

    [Header("텍스트")]
    public TMP_Text scoreText;
    public TMP_Text turnText;

    [Header("손패")]
    public Transform handContainer;      // Horizontal Layout Group을 단 빈 오브젝트
    public GameObject cardButtonPrefab;  // Button + 자식 TMP_Text

    [Header("게임오버")]
    public GameObject gameOverPanel;
    public TMP_Text resultText;
    public Button restartButton;

    /// <summary>손패의 카드가 클릭됐을 때 그 인덱스를 알린다.</summary>
    public event Action<int> CardClicked;

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
        if (handContainer == null || cardButtonPrefab == null) return;

        // 기존 카드 버튼 제거
        for (int i = handContainer.childCount - 1; i >= 0; i--)
            Destroy(handContainer.GetChild(i).gameObject);

        // 손패 다시 그리기
        for (int i = 0; i < cards.Count; i++)
        {
            int index = i;   // 클로저 캡처 주의: 반복 변수 대신 지역 복사본 사용
            GameObject go = Instantiate(cardButtonPrefab);   // 먼저 씬에 복제한 뒤
            go.transform.SetParent(handContainer, false);    // 그 다음 부모 지정(false=로컬 좌표 유지)

            var label = go.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = cards[i].cardName;

            var btn = go.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => CardClicked?.Invoke(index));
        }
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