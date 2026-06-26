using System.Collections.Generic;

/// <summary>
/// 돌의 색(빈 칸 포함). 화면(BoardView)과 턴 관리(GameManager) 등
/// 여러 스크립트에서 공통으로 쓰므로 최상위 enum으로 둔다.
/// </summary>
public enum CellState { Empty, Black, White }

/// <summary>
/// 오목 보드의 순수 데이터 모델. MonoBehaviour가 아니므로 유니티에 의존하지 않고,
/// 테스트나 서버에서도 그대로 재사용할 수 있다.
/// 화면 표시나 턴 관리는 이 클래스를 "사용"하기만 한다.
/// </summary>
public class BoardState
{
    /// <summary>돌을 놓은 결과를 호출자(GameManager)에게 돌려주는 구조체.</summary>
    public struct PlaceResult
    {
        public bool Success;      // 놓기 성공 여부
        public int PointsScored;  // 이번 수로 새로 획득한 점수(새로 완성된 5목 개수)
        public string Error;      // 실패 시 이유(디버그용)

        public static PlaceResult Fail(string reason) =>
            new PlaceResult { Success = false, PointsScored = 0, Error = reason };
    }

    public int Size { get; private set; }
    public int MoveCount { get; private set; }   // 놓인 돌 총 개수(고정 턴 수 종료에 사용)
    public int BlackScore { get; private set; }
    public int WhiteScore { get; private set; }

    private readonly CellState[,] _cells;

    // 이미 득점 처리된 5칸 윈도우를 잠가서 같은 묶음의 중복 득점을 막는다.
    // 키: (방향 인덱스, 시작 col, 시작 row)
    private readonly HashSet<(int dir, int col, int row)> _scoredWindows
        = new HashSet<(int, int, int)>();

    // 4방향 단위벡터: 가로, 세로, ↗대각, ↘대각.
    // 각 방향을 하나의 벡터로만 보면(반대 방향은 보지 않음) 윈도우가 중복 정의되지 않는다.
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
        // C# 배열 기본값은 0 = CellState.Empty 라서 따로 초기화하지 않아도 된다.
    }

    public bool InBounds(int col, int row) =>
        col >= 0 && col < Size && row >= 0 && row < Size;

    public CellState GetCell(int col, int row) =>
        InBounds(col, row) ? _cells[col, row] : CellState.Empty;

    public bool IsEmpty(int col, int row) =>
        InBounds(col, row) && _cells[col, row] == CellState.Empty;

    public bool IsBoardFull => MoveCount >= Size * Size;

    /// <summary>
    /// (col,row)에 color 돌을 놓는다. 성공 시 새로 완성된 5목 개수만큼 점수를 더한다.
    /// </summary>
    public PlaceResult PlaceStone(int col, int row, CellState color)
    {
        if (color == CellState.Empty)
            return PlaceResult.Fail("빈 색은 놓을 수 없습니다.");
        if (!InBounds(col, row))
            return PlaceResult.Fail("보드 범위를 벗어났습니다.");
        if (_cells[col, row] != CellState.Empty)
            return PlaceResult.Fail("이미 돌이 있는 자리입니다.");

        _cells[col, row] = color;
        MoveCount++;

        int gained = CountNewlyCompletedWindows(col, row, color);
        if (gained > 0)
        {
            if (color == CellState.Black) BlackScore += gained;
            else WhiteScore += gained;
        }

        return new PlaceResult { Success = true, PointsScored = gained, Error = null };
    }

    /// <summary>
    /// 방금 놓은 돌이 포함된, 아직 득점되지 않은 5칸 윈도우 중
    /// 모두 같은 색으로 채워진 것의 개수를 세고 잠근다.
    /// 한 수가 두 방향에서 동시에 5목을 만들면 그만큼 여러 점이 들어갈 수 있다.
    /// </summary>
    private int CountNewlyCompletedWindows(int col, int row, CellState color)
    {
        int newWindows = 0;

        for (int d = 0; d < Directions.Length; d++)
        {
            var (dc, dr) = Directions[d];

            // 이 돌을 포함하는 5칸 윈도우의 시작점은 0~4칸 뒤에 있을 수 있다.
            for (int back = 0; back < 5; back++)
            {
                int startCol = col - dc * back;
                int startRow = row - dr * back;

                if (IsWindowAllColor(startCol, startRow, dc, dr, color))
                {
                    var key = (d, startCol, startRow);
                    if (_scoredWindows.Add(key)) // 처음 득점되는 윈도우면 true 반환
                        newWindows++;
                }
            }
        }

        return newWindows;
    }

    // 시작점에서 방향으로 이어지는 5칸이 모두 보드 안이고 모두 color인지 검사.
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

    /// <summary>판을 처음 상태로 되돌린다(재시작용).</summary>
    public void Reset()
    {
        System.Array.Clear(_cells, 0, _cells.Length);
        _scoredWindows.Clear();
        MoveCount = 0;
        BlackScore = 0;
        WhiteScore = 0;
    }
}