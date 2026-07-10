using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 규칙 기반(휴리스틱) AI 플레이어. 학습/신경망이 아니라,
/// "각 빈 칸이 얼마나 좋은가"를 직접 정의한 평가 함수로 최고점 칸을 고른다.
/// (8d-2) 자기 차례에 카드를 쓸지도 판단한다. 카운터 응답은 GameManager가 물어본다.
/// </summary>
public class AIPlayer : IPlayerAgent
{
    // 착수 평가 가중치 — 성격을 바꾸려면 이 숫자들만 조절.
    private const float ScorePerPoint = 1000f;
    private const float OffenseLineWeight = 1f;
    private const float DefenseLineWeight = 0.9f;
    private const float CenterBias = 0.05f;

    // 줄 길이별 가치. index = 연속 길이. 4목은 5목 직전이라 급등.
    private static readonly float[] LineValue = { 0f, 1f, 5f, 25f, 120f, 0f };

    // 상대의 연속 돌이 이 길이 이상이면 "위협"으로 보고 제거 카드를 쓴다.
    private const int ThreatLength = 4;

    private readonly float _thinkDelay;
    private BoardState _board;
    private CellState _myColor;
    private Action<TurnAction> _onAction;
    private float _timer;
    private bool _thinking;

    // GameManager가 턴 시작 시 넣어주는 내 손패(읽기 전용) + 이번 턴 카드 사용 여부.
    private IReadOnlyList<CardData> _hand;
    private bool _cardUsedThisTurn;

    public AIPlayer(float thinkDelay = 0.4f) { _thinkDelay = thinkDelay; }

    /// <summary>GameManager가 AI에게 자기 손패를 알려준다(사람은 UI로 보므로 불필요).</summary>
    public void SetHand(IReadOnlyList<CardData> hand) => _hand = hand;

    /// <summary>새 턴이 시작될 때 GameManager가 호출(카드 사용 플래그 초기화).</summary>
    public void BeginTurn() => _cardUsedThisTurn = false;

    public void RequestAction(BoardState board, CellState myColor, Action<TurnAction> onAction)
    {
        _board = board;
        _myColor = myColor;
        _onAction = onAction;
        _timer = 0f;
        _thinking = true;   // 실제 결정은 Tick()에서 딜레이 뒤에
    }

    public void Tick(float deltaTime)
    {
        if (!_thinking) return;
        _timer += deltaTime;
        if (_timer < _thinkDelay) return;

        _thinking = false;
        var callback = _onAction;
        TurnAction action = DecideAction();
        Cancel();
        callback?.Invoke(action);
    }

    public void Cancel()
    {
        _thinking = false;
        _onAction = null;
    }

    private CellState Opponent => _myColor == CellState.Black ? CellState.White : CellState.Black;

    // ── 행동 결정: 카드를 쓸지 먼저 보고, 아니면 돌을 둔다 ──

    private TurnAction DecideAction()
    {
        if (!_cardUsedThisTurn)
        {
            int cardIndex = ChooseCardToPlay();
            if (cardIndex >= 0)
            {
                _cardUsedThisTurn = true;   // GameManager도 턴당 1장 제한이 있지만, AI가 반복 시도하지 않게.
                return TurnAction.Card(cardIndex);
            }
        }
        ChooseBestMove(out int col, out int row);
        return TurnAction.Place(col, row);
    }

    /// <summary>쓸 만한 액티브 카드의 손패 인덱스. 없으면 -1.</summary>
    private int ChooseCardToPlay()
    {
        if (_hand == null || _hand.Count == 0) return -1;

        bool threatened = OpponentHasThreat();

        for (int i = 0; i < _hand.Count; i++)
        {
            CardData card = _hand[i];
            if (card == null || card.Type != CardType.Active) continue;   // 패시브·카운터는 여기서 안 씀

            // 제거 계열: 상대가 5목 직전(4목)일 때만 쓴다. 아니면 아껴둔다.
            if (card is RemoveStoneCardData || card is RemoveAndBlockCardData)
            {
                if (threatened) return i;
                continue;
            }

            // 추가 돌 두기: 돌을 더 두는 건 거의 항상 이득이므로 바로 쓴다.
            if (card is ExtraStoneCardData) return i;
        }
        return -1;
    }

    /// <summary>상대에게 5목 직전(연속 ThreatLength 이상) 줄이 있는가.</summary>
    private bool OpponentHasThreat()
    {
        int size = _board.Size;
        var dirs = new (int dc, int dr)[] { (1, 0), (0, 1), (1, 1), (1, -1) };

        for (int c = 0; c < size; c++)
        for (int r = 0; r < size; r++)
        {
            if (_board.GetCell(c, r) != Opponent) continue;
            foreach (var (dc, dr) in dirs)
            {
                int len = 1 + CountSame(c, r, dc, dr, Opponent)
                            + CountSame(c, r, -dc, -dr, Opponent);
                if (len >= ThreatLength) return true;
            }
        }
        return false;
    }

    // ── 착수 평가(기존 로직 그대로) ──

    private void ChooseBestMove(out int bestCol, out int bestRow)
    {
        int size = _board.Size;
        float center = (size - 1) / 2f;
        float bestScore = float.NegativeInfinity;
        bestCol = size / 2;
        bestRow = size / 2;

        for (int c = 0; c < size; c++)
        for (int r = 0; r < size; r++)
        {
            if (!_board.IsPlayable(c, r)) continue;   // 빈 칸 + 막히지 않은 칸만 후보

            float offense = EvaluateFor(c, r, _myColor);
            float defense = EvaluateFor(c, r, Opponent);

            float distToCenter = Mathf.Abs(c - center) + Mathf.Abs(r - center);
            float centerScore = -distToCenter * CenterBias;

            float total = OffenseLineWeight * offense
                        + DefenseLineWeight * defense
                        + centerScore;

            if (total > bestScore)
            {
                bestScore = total;
                bestCol = c;
                bestRow = r;
            }
        }
    }

    private float EvaluateFor(int c, int r, CellState color)
    {
        float value = 0f;

        BoardState sim = CloneBoard();
        var result = sim.PlaceStone(c, r, color);
        if (result.Success) value += result.PointsScored * ScorePerPoint;

        var dirs = new (int dc, int dr)[] { (1, 0), (0, 1), (1, 1), (1, -1) };
        foreach (var (dc, dr) in dirs)
        {
            int len = 1 + CountSame(c, r, dc, dr, color) + CountSame(c, r, -dc, -dr, color);
            int idx = Mathf.Clamp(len, 0, LineValue.Length - 1);
            value += LineValue[idx];
        }
        return value;
    }

    private int CountSame(int c, int r, int dc, int dr, CellState color)
    {
        int count = 0;
        int cc = c + dc, rr = r + dr;
        while (_board.InBounds(cc, rr) && _board.GetCell(cc, rr) == color)
        {
            count++;
            cc += dc; rr += dr;
        }
        return count;
    }

    private BoardState CloneBoard()
    {
        var clone = new BoardState(_board.Size);
        for (int c = 0; c < _board.Size; c++)
        for (int r = 0; r < _board.Size; r++)
        {
            var cell = _board.GetCell(c, r);
            if (cell != CellState.Empty) clone.PlaceStone(c, r, cell);
        }
        return clone;
    }
}