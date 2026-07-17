using System.Collections.Generic;

/// <summary>
/// 돌의 색(빈 칸 포함). 여러 스크립트에서 공통으로 쓰므로 최상위 enum.
/// </summary>
public enum CellState { Empty, Black, White }

/// <summary>
/// 오목 보드의 순수 데이터 모델. MonoBehaviour가 아니라 유니티에 의존하지 않는다.
/// 화면/턴/카드 효과는 이 클래스를 "사용"하기만 한다.
/// 카드 능력을 대비해 돌 제거(RemoveStone)와 잠금 해제를 지원한다.
/// </summary>
public class BoardState
{
    /// <summary>돌을 놓은 결과를 호출자에게 돌려주는 구조체.</summary>
    public struct PlaceResult
    {
        public bool Success;      // 놓기 성공 여부
        public int PointsScored;  // 이번 수로 새로 획득한 점수(새로 완성된 5목 개수)
        public string Error;      // 실패 시 이유(디버그용)

        public static PlaceResult Fail(string reason) =>
            new PlaceResult { Success = false, PointsScored = 0, Error = reason };
    }

    public int Size { get; private set; }
    public int StoneCount { get; private set; }   // 현재 보드 위 돌 수(IsBoardFull 판정용)
    public int BlackScore { get; private set; }
    public int WhiteScore { get; private set; }

    private readonly CellState[,] _cells;

    // 이미 득점 처리된 5칸 윈도우를 잠가 중복 득점을 막는다.
    // 키: (방향 인덱스, 시작 col, 시작 row)
    private readonly HashSet<(int dir, int col, int row)> _scoredWindows
        = new HashSet<(int, int, int)>();

    // 카드로 "둘 수 없게" 막은 칸들(돌+칸 제거 카드 등). 벽 카드도 나중에 여기에 얹는다.
    private readonly HashSet<(int col, int row)> _blocked
        = new HashSet<(int, int)>();

    // 감염된(좀비가 된) 돌들. 원래 주인 색은 _cells에 그대로 남고, 여기 있으면 "좀비"다.
    // 좀비 돌은 득점 라인에 포함되지 않는다(IsWindowAllColor 참고).
    private readonly HashSet<(int col, int row)> _infected
        = new HashSet<(int, int)>();

    // 4방향 단위벡터: 가로, 세로, ↗대각, ↘대각.
    private static readonly (int dc, int dr)[] Directions =
    {
        (1, 0),   // 가로
        (0, 1),   // 세로
        (1, 1),   // ↗ 대각
        (1, -1),  // ↘ 대각
    };

    public BoardState(int size = 15)
    {
        Size = size;
        _cells = new CellState[size, size];
    }

    public bool InBounds(int col, int row) =>
        col >= 0 && col < Size && row >= 0 && row < Size;

    public CellState GetCell(int col, int row) =>
        InBounds(col, row) ? _cells[col, row] : CellState.Empty;

    public bool IsEmpty(int col, int row) =>
        InBounds(col, row) && _cells[col, row] == CellState.Empty;

    /// <summary>
    /// 그 칸에 돌을 둘 수 있는지 — 미리보기와 실제 착수가 공유하는 단일 규칙.
    /// 지금은 "보드 안의 빈 칸"이면 가능. 벽·함정 카드가 생기면 여기에만 조건을 더한다.
    /// </summary>
    public bool IsPlayable(int col, int row) => IsEmpty(col, row) && !IsBlocked(col, row);

    /// <summary>그 칸이 카드로 막혀 있는가.</summary>
    public bool IsBlocked(int col, int row) => _blocked.Contains((col, row));

    /// <summary>그 칸을 둘 수 없게 막는다(돌+칸 제거, 벽 카드 등). 빈 칸에만 의미가 있다.</summary>
    public void Block(int col, int row)
    {
        if (InBounds(col, row)) _blocked.Add((col, row));
    }

    /// <summary>막힌 칸을 해제한다(임시 벽 해제 등, 나중 단계용).</summary>
    public void Unblock(int col, int row) => _blocked.Remove((col, row));

    /// <summary>그 칸의 돌이 감염(좀비)되었는가.</summary>
    public bool IsInfected(int col, int row) => _infected.Contains((col, row));

    /// <summary>그 칸의 돌을 감염시킨다. 원래 주인 색은 유지되지만 득점 라인에서는 빠진다.</summary>
    public void Infect(int col, int row)
    {
        if (!InBounds(col, row)) return;
        if (_cells[col, row] == CellState.Empty) return;   // 빈 칸은 감염 불가
        if (!_infected.Add((col, row))) return;            // 이미 감염됨

        // 감염되면 그 칸을 지나던 줄이 끊기므로, 득점 잠금을 풀어 재득점 가능하게 한다.
        // (이미 얻은 점수는 유지 — 제거 카드와 같은 규칙)
        UnlockWindowsThrough(col, row);
    }

    /// <summary>특정 색의 "멀쩡한"(감염되지 않은) 돌 개수. 좀비 전멸 판정에 쓴다.</summary>
    public int CountHealthy(CellState color)
    {
        int n = 0;
        for (int c = 0; c < Size; c++)
        for (int r = 0; r < Size; r++)
            if (_cells[c, r] == color && !_infected.Contains((c, r))) n++;
        return n;
    }

