using System;
using System.Collections;

/// <summary>
/// 카드가 발동될 때 받는 "도구 상자". 보드/화면 접근, 내 색·상대 색,
/// 타겟 요청(PickTarget), 그리고 보드+화면을 함께 바꾸는 고수준 조작을 제공한다.
/// GameManager가 카드마다 새로 만들어 Execute에 넘겨준다.
/// </summary>
public class CardContext
{
    public BoardState Board { get; }
    public BoardView View { get; }
    public CellState User { get; }
    public CellState Opponent { get; }

    // 타겟팅 결과
    public bool Cancelled { get; private set; }
    public int PickedCol { get; private set; }
    public int PickedRow { get; private set; }

    public CardContext(BoardState board, BoardView view, CellState user)
    {
        Board = board;
        View = view;
        User = user;
        Opponent = (user == CellState.Black) ? CellState.White : CellState.Black;
    }

    /// <summary>
    /// isValid를 통과하는 칸을 플레이어가 하나 고르게 한다(코루틴).
    /// 유효 칸을 클릭하면 PickedCol/Row가 채워지고, 우클릭하면 Cancelled=true.
    /// </summary>
    public IEnumerator PickTarget(Func<int, int, bool> isValid)
    {
        bool done = false;
        Cancelled = false;

        View.BeginTargeting(
            isValid,
            (c, r) => { PickedCol = c; PickedRow = r; Cancelled = false; done = true; },
            ()     => { Cancelled = true; done = true; });

        while (!done) yield return null;   // 클릭/취소가 올 때까지 대기
    }

    // ── 고수준 조작: 보드 데이터와 화면을 함께 갱신 ──

    public CellState RemoveStoneAt(int col, int row)
    {
        var removed = Board.RemoveStone(col, row);
        if (removed != CellState.Empty) View.RemoveStoneVisual(col, row);
        return removed;
    }

    public BoardState.PlaceResult PlaceStoneAt(int col, int row, CellState color)
    {
        var res = Board.PlaceStone(col, row, color);
        if (res.Success) View.PlaceStoneVisual(col, row, color);
        return res;
    }

    /// <summary>그 칸을 둘 수 없게 막고 화면에 표식을 놓는다(돌+칸 제거 등).</summary>
    public void BlockCell(int col, int row)
    {
        Board.Block(col, row);
        View.MarkBlockedVisual(col, row);
    }
}