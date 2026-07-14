using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면(UI) 담당. 점수·턴·게임오버·손패를 GameManager 이벤트로 갱신한다.
/// (8a-2b) 손패는 CardView로 그리고, 카드 호버 시 화면을 어둡게 덮는 포커스 오버레이에
/// 큰 이미지 + 이름 + 효과를 띄운다. 벗어나면 오버레이를 끈다.
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("참조")]
    public GameManager gameManager;
    public BoardView boardView;   // 타겟 모드 안내를 받기 위해

    [Header("텍스트")]
    public TMP_Text scoreText;
    public TMP_Text turnText;
    public TMP_Text targetingHintText;   // 타겟 모드일 때 "우클릭으로 취소" 안내

    [Header("손패")]
    public Transform handContainer;      // Horizontal Layout Group을 단 빈 오브젝트
    public GameObject cardButtonPrefab;  // CardView가 붙은 카드 프리팹

    [Header("카드 포커스(호버 시)")]
    public GameObject cardFocusOverlay;  // 화면을 덮는 어두운 패널(루트)
    public Image focusImage;             // 큰 카드 이미지
    public TMP_Text focusName;           // 카드 이름
    public TMP_Text focusDescription;    // 카드 효과 설명

    [Header("묘지 목록")]
    public GameObject graveyardPanel;       // 묘지 카드 목록 패널(루트)
    public Transform graveyardContainer;    // Horizontal/Grid Layout Group을 단 오브젝트
    public UnityEngine.UI.Button graveyardCloseButton;

    [Header("카운터 프롬프트")]
    public GameObject counterPromptPanel;
    public TMP_Text counterPromptText;
    public Button counterUseButton;
    public Button counterSkipButton;

    [Header("게임오버")]
    public GameObject gameOverPanel;
    public TMP_Text resultText;
    public Button restartButton;

    /// <summary>손패의 카드가 클릭됐을 때 그 인덱스를 알린다.</summary>
    public event Action<int> CardClicked;

    private Action<bool> _counterDecision;

    void Awake()
    {
        if (gameManager != null)
        {
            gameManager.ScoreChanged += OnScoreChanged;
            gameManager.TurnChanged += OnTurnChanged;
            gameManager.GameOver += OnGameOver;
            gameManager.HumanHandChanged += OnHumanHandChanged;
        }
        if (boardView != null)
            boardView.TargetingChanged += OnTargetingChanged;
        if (targetingHintText != null)
            targetingHintText.gameObject.SetActive(false);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (counterUseButton != null)
            counterUseButton.onClick.AddListener(OnCounterUse);
        if (counterSkipButton != null)
            counterSkipButton.onClick.AddListener(OnCounterSkip);
        if (graveyardCloseButton != null)
            graveyardCloseButton.onClick.AddListener(CloseGraveyard);
        if (graveyardPanel != null)
            graveyardPanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (counterPromptPanel != null)
            counterPromptPanel.SetActive(false);
        if (cardFocusOverlay != null)
            cardFocusOverlay.SetActive(false);   // 처음엔 숨김
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
        if (boardView != null)
            boardView.TargetingChanged -= OnTargetingChanged;
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
        if (counterUseButton != null)
            counterUseButton.onClick.RemoveListener(OnCounterUse);
        if (counterSkipButton != null)
            counterSkipButton.onClick.RemoveListener(OnCounterSkip);
        if (graveyardCloseButton != null)
            graveyardCloseButton.onClick.RemoveListener(CloseGraveyard);
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

    private void OnTargetingChanged(bool active)
    {
        if (targetingHintText == null) return;
        targetingHintText.text = "대상을 클릭하세요 (우클릭: 취소)";
        targetingHintText.gameObject.SetActive(active);
    }

    private void OnHumanHandChanged(IReadOnlyList<CardData> cards)
    {
        HideCardFocus();   // 손패가 바뀌면 포커스는 닫아둔다(카드 소모 후 잔상 방지)

        if (handContainer == null || cardButtonPrefab == null) return;

        // 기존 카드 제거
        for (int i = handContainer.childCount - 1; i >= 0; i--)
            Destroy(handContainer.GetChild(i).gameObject);

        // 손패 다시 그리기 — 각 카드에 CardView.Setup 으로 데이터 주입
        for (int i = 0; i < cards.Count; i++)
        {
            GameObject go = Instantiate(cardButtonPrefab);
            go.transform.SetParent(handContainer, false);

            var view = go.GetComponent<CardView>();
            if (view != null) view.Setup(cards[i], i, this);
        }
    }

    // ── CardView가 호출하는 콜백 ──

    public void ShowCardFocus(CardData card)
    {
        if (cardFocusOverlay == null || card == null) return;

        if (focusImage != null)
        {
            // artFull 우선, 없으면 artIcon, 그것도 없으면 단색(빈 카드)로 표시
            focusImage.sprite = card.artFull != null ? card.artFull : card.artIcon;
        }
        if (focusName != null) focusName.text = card.cardName;
        if (focusDescription != null) focusDescription.text = card.description;

        cardFocusOverlay.SetActive(true);
    }

    public void HideCardFocus()
    {
        if (cardFocusOverlay != null) cardFocusOverlay.SetActive(false);
    }

    public void OnCardViewClicked(int index)
    {
        if (index < 0) return;   // 묘지 목록 카드(-1) 등은 클릭해도 사용되지 않음
        CardClicked?.Invoke(index);
    }

    // ── 묘지 목록 ──

    /// <summary>버려진 순서대로 카드 앞면을 나열해 보여준다. 호버 시 카드 포커스가 뜬다.</summary>
    public void ShowGraveyard(System.Collections.Generic.IReadOnlyList<GraveEntry> entries)
    {
        if (graveyardPanel == null || graveyardContainer == null || cardButtonPrefab == null) return;

        for (int i = graveyardContainer.childCount - 1; i >= 0; i--)
            Destroy(graveyardContainer.GetChild(i).gameObject);

        // 버려진 순서대로(오래된 것부터). 주인 구분은 표시하지 않는다.
        for (int i = 0; i < entries.Count; i++)
        {
            GameObject go = Instantiate(cardButtonPrefab);
            go.transform.SetParent(graveyardContainer, false);

            var view = go.GetComponent<CardView>();
            // index -1: 묘지 카드는 클릭해도 사용되지 않게(손패가 아님). 호버 포커스만 동작.
            if (view != null) view.Setup(entries[i].Card, -1, this);
        }

        graveyardPanel.SetActive(true);
        if (boardView != null) boardView.InputLocked = true;   // 목록 보는 동안 착수 금지
    }

    public void CloseGraveyard()
    {
        HideCardFocus();
        if (graveyardPanel != null) graveyardPanel.SetActive(false);
        if (boardView != null) boardView.InputLocked = false;  // 다시 착수 허용
    }

    // ── 카운터 프롬프트 (GameManager가 코루틴에서 호출) ──

    public void ShowCounterPrompt(CardData card, Action<bool> onDecision)
    {
        _counterDecision = onDecision;
        if (counterPromptText != null)
            counterPromptText.text = $"카운터 발동 가능: {card.cardName}\n{card.description}\n\n사용하시겠습니까?";
        if (counterPromptPanel != null) counterPromptPanel.SetActive(true);
    }

    private void OnCounterUse()  { ResolveCounterDecision(true); }
    private void OnCounterSkip() { ResolveCounterDecision(false); }

    private void ResolveCounterDecision(bool use)
    {
        if (counterPromptPanel != null) counterPromptPanel.SetActive(false);
        var cb = _counterDecision;
        _counterDecision = null;
        cb?.Invoke(use);
    }

    // ── 게임오버 ──

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