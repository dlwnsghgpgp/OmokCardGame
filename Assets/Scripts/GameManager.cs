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
    public AIHandView aiHandView;       // AI 손패 3D 표시(선택)
    public GraveyardStackView graveyardStackView;   // 묘지 3D 더미 표시(선택)
    public GraveyardClickable graveyardClickable;   // 묘지 클릭 감지(선택)
    public FieldZoneView fieldZoneView;             // 필드 카드 3D 표시(선택)

    [Header("종료 조건")]
    public EndCondition endCondition = EndCondition.FixedMoves;
    public int maxMovesPerPlayer = 30;
    public int targetScore = 5;

    [Header("AI 설정")]
    public float aiThinkDelay = 0.4f;

    [Header("카드 — 개별 덱")]
    public List<CardData> blackDeckCards = new List<CardData>();   // 흑(사람) 덱 구성
    public List<CardData> whiteDeckCards = new List<CardData>();   // 백(AI) 덱 구성
    public int maxHandSize = 7;

    [Header("필드 카드")]
    public List<CardData> fieldDeckCards = new List<CardData>();   // 필드 전용 덱(양쪽 공용)
    public int fieldCardTurn = 10;      // 이 턴(양쪽 합산)에 딱 한 번 필드 카드가 나온다
    public int fieldChoiceCount = 3;    // 제시할 후보 장수

    public event Action<int, int> ScoreChanged;
    public event Action<CellState> TurnChanged;
    public event Action<string> GameOver;
    public event Action<IReadOnlyList<CardData>> HumanHandChanged;
    public event Action<int> AIHandCountChanged;   // AI 손패 장수(내용은 비공개)
    public event Action<int> GraveyardChanged;     // 묘지 장수

    private BoardState _board;
    private IPlayerAgent _blackPlayer;
    private IPlayerAgent _whitePlayer;
    private IPlayerAgent _current;
    private CellState _currentColor;
    private bool _gameOver;
    private bool _cardPlayedThisTurn;
    private int _placementsThisTurn;
    private int _placementsRequired;

    private Deck _blackDeck;
    private Deck _whiteDeck;
    private Hand _blackHand;
    private Hand _whiteHand;
    private Graveyard _graveyard = new Graveyard();

    private Deck _fieldDeck;
    private FieldCardData _fieldCard;   // 필드에 깔린 카드(없으면 null)
    private int _turnCount;             // 양쪽 합산 턴 수
    private string _pendingResult;      // 필드 카드가 정한 종료 결과(있으면 이걸 사용)

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
        if (graveyardStackView != null) GraveyardChanged += graveyardStackView.SetCount;
        if (graveyardClickable != null) graveyardClickable.Clicked += OnGraveyardClicked;
        if (fieldZoneView != null) fieldZoneView.HoverChanged += OnFieldCardHover;

        StartGame();
    }

    void OnDestroy()
    {
        if (gameUI != null) gameUI.CardClicked -= OnHumanCardClicked;
        if (aiHandView != null) AIHandCountChanged -= aiHandView.SetHandCount;
        if (graveyardStackView != null) GraveyardChanged -= graveyardStackView.SetCount;
        if (graveyardClickable != null) graveyardClickable.Clicked -= OnGraveyardClicked;
        if (fieldZoneView != null) fieldZoneView.HoverChanged -= OnFieldCardHover;
    }

    void Update()
    {
        if (!_gameOver) _current?.Tick(Time.deltaTime);
    }

    public void StartGame()
    {
        _board.Reset();
        boardView.ClearAll();

        _blackDeck = new Deck(blackDeckCards);
        _whiteDeck = new Deck(whiteDeckCards);
        _blackDeck.Shuffle();
        _whiteDeck.Shuffle();
        _blackHand = new Hand();
        _whiteHand = new Hand();

        _gameOver = false;
        _currentColor = CellState.Black;

        ScoreChanged?.Invoke(_board.BlackScore, _board.WhiteScore);
        HumanHandChanged?.Invoke(_blackHand.Cards);
        AIHandCountChanged?.Invoke(_whiteHand.Count);
        _graveyard.Clear();
        GraveyardChanged?.Invoke(_graveyard.Count);

        _fieldDeck = new Deck(fieldDeckCards);
        _fieldDeck.Shuffle();
        _fieldCard = null;
        _turnCount = 0;
        _pendingResult = null;
        if (fieldZoneView != null) fieldZoneView.SetCard(null);
        Debug.Log($"게임 시작! 흑(사람)부터. 흑 덱 {_blackDeck.Count} / 백 덱 {_whiteDeck.Count}장.");
        BeginTurn();
    }

    private Hand HandOf(CellState color) => (color == CellState.Black) ? _blackHand : _whiteHand;

    /// <summary>묘지에 쌓인 기록(버려진 순서). 9-2b 목록 표시에서 읽는다.</summary>
    public IReadOnlyList<GraveEntry> GraveyardEntries => _graveyard.Entries;

    private void OnGraveyardClicked()
    {
        if (gameUI != null) gameUI.ShowGraveyard(_graveyard.Entries);
    }
    private Hand CurrentHand() => HandOf(_currentColor);

    // 특정 색 플레이어가 해당 패시브를 손에 들고 있는가.
    private bool HasPassiveEffect(CellState color, PassiveEffect effect)
    {
        foreach (var c in HandOf(color).Cards)
            if (c != null && c.Passive == effect) return true;
        return false;
    }

    // 패시브 조회가 주입된 카드 컨텍스트를 만든다.
    // 사용자가 AI(백)라면 자동 대상 선택기도 함께 넣어 클릭 없이 진행되게 한다.
    private CardContext NewContext(CellState user)
    {
        var ctx = new CardContext(_board, boardView, user) { HasPassive = HasPassiveEffect };
        if (user == CellState.White)   // 백 = AI
            ctx.AutoPickTarget = AutoPickForAI;
        return ctx;
    }

    /// <summary>AI용 자동 대상 선택: 유효 칸 중 상대 줄이 가장 긴 곳을 고른다.</summary>
    private bool AutoPickForAI(Func<int, int, bool> isValid, out int col, out int row)
    {
        col = row = -1;
        int size = _board.Size;
        int bestLen = -1;

        for (int c = 0; c < size; c++)
        for (int r = 0; r < size; r++)
        {
            if (!isValid(c, r)) continue;
            int len = LongestLineThrough(c, r);   // 그 칸을 지나는 가장 긴 같은 색 줄
            if (len > bestLen) { bestLen = len; col = c; row = r; }
        }
        return bestLen >= 0;   // 유효 칸이 하나라도 있었는가
    }

    // (c,r)을 지나는, 그 칸 돌 색 기준 가장 긴 연속 줄 길이. 빈 칸이면 0.
    private int LongestLineThrough(int c, int r)
    {
        CellState color = _board.GetCell(c, r);
        if (color == CellState.Empty) return 0;

        var dirs = new (int dc, int dr)[] { (1, 0), (0, 1), (1, 1), (1, -1) };
        int best = 1;
        foreach (var (dc, dr) in dirs)
        {
            int len = 1 + CountSameDir(c, r, dc, dr, color) + CountSameDir(c, r, -dc, -dr, color);
            if (len > best) best = len;
        }
        return best;
    }

    private int CountSameDir(int c, int r, int dc, int dr, CellState color)
    {
        int n = 0, cc = c + dc, rr = r + dr;
        while (_board.InBounds(cc, rr) && _board.GetCell(cc, rr) == color)
        {
            n++; cc += dc; rr += dr;
        }
        return n;
    }

    private void BeginTurn()
    {
        if (_gameOver) return;
        StartCoroutine(BeginTurnRoutine());
    }

    private IEnumerator BeginTurnRoutine()
    {
        if (_gameOver) yield break;

        _turnCount++;

        // 정해진 턴에 딱 한 번, 필드 카드 후보를 제시하고 점수가 낮은 쪽이 고른다.
        if (_turnCount == fieldCardTurn && _fieldCard == null && _fieldDeck.Count > 0)
            yield return StartCoroutine(OfferFieldCard());

        // 필드 카드의 매 턴 효과(예: 감염 확산)
        if (_fieldCard != null)
        {
            _fieldCard.OnTurnBegin(NewFieldContext());
            ScoreChanged?.Invoke(_board.BlackScore, _board.WhiteScore);
            if (CheckGameEnd()) { EndGame(); yield break; }
        }

        DrawFor(_currentColor);
        TurnChanged?.Invoke(_currentColor);

        _cardPlayedThisTurn = false;
        _placementsThisTurn = 0;
        // 추가 착수 패시브를 들고 있으면 이번 턴 착수 2회.
        _placementsRequired = HasPassiveEffect(_currentColor, PassiveEffect.ExtraStonePerTurn) ? 2 : 1;

        _current = (_currentColor == CellState.Black) ? _blackPlayer : _whitePlayer;

        // AI에게 자기 손패를 알려주고 턴 상태를 초기화한다(사람은 UI로 보므로 불필요).
        if (_current is AIPlayer ai)
        {
            ai.SetHand(HandOf(_currentColor).Cards);
            ai.BeginTurn();
        }

        RequestActionFromCurrent();
    }

    private void RequestActionFromCurrent()
    {
        if (_gameOver) return;
        _current.RequestAction(_board, _currentColor, OnActionChosen);
    }

    private Deck DeckOf(CellState color) => (color == CellState.Black) ? _blackDeck : _whiteDeck;

    private void DrawFor(CellState color)
    {
        Hand hand = HandOf(color);
        if (hand.Count >= maxHandSize) return;

        Deck deck = DeckOf(color);
        CardData card = deck.Draw();
        if (card == null) return;   // 그 플레이어의 덱이 비면 못 뽑음

        hand.Add(card);
        string kind = card.Type == CardType.Passive ? "[패시브·공개]" : "";
        Debug.Log($"{color} 드로우: {card.cardName} {kind} (덱 {deck.Count}장 남음)");
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
            _graveyard.Add(card, _currentColor);
            GraveyardChanged?.Invoke(_graveyard.Count);
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
        if (reactor == CellState.White)   // AI: 조건이 맞으면 자동 발동
        {
            yield return StartCoroutine(OfferCountersAI(evt));
            yield break;
        }
        if (gameUI == null) yield break;

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
                        _graveyard.Add(card, CellState.Black);
                        GraveyardChanged?.Invoke(_graveyard.Count);
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

    // AI의 카운터: 사람이 득점했거나 5목 직전 줄을 만들었으면 무효화한다.
    private IEnumerator OfferCountersAI(GameEventInfo evt)
    {
        Hand hand = _whiteHand;
        int i = 0;
        while (i < hand.Count)
        {
            CardData card = hand.Get(i);
            var ctx = NewContext(CellState.White);
            ctx.TriggerInfo = evt;

            if (card != null && card.Type == CardType.Counter && card.CanCounter(evt, ctx))
            {
                // 판단 기준: 그 수가 득점했거나, 4목 이상 줄을 만들었으면 막을 가치가 있다.
                bool worthIt = evt.PointsScored > 0 || LongestLineThrough(evt.Col, evt.Row) >= 4;
                if (worthIt)
                {
                    yield return new WaitForSeconds(aiThinkDelay);   // 반응하는 척 잠깐 멈춤
                    yield return StartCoroutine(card.Execute(ctx));
                    if (!ctx.Cancelled)
                    {
                        hand.RemoveAt(i);
                        _graveyard.Add(card, CellState.White);
                        GraveyardChanged?.Invoke(_graveyard.Count);
                        ScoreChanged?.Invoke(_board.BlackScore, _board.WhiteScore);
                        AIHandCountChanged?.Invoke(_whiteHand.Count);
                        Debug.Log($"AI 카운터 사용: {card.cardName}");
                        continue;
                    }
                }
            }
            i++;
        }
    }

    private FieldContext NewFieldContext() => new FieldContext(_board, boardView);

    // 필드 존 카드에 마우스를 올리면 손패와 같은 카드 포커스를 띄운다.
    private void OnFieldCardHover(bool over)
    {
        if (gameUI == null) return;
        if (over && _fieldCard != null) gameUI.ShowCardFocus(_fieldCard);
        else gameUI.HideCardFocus();
    }

    /// <summary>필드 덱에서 후보를 뽑아, 점수가 낮은 플레이어가 1장을 고르게 한다.</summary>
    private IEnumerator OfferFieldCard()
    {
        // 후보 뽑기
        var candidates = new List<CardData>();
        for (int i = 0; i < fieldChoiceCount; i++)
        {
            var c = _fieldDeck.Draw();
            if (c == null) break;
            candidates.Add(c);
        }
        if (candidates.Count == 0) yield break;

        // 점수가 낮은 쪽이 선택. 동점이면 흑(사람).
        CellState chooser = (_board.WhiteScore < _board.BlackScore) ? CellState.White : CellState.Black;
        Debug.Log($"[필드] {_turnCount}턴 — {chooser}(점수 낮음)가 필드 카드를 고릅니다. 후보 {candidates.Count}장.");

        int picked = -1;

        if (chooser == CellState.Black && gameUI != null)
        {
            // 사람: UI로 고르게 하고 기다린다.
            bool decided = false;
            gameUI.ShowFieldChoice(candidates, "필드 카드를 선택하세요 (점수가 낮은 쪽의 권한)",
                                   i => { picked = i; decided = true; });
            while (!decided) yield return null;
        }
        else
        {
            // AI: 지금은 무작위 선택(판단 로직은 나중에 강화 가능).
            yield return new WaitForSeconds(aiThinkDelay);
            picked = UnityEngine.Random.Range(0, candidates.Count);
        }

        if (picked < 0 || picked >= candidates.Count) picked = 0;

        // 고르지 않은 카드는 덱 아래로 되돌린다.
        for (int i = 0; i < candidates.Count; i++)
            if (i != picked) _fieldDeck.AddBottom(candidates[i]);

        _fieldCard = candidates[picked] as FieldCardData;
        if (_fieldCard == null)
        {
            Debug.LogWarning("[필드] 필드 덱에 필드 카드가 아닌 카드가 들어 있습니다.");
            yield break;
        }

        if (fieldZoneView != null) fieldZoneView.SetCard(_fieldCard);
        _fieldCard.OnActivated(NewFieldContext());
        Debug.Log($"[필드] 발동: {_fieldCard.cardName}");
    }

    private bool CheckGameEnd()
    {
        if (_board.IsBoardFull) return true;

        // 필드 카드가 승패를 정하면 그게 우선한다.
        if (_fieldCard != null)
        {
            if (_fieldCard.CheckWin(NewFieldContext(), out string fieldResult))
            {
                _pendingResult = fieldResult;
                return true;
            }
            // 이 카드가 기존 종료 조건을 덮어쓰면, 여기서 끝내지 않는다.
            if (_fieldCard.SuppressNormalEnd) return false;
        }

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

        string result;
        if (!string.IsNullOrEmpty(_pendingResult))
        {
            // 필드 카드가 정한 승패(예: 좀비 전멸)
            result = $"{_pendingResult}\n흑 {_board.BlackScore} : 백 {_board.WhiteScore}";
        }
        else
        {
            string winner = _board.BlackScore > _board.WhiteScore ? "흑 승리"
                          : _board.WhiteScore > _board.BlackScore ? "백 승리"
                          : "무승부";
            result = $"{winner}\n흑 {_board.BlackScore} : 백 {_board.WhiteScore}";
        }

        Debug.Log($"게임 종료!  {result.Replace("\n", "  ")}");
        GameOver?.Invoke(result);
    }
}