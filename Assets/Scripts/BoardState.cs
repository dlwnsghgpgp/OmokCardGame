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
    public bool IsPlayable(int col, int row) => IsEmpty(col, row);

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
        }
        return true;
    }

    public void Reset()
    {
        System.Array.Clear(_cells, 0, _cells.Length);
        _scoredWindows.Clear();
        StoneCount = 0;
        BlackScore = 0;
        WhiteScore = 0;
    }
}