    public bool IsBoardFull => StoneCount >= Size * Size;

    /// <summary>(col,row)에 color 돌을 놓는다. 새로 완성된 5목 개수만큼 점수를 더한다.</summary>
    public PlaceResult PlaceStone(int col, int row, CellState color)
    {
        if (color == CellState.Empty)
            return PlaceResult.Fail("빈 색은 놓을 수 없습니다.");
        if (!InBounds(col, row))
            return PlaceResult.Fail("보드 범위를 벗어났습니다.");
        if (!IsPlayable(col, row))
            return PlaceResult.Fail("둘 수 없는 자리입니다.");

        _cells[col, row] = color;
        StoneCount++;

        int gained = CountNewlyCompletedWindows(col, row, color);
        if (gained > 0)
        {
            if (color == CellState.Black) BlackScore += gained;
            else WhiteScore += gained;
        }

        return new PlaceResult { Success = true, PointsScored = gained, Error = null };
    }

    /// <summary>
    /// (col,row)의 돌을 제거한다(카드 능력용). 제거된 돌의 색을 반환(빈 칸이면 Empty).
    /// 그 칸을 지나던 득점 윈도우 잠금을 풀어, 나중에 줄을 다시 만들면 재득점되게 한다.
    /// revokePoints=true 이면 그 윈도우들로 얻었던 점수도 함께 차감한다(점수까지 태우는 카드용).
    /// 기본값은 false — 이미 번 점수는 유지.
    /// </summary>
    public CellState RemoveStone(int col, int row, bool revokePoints = false)
    {
        if (!InBounds(col, row)) return CellState.Empty;
        CellState color = _cells[col, row];
        if (color == CellState.Empty) return CellState.Empty;

        _cells[col, row] = CellState.Empty;
        StoneCount--;
        _infected.Remove((col, row));   // 돌이 사라지면 감염 정보도 사라진다

        int unlocked = UnlockWindowsThrough(col, row);
        if (revokePoints && unlocked > 0)
        {
            // 그 칸을 지나던 득점 윈도우는 모두 제거된 돌의 색이었다.
            if (color == CellState.Black) BlackScore -= unlocked;
            else WhiteScore -= unlocked;
        }
        return color;
    }

    /// <summary>
    /// 방금 놓은 돌이 포함된, 아직 득점되지 않은 5칸 윈도우 중
    /// 모두 같은 색인 것의 개수를 세고 잠근다. 두 방향 동시 완성 시 여러 점이 들어갈 수 있다.
    /// </summary>
    private int CountNewlyCompletedWindows(int col, int row, CellState color)
    {
        int newWindows = 0;
        for (int d = 0; d < Directions.Length; d++)
        {
            var (dc, dr) = Directions[d];
            for (int back = 0; back < 5; back++)   // 이 돌을 포함하는 5칸 윈도우 후보
            {
                int startCol = col - dc * back;
                int startRow = row - dr * back;
                if (IsWindowAllColor(startCol, startRow, dc, dr, color))
                {
                    if (_scoredWindows.Add((d, startCol, startRow)))
                        newWindows++;
                }
            }
        }
        return newWindows;
    }

    // 그 칸을 지나는 모든 득점 윈도우의 잠금을 푼다. 푼 개수를 반환.
    private int UnlockWindowsThrough(int col, int row)
    {
        int removed = 0;
        for (int d = 0; d < Directions.Length; d++)
        {
            var (dc, dr) = Directions[d];
            for (int back = 0; back < 5; back++)
            {
                var key = (d, col - dc * back, row - dr * back);
                if (_scoredWindows.Remove(key)) removed++;
            }
        }
        return removed;
    }

    private bool IsWindowAllColor(int startCol, int startRow, int dc, int dr, CellState color)
    {
        for (int i = 0; i < 5; i++)
        {
            int c = startCol + dc * i;
            int r = startRow + dr * i;
            if (!InBounds(c, r) || _cells[c, r] != color)
                return false;
            if (_infected.Contains((c, r)))   // 좀비 돌은 줄로 인정하지 않는다
                return false;
        }
        return true;
    }

    /// <summary>
    /// 보드를 그대로 복제한다(AI의 가상 평가용).
    /// 돌을 다시 두는 방식과 달리 감염·차단·득점 잠금까지 정확히 옮긴다.
    /// </summary>
    public BoardState Clone()
    {
        var clone = new BoardState(Size);
        System.Array.Copy(_cells, clone._cells, _cells.Length);
        foreach (var w in _scoredWindows) clone._scoredWindows.Add(w);
        foreach (var b in _blocked) clone._blocked.Add(b);
        foreach (var i in _infected) clone._infected.Add(i);
        clone.StoneCount = StoneCount;
        clone.BlackScore = BlackScore;
        clone.WhiteScore = WhiteScore;
        return clone;
    }

    public void Reset()
    {
        System.Array.Clear(_cells, 0, _cells.Length);
        _scoredWindows.Clear();
        _blocked.Clear();
        _infected.Clear();
        StoneCount = 0;
        BlackScore = 0;
        WhiteScore = 0;
    }
}