using System;

/// <summary>한 턴에 플레이어가 할 수 있는 행동의 종류.</summary>
public enum TurnActionType { PlaceStone, PlayCard }

/// <summary>플레이어가 고른 행동. 돌 두기(좌표) 또는 카드 쓰기(손패 인덱스).</summary>
public struct TurnAction
{
    public TurnActionType Type;
    public int Col, Row;      // PlaceStone
    public int CardIndex;     // PlayCard

    public static TurnAction Place(int col, int row) =>
        new TurnAction { Type = TurnActionType.PlaceStone, Col = col, Row = row };

    public static TurnAction Card(int handIndex) =>
        new TurnAction { Type = TurnActionType.PlayCard, CardIndex = handIndex };
}

/// <summary>
/// 플레이어(사람/AI/네트워크) 공통 역할. "이번에 할 행동을 정한다".
/// 돌을 두면 그 턴이 끝난다. 카드를 쓰면 GameManager가 처리 후 다시 행동을 요청한다.
/// </summary>
public interface IPlayerAgent
{
    void RequestAction(BoardState board, CellState myColor, Action<TurnAction> onAction);
    void Tick(float deltaTime);
    void Cancel();
}

/// <summary>
/// 사람 플레이어. 보드 클릭 → 돌 두기 행동. UI 카드 클릭 → GameManager가 SubmitCardChoice로 전달.
/// </summary>
public class HumanPlayer : IPlayerAgent
{
    private readonly BoardView _view;
    private BoardState _board;
    private Action<TurnAction> _onAction;

    public HumanPlayer(BoardView view) { _view = view; }

    public void RequestAction(BoardState board, CellState myColor, Action<TurnAction> onAction)
    {
        _board = board;
        _onAction = onAction;
        _view.CellClicked += HandleBoardClick;
    }

    public void Tick(float deltaTime) { }

    /// <summary>UI에서 카드가 클릭됐을 때 GameManager가 호출. 지금 행동 대기 중이면 카드 행동을 낸다.</summary>
    public void SubmitCardChoice(int handIndex)
    {
        if (_onAction == null) return;   // 지금 이 사람 차례의 행동 대기 중이 아니면 무시
        var cb = _onAction;
        Cancel();
        cb(TurnAction.Card(handIndex));
    }

    private void HandleBoardClick(int col, int row)
    {
        if (_board == null || !_board.IsPlayable(col, row)) return;   // 둘 수 있는 칸만
        var cb = _onAction;
        Cancel();
        cb(TurnAction.Place(col, row));
    }

    public void Cancel()
    {
        _view.CellClicked -= HandleBoardClick;
        _onAction = null;
        _board = null;
    }
}