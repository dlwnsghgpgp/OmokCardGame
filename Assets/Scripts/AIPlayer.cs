using System;
using UnityEngine;

/// <summary>
/// 규칙 기반(휴리스틱) AI 플레이어. 학습/신경망이 아니라,
/// "각 빈 칸이 얼마나 좋은가"를 직접 정의한 평가 함수로 최고점 칸을 고른다.
/// IPlayerAgent 를 구현하므로 GameManager 입장에선 사람과 똑같이 다뤄진다.
/// 균형형: 공격(내 줄 키우기)과 수비(상대 줄 막기)를 비슷한 비중으로 본다.
/// </summary>
public class AIPlayer : IPlayerAgent
{
    // 평가 가중치 — 성격을 바꾸고 싶으면 이 숫자들만 조절하면 된다.
    private const float ScorePerPoint = 1000f; // 실제 5목 완성(=득점) 1점의 가치: 압도적으로 크게
    private const float OffenseLineWeight = 1f;  // 내 줄 길이 키우기 비중
    private const float DefenseLineWeight = 0.9f; // 상대 줄 막기 비중(균형형이라 공격과 비슷)
    private const float CenterBias = 0.05f;       // 동점일 때 중앙을 살짝 선호

    // 줄 길이별 가치(개방형 기준). index = 연속 길이. 4목은 5목 직전이라 급등.
    private static readonly float[] LineValue = { 0f, 1f, 5f, 25f, 120f, 0f };

    private readonly float _thinkDelay;
    private BoardState _board;
    private CellState _myColor;
    private Action<int, int> _onChosen;
    private float _timer;
    private bool _thinking;

    /// <param name="thinkDelay">수를 두기 전 잠깐 멈추는 시간(초). 사람이 보기 자연스럽게.</param>
    public AIPlayer(float thinkDelay = 0.4f)
    {
        _thinkDelay = thinkDelay;
    }

    public void RequestMove(BoardState board, CellState myColor, Action<int, int> onChosen)
    {
        _board = board;
        _myColor = myColor;
        _onChosen = onChosen;
        _timer = 0f;
        _thinking = true; // 실제 결정은 Tick()에서 약간의 딜레이 뒤에
    }

    /// <summary>GameManager가 매 프레임 호출해 준다(생각하는 척 딜레이 처리).</summary>
    public void Tick(float deltaTime)
    {
        if (!_thinking) return;
        _timer += deltaTime;
        if (_timer < _thinkDelay) return;

        _thinking = false;
        var callback = _onChosen;
        ChooseBestMove(out int col, out int row);
        Cancel();
        callback?.Invoke(col, row);
    }

    public void Cancel()
    {
        _thinking = false;
        _onChosen = null;
    }

    private CellState Opponent => _myColor == CellState.Black ? CellState.White : CellState.Black;

    // 모든 빈 칸을 평가해 최고점 칸을 고른다.
    private void ChooseBestMove(out int bestCol, out int bestRow)
    {
        int size = _board.Size;
        float center = (size - 1) / 2f;
        float bestScore = float.NegativeInfinity;
        bestCol = size / 2;
        bestRow = size / 2; // 안전 기본값(중앙)

        for (int c = 0; c < size; c++)
        for (int r = 0; r < size; r++)
        {
            if (!_board.IsEmpty(c, r)) continue;

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

    /// <summary>
    /// (c,r)에 color 돌을 두면 얼마나 좋은지를 점수화.
    /// 1) 실제 득점(5목 완성)은 복제 보드에 둬보고 PlaceStone 결과로 정확히 잰다.
    /// 2) 5목까지 못 가도 2·3·4목으로 키우는 가치를 4방향 줄 길이로 더한다.
    /// </summary>
    private float EvaluateFor(int c, int r, CellState color)
    {
        float value = 0f;

        // 1) 실제 득점 가치 — BoardState 를 그대로 재활용
        BoardState sim = CloneBoard();
        var result = sim.PlaceStone(c, r, color);
        if (result.Success)
            value += result.PointsScored * ScorePerPoint;

        // 2) 줄 길이 가치(4방향 합산)
        var dirs = new (int dc, int dr)[] { (1, 0), (0, 1), (1, 1), (1, -1) };
        foreach (var (dc, dr) in dirs)
        {
            int len = 1 // 이 칸 자신
                    + CountSame(c, r, dc, dr, color)
                    + CountSame(c, r, -dc, -dr, color);
            int idx = Mathf.Clamp(len, 0, LineValue.Length - 1);
            value += LineValue[idx];
        }
        return value;
    }

    // (c,r)에서 (dc,dr) 방향으로 같은 색 돌이 몇 개 연속인지 센다(자기 칸 제외).
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

    // 현재 보드를 가상 평가용으로 복제한다.
    private BoardState CloneBoard()
    {
        var clone = new BoardState(_board.Size);
        for (int c = 0; c < _board.Size; c++)
        for (int r = 0; r < _board.Size; r++)
        {
            var cell = _board.GetCell(c, r);
            if (cell != CellState.Empty)
                clone.PlaceStone(c, r, cell);
        }
        return clone;
    }
}