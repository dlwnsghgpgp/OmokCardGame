using System;
using System.Collections;

/// <summary>
/// 카드가 발동될 때 받는 "도구 상자". 보드/화면 접근, 내 색·상대 색,
/// 타겟 요청, 보드+화면 동시 조작, 그리고 패시브(보호 등) 조회를 제공한다.
/// </summary>
public class CardContext
{
    public BoardState Board { get; }
    public BoardView View { get; }
    public CellState User { get; }
    public CellState Opponent { get; }

    public bool Cancelled { get; private set; }
    public int PickedCol { get; private set; }
    public int PickedRow { get; private set; }

    // 카운터 실행 시 GameManager가 채워주는 사건 정보.
    public GameEventInfo TriggerInfo;

    // 특정 색 플레이어가 특정 패시브를 보유했는지 묻는 함수(GameManager가 주입).
    public Func<CellState, PassiveEffect, bool> HasPassive;

    public CardContext(BoardState board, BoardView view, CellState user)
    {
        Board = board;
        View = view;
        User = user;
        Opponent = (user == CellState.Black) ? CellState.White : CellState.Black;
    }

    public IEnumerator PickTarget(Func<int, int, bool> isValid)
    {
        bool done = false;
        Cancelled = false;

        View.BeginTargeting(
            isValid,
            (c, r) => { PickedCol = c; PickedRow = r; Cancelled = false; done = true; },
            ()     => { Cancelled = true; done = true; });

        while (!done) yield return null;
    }

    /// <summary>그 칸의 돌이 보호 패시브(ProtectOwnStones)로 보호받고 있는가.</summary>
    public bool IsProtected(int col, int row)
    {
        CellState owner = Board.GetCell(col, row);
        return owner != CellState.Empty
            && HasPassive != null
            && HasPassive(owner, PassiveEffect.ProtectOwnStones);
    }

    // ── 고수준 조작 ──

    /// <summary>돌 제거. 단, 보호된 돌은 지우지 않는다(모든 제거 카드가 이 길목을 지난다).</summary>
    public CellState RemoveStoneAt(int col, int row)
    {
        if (IsProtected(col, row))
        {
            UnityEngine.Debug.Log($"({col},{row}) 돌은 보호 패시브로 제거되지 않았습니다.");
            return CellState.Empty;
        }
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

    /// <summary>그 칸을 둘 수 없게 막고 화면에 표식을 놓는다.</summary>
    public void BlockCell(int col, int row)
    {
        Board.Block(col, row);
        View.MarkBlockedVisual(col, row);
    }
}