using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>게임을 끝내는 방식.</summary>
public enum EndCondition { FixedMoves, TargetScore, BoardFull }

/// <summary>
/// 게임 진행 총괄. 한 턴 = 드로우 → [행동 루프: 카드 or 돌] → 돌을 두면
/// 상대에게 카운터 기회를 준 뒤 턴 종료.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("참조")]
    public BoardView boardView;
    public GameUI gameUI;

    [Header("종료 조건")]
    public EndCondition endCondition = EndCondition.FixedMoves;
    public int maxMovesPerPlayer = 30;
    public int targetScore = 5;

    [Header("AI 설정")]
    public float aiThinkDelay = 0.4f;

    [Header("카드")]
    public List<CardData> deckCards = new List<CardData>();
    public int maxHandSize = 7;

    public event Action<int, int> ScoreChanged;
    public event Action<CellState> TurnChanged;
    public event Action<string> GameOver;
    public event Action<IReadOnlyList<CardData>> HumanHandChanged;

    private BoardState _board;
    private IPlayerAgent _blackPlayer;
    private IPlayerAgent _whitePlayer;
    private IPlayerAgent _current;
    private CellState _currentColor;
    private bool _gameOver;
    private bool _cardPlayedThisTurn;

    private Deck _deck;
    private Hand _blackHand;
    private Hand _whiteHand;

    private static CellState Opponent(CellState c) =>
        (c == CellState.Black) ? CellState.White : CellState.Black;

    void Start()
    {
        _board = new BoardState(boardView.boardSize);
        boardView.CanPlace = _board.IsPlayable;

        _blackPlayer = new HumanPlayer(boardView);
        _whitePlayer = new AIPlayer(aiThinkDelay);

        if (gameUI != null) gameUI.CardClicked += OnHumanCardClicked;

        StartGame();
    }

    void OnDestroy()
    {
        if (gameUI != null) gameUI.CardClicked -= OnHumanCardClicked;
    }

    void Update()
    {
        if (!_gameOver) _current?.Tick(Time.deltaTime);
    }

    public void StartGame()
    {
        _board.Reset();
        boardView.ClearAll();

        _deck = new Deck(deckCards);
        _deck.Shuffle();
        _blackHand = new Hand();
        _whiteHand = new Hand();

        _gameOver = false;
        _currentColor = CellState.Black;

        ScoreChanged?.Invoke(_board.BlackScore, _board.WhiteScore);
        HumanHandChanged?.Invoke(_blackHand.Cards);
        Debug.Log($"게임 시작! 흑(사람)부터. 덱 {_deck.Count}장.");
        BeginTurn();
    }

    private Hand HandOf(CellState color) => (color == CellState.Black) ? _blackHand : _whiteHand;
    private Hand CurrentHand() => HandOf(_currentColor);

    private void BeginTurn()
    {
        if (_gameOver) return;

        DrawFor(_currentColor);
        TurnChanged?.Invoke(_currentColor);
        _cardPlayedThisTurn = false;

        _current = (_currentColor == CellState.Black) ? _blackPlayer : _whitePlayer;
        RequestActionFromCurrent();
    }

    private void RequestActionFromCurrent()
    {
        if (_gameOver) return;
        _current.RequestAction(_board, _currentColor, OnActionChosen);
    }

    private void DrawFor(CellState color)
    {
        Hand hand = HandOf(color);
        if (hand.Count >= maxHandSize) return;

        CardData card = _deck.Draw();
        if (card == null) return;

        hand.Add(card);
        Debug.Log($"{color} 드로우: {card.cardName} (덱 {_deck.Count}장 남음)");
        if (color == CellState.Black) HumanHandChanged?.Invoke(_blackHand.Cards);
    }

    private void OnHumanCardClicked(int handIndex)
    {
        if (_current is HumanPlayer hp) hp.SubmitCardChoice(handIndex);
    }

    private void OnActionChosen(TurnAction action)
    {
        if (action.Type == TurnActionType.PlayCard)
            HandlePlayCard(action.CardIndex);
        else
            PlaceStoneAction(action.Col, action.Row);
    }

    private void HandlePlayCard(int index)
    {
        Hand hand = CurrentHand();
        CardData card = hand.Get(index);

        if (card == null) { RequestActionFromCurrent(); return; }
        if (_cardPlayedThisTurn)
        {
            Debug.Log("이번 턴엔 이미 카드를 사용했습니다.");
            RequestActionFromCurrent();
            return;
        }

        var checkCtx = new CardContext(_board, boardView, _currentColor);
        if (card.Type != CardType.Active || !card.CanUse(checkCtx))
        {
            Debug.Log($"{card.cardName}: 지금 사용할 수 없는 카드입니다(카운터는 상대 턴에 자동 발동).");
            RequestActionFromCurrent();
            return;
        }

        StartCoroutine(ResolveCard(card, index));
    }

    private IEnumerator ResolveCard(CardData card, int index)
    {
        var ctx = new CardContext(_board, boardView, _currentColor);
        yield return StartCoroutine(card.Execute(ctx));

        if (!ctx.Cancelled)
        {
            CurrentHand().RemoveAt(index);
            _cardPlayedThisTurn = true;
            ScoreChanged?.Invoke(_board.BlackScore, _board.WhiteScore);
            if (_currentColor == CellState.Black) HumanHandChanged?.Invoke(_blackHand.Cards);
            Debug.Log($"카드 사용 완료: {card.cardName}");

            if (CheckGameEnd()) { EndGame(); yield break; }
        }
        else Debug.Log($"카드 취소: {card.cardName}");

        RequestActionFromCurrent();
    }

    private void PlaceStoneAction(int col, int row)
    {
        var result = _board.PlaceStone(col, row, _currentColor);
        if (!result.Success)
        {
            Debug.LogWarning($"잘못된 수({result.Error}) — 다시.");
            RequestActionFromCurrent();
            return;
        }

        boardView.PlaceStoneVisual(col, row, _currentColor);
        ScoreChanged?.Invoke(_board.BlackScore, _board.WhiteScore);

        if (result.PointsScored > 0)
            Debug.Log($"{_currentColor} +{result.PointsScored}점!  흑 {_board.BlackScore} : 백 {_board.WhiteScore}");

        // 착수 후: 상대에게 카운터 기회를 준 뒤 턴을 마무리한다(비동기).
        StartCoroutine(AfterStonePlaced(col, row, result.PointsScored));
    }

    private IEnumerator AfterStonePlaced(int col, int row, int points)
    {
        CellState placer = _currentColor;
        CellState reactor = Opponent(placer);

        var evt = new GameEventInfo
        {
            Trigger = GameTrigger.StonePlaced,
            Actor = placer,
            Col = col,
            Row = row,
            PointsScored = points,
        };

        yield return StartCoroutine(OfferCounters(reactor, evt));

        if (CheckGameEnd()) { EndGame(); yield break; }

        _currentColor = Opponent(_currentColor);
        BeginTurn();
    }

    // 상대(reactor)의 손패에서 조건이 맞는 카운터를 찾아 사용 여부를 묻고, 예면 실행.
    private IEnumerator OfferCounters(CellState reactor, GameEventInfo evt)
    {
        // AI는 아직 카운터를 쓰지 않는다(8d에서). 지금은 사람(흑)만.
        if (reactor != CellState.Black || gameUI == null) yield break;

        Hand hand = _blackHand;
        int i = 0;
        while (i < hand.Count)
        {
            CardData card = hand.Get(i);
            var ctx = new CardContext(_board, boardView, reactor) { TriggerInfo = evt };

            if (card != null && card.Type == CardType.Counter && card.CanCounter(evt, ctx))
            {
                bool decided = false, use = false;
                gameUI.ShowCounterPrompt(card, d => { use = d; decided = true; });
                while (!decided) yield return null;

                if (use)
                {
                    yield return StartCoroutine(card.Execute(ctx));
                    if (!ctx.Cancelled)
                    {
                        hand.RemoveAt(i);
                        ScoreChanged?.Invoke(_board.BlackScore, _board.WhiteScore);
                        HumanHandChanged?.Invoke(_blackHand.Cards);
                        Debug.Log($"카운터 사용: {card.cardName}");
                        continue;   // 카드가 빠져 뒤가 당겨졌으니 i 유지하고 계속 스캔
                    }
                }
            }
            i++;
        }
    }

    private bool CheckGameEnd()
    {
        if (_board.IsBoardFull) return true;
        switch (endCondition)
        {
            case EndCondition.FixedMoves:
                return _board.StoneCount >= maxMovesPerPlayer * 2;
            case EndCondition.TargetScore:
                return _board.BlackScore >= targetScore || _board.WhiteScore >= targetScore;
            case EndCondition.BoardFull:
                return false;
        }
        return false;
    }

    private void EndGame()
    {
        _gameOver = true;
        _blackPlayer.Cancel();
        _whitePlayer.Cancel();

        string winner = _board.BlackScore > _board.WhiteScore ? "흑 승리"
                      : _board.WhiteScore > _board.BlackScore ? "백 승리"
                      : "무승부";
        string result = $"{winner}\n흑 {_board.BlackScore} : 백 {_board.WhiteScore}";

        Debug.Log($"게임 종료!  {result.Replace("\n", "  ")}");
        GameOver?.Invoke(result);
    }
}