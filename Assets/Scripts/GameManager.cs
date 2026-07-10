using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>게임을 끝내는 방식.</summary>
public enum EndCondition { FixedMoves, TargetScore, BoardFull }

/// <summary>
/// 게임 진행 총괄. 한 턴 = 드로우 → [행동 루프] → 필요한 만큼 착수 → 상대 카운터 기회 → 턴 종료.
/// 패시브는 들고만 있어도 적용되며(추가 착수·돌 보호), 누가 들었든 자동으로 작동한다.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("참조")]
    public BoardView boardView;
    public GameUI gameUI;
    public AIHandView aiHandView;   // AI 손패 3D 표시(선택)

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
    public event Action<int> AIHandCountChanged;   // AI 손패 장수(내용은 비공개)

    private BoardState _board;
    private IPlayerAgent _blackPlayer;
    private IPlayerAgent _whitePlayer;
    private IPlayerAgent _current;
    private CellState _currentColor;
    private bool _gameOver;
    private bool _cardPlayedThisTurn;
    private int _placementsThisTurn;
    private int _placementsRequired;

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
        if (aiHandView != null) AIHandCountChanged += aiHandView.SetHandCount;

        StartGame();
    }

    void OnDestroy()
    {
        if (gameUI != null) gameUI.CardClicked -= OnHumanCardClicked;
        if (aiHandView != null) AIHandCountChanged -= aiHandView.SetHandCount;
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
        AIHandCountChanged?.Invoke(_whiteHand.Count);
        Debug.Log($"게임 시작! 흑(사람)부터. 덱 {_deck.Count}장.");
        BeginTurn();
    }

    private Hand HandOf(CellState color) => (color == CellState.Black) ? _blackHand : _whiteHand;
    private Hand CurrentHand() => HandOf(_currentColor);

    // 특정 색 플레이어가 해당 패시브를 손에 들고 있는가.
    private bool HasPassiveEffect(CellState color, PassiveEffect effect)
    {
        foreach (var c in HandOf(color).Cards)
            if (c != null && c.Passive == effect) return true;
        return false;
    }

    // 패시브 조회가 주입된 카드 컨텍스트를 만든다.
    private CardContext NewContext(CellState user)
        => new CardContext(_board, boardView, user) { HasPassive = HasPassiveEffect };

    private void BeginTurn()
    {
        if (_gameOver) return;

        DrawFor(_currentColor);
        TurnChanged?.Invoke(_currentColor);

        _cardPlayedThisTurn = false;
        _placementsThisTurn = 0;
        // 추가 착수 패시브를 들고 있으면 이번 턴 착수 2회.
        _placementsRequired = HasPassiveEffect(_currentColor, PassiveEffect.ExtraStonePerTurn) ? 2 : 1;

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
        string kind = card.Type == CardType.Passive ? "[패시브·공개]" : "";
        Debug.Log($"{color} 드로우: {card.cardName} {kind} (덱 {_deck.Count}장 남음)");
        if (color == CellState.Black) HumanHandChanged?.Invoke(_blackHand.Cards);
        else AIHandCountChanged?.Invoke(_whiteHand.Count);
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
        if (card.Type == CardType.Passive)
        {
            Debug.Log($"{card.cardName}: 패시브 카드는 들고만 있어도 효과가 적용됩니다.");
            RequestActionFromCurrent();
            return;
        }
        if (_cardPlayedThisTurn)
        {
            Debug.Log("이번 턴엔 이미 카드를 사용했습니다.");
            RequestActionFromCurrent();
            return;
        }

        var checkCtx = NewContext(_currentColor);
        if (card.Type != CardType.Active || !card.CanUse(checkCtx))
        {
            Debug.Log($"{card.cardName}: 지금 사용할 수 없는 카드입니다.");
            RequestActionFromCurrent();
            return;
        }

        StartCoroutine(ResolveCard(card, index));
    }

    private IEnumerator ResolveCard(CardData card, int index)
    {
        var ctx = NewContext(_currentColor);
        yield return StartCoroutine(card.Execute(ctx));

        if (!ctx.Cancelled)
        {
            CurrentHand().RemoveAt(index);
            _cardPlayedThisTurn = true;
            ScoreChanged?.Invoke(_board.BlackScore, _board.WhiteScore);
            if (_currentColor == CellState.Black) HumanHandChanged?.Invoke(_blackHand.Cards);
            else AIHandCountChanged?.Invoke(_whiteHand.Count);
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

        _placementsThisTurn++;
        boardView.PlaceStoneVisual(col, row, _currentColor);
        ScoreChanged?.Invoke(_board.BlackScore, _board.WhiteScore);

        if (result.PointsScored > 0)
            Debug.Log($"{_currentColor} +{result.PointsScored}점!  흑 {_board.BlackScore} : 백 {_board.WhiteScore}");

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

        // 추가 착수 패시브: 아직 이번 턴 착수가 덜 됐으면 같은 플레이어가 이어서 둔다.
        if (_placementsThisTurn < _placementsRequired)
        {
            Debug.Log($"추가 착수: {_placementsThisTurn}/{_placementsRequired} — 한 번 더 두세요.");
            RequestActionFromCurrent();
            yield break;
        }

        _currentColor = Opponent(_currentColor);
        BeginTurn();
    }

    private IEnumerator OfferCounters(CellState reactor, GameEventInfo evt)
    {
        if (reactor != CellState.Black || gameUI == null) yield break;   // 사람(흑)만, 8d에서 AI

        Hand hand = _blackHand;
        int i = 0;
        while (i < hand.Count)
        {
            CardData card = hand.Get(i);
            var ctx = NewContext(reactor);
            ctx.TriggerInfo = evt;

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
                        continue;
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