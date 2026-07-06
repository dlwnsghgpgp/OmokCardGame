/// <summary>게임 중 일어나는 사건의 종류. 8b에선 StonePlaced만. 이후 확장.</summary>
public enum GameTrigger
{
    StonePlaced,   // 돌이 놓였다
    // 이후: CardPlayed, StoneRemoved, LineScored ...
}

/// <summary>
/// 카운터 카드에게 전달되는 사건 정보. "누가, 어디에, 어떤 결과로" 행동했는지 담는다.
/// </summary>
public struct GameEventInfo
{
    public GameTrigger Trigger;
    public CellState Actor;    // 이 행동을 한 플레이어
    public int Col, Row;       // 관련 칸(StonePlaced: 놓인 위치)
    public int PointsScored;   // 그 수로 얻은 점수(줄 완성 여부 판단 등에 사용)
}