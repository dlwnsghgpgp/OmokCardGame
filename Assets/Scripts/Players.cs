using System;

/// <summary>
/// 한 명의 플레이어(사람/AI/네트워크)가 공통으로 가지는 역할.
/// "자기 차례에 수를 하나 정한다"는 것만 정의한다.
/// 이 추상화 덕분에 6단계에서 사람 자리를 AI로, 나중엔 네트워크로 갈아끼울 수 있다.
/// </summary>
public interface IPlayerAgent
{
    // 자기 차례가 되면 GameManager가 호출. 수를 정하면 onChosen(col,row)을 정확히 한 번 호출한다.
    void RequestMove(BoardState board, CellState myColor, Action<int, int> onChosen);

    // 진행 중인 입력 대기를 취소/정리한다(게임 종료·재시작 시).
    void Cancel();
}

/// <summary>
/// 사람 플레이어. BoardView의 클릭 이벤트를 "자기 차례에만" 구독했다가,
/// 빈 자리를 클릭하면 그 좌표를 수로 확정한다.
/// MonoBehaviour가 아니라 일반 C# 클래스 — GameManager가 new로 생성한다.
/// </summary>
public class HumanPlayer : IPlayerAgent
{
    private readonly BoardView _view;
    private BoardState _board;
    private Action<int, int> _onChosen;

    public HumanPlayer(BoardView view) { _view = view; }

    public void RequestMove(BoardState board, CellState myColor, Action<int, int> onChosen)
    {
        _board = board;
        _onChosen = onChosen;
        _view.CellClicked += HandleClick;   // 내 차례 동안만 클릭을 듣는다
    }

    private void HandleClick(int col, int row)
    {
        // 빈 자리가 아니면 무시하고 다음 클릭을 계속 기다린다.
        if (_board == null || !_board.IsEmpty(col, row)) return;

        var callback = _onChosen;
        Cancel();                   // 먼저 구독 해제(중복 입력 방지) 후
        callback?.Invoke(col, row); // 수를 확정해 GameManager에 알린다
    }

    public void Cancel()
    {
        _view.CellClicked -= HandleClick;
        _onChosen = null;
        _board = null;
    }
}