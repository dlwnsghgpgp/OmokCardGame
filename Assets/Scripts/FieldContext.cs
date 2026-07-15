/// <summary>
/// 필드 카드가 받는 도구 상자. 필드 카드는 주인이 없으므로 User/Opponent가 없다.
/// 보드 데이터와 화면에 접근해 양쪽 모두에게 영향을 주는 효과를 실행한다.
/// </summary>
public class FieldContext
{
    public BoardState Board { get; }
    public BoardView View { get; }

    public FieldContext(BoardState board, BoardView view)
    {
        Board = board;
        View = view;
    }
}