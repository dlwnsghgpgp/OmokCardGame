using UnityEngine;

// 3단계 동작 확인용 임시 스크립트. 4단계에서 GameManager로 대체되므로 그때 삭제.
public class BoardViewTester : MonoBehaviour
{
    public BoardView view;
    private BoardState _board;
    private CellState _turn = CellState.Black;

    void Awake()
    {
        _board = new BoardState(15);
        if (view == null) view = GetComponent<BoardView>();
    }

    void OnEnable()  { view.CellClicked += OnCellClicked; }
    void OnDisable() { view.CellClicked -= OnCellClicked; }

    void OnCellClicked(int col, int row)
    {
        if (!_board.IsEmpty(col, row)) return;          // 빈 자리만

        var result = _board.PlaceStone(col, row, _turn);
        if (!result.Success) return;

        view.PlaceStoneVisual(col, row, _turn);
        Debug.Log($"{_turn} ({col},{row}) 득점:{result.PointsScored} " +
                  $"| 흑 {_board.BlackScore} : 백 {_board.WhiteScore}");

        _turn = (_turn == CellState.Black) ? CellState.White : CellState.Black;
    }
}