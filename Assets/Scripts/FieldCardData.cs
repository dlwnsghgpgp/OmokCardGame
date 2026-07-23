using System.Collections;

/// <summary>
/// 필드 카드의 베이스. 주인이 없고 양쪽 플레이어 모두에게 영향을 준다.
/// 손패에 들어오지 않고, 테마가 정한 규칙에 따라 필드 덱에서 뽑혀 필드에 깔린다.
///
/// 훅 4종:
///  - OnActivated: 필드에 깔리는 순간 1회(초기 설정. 예: 좀비 숙주 지정)
///  - OnTurnBegin: 매 턴 시작마다(지속 효과. 예: 감염 확산)
///  - OnRemoved:   다른 필드 카드로 교체되어 내려갈 때 1회(반복 테마에서만 발생)
///  - CheckWin:    승리 조건을 덮어쓴다(예: 한쪽 돌 전멸 시 패배)
/// </summary>
public abstract class FieldCardData : CardData
{
    public override CardType Type => CardType.Field;

    /// <summary>필드 카드는 손패에서 능동 사용되지 않는다.</summary>
    public override IEnumerator Execute(CardContext ctx) { yield break; }
    public override bool CanUse(CardContext ctx) => false;

    /// <summary>필드에 깔리는 순간 1회 호출.</summary>
    public virtual void OnActivated(FieldContext ctx) { }

    /// <summary>매 턴 시작 시 호출(지속 효과).</summary>
    public virtual void OnTurnBegin(FieldContext ctx) { }

    /// <summary>
    /// 다른 필드 카드로 교체되어 필드에서 내려갈 때 1회 호출.
    /// 남긴 흔적(감염 상태 등)을 되돌릴지 그대로 둘지는 카드가 정한다.
    /// </summary>
    public virtual void OnRemoved(FieldContext ctx) { }

    /// <summary>
    /// 이 필드 카드가 정하는 승패. true를 반환하면 게임이 끝나고 result가 결과 문구가 된다.
    /// </summary>
    public virtual bool CheckWin(FieldContext ctx, out string result)
    {
        result = null;
        return false;
    }

    /// <summary>
    /// true면 기존 종료 조건(고정 턴 수·목표 점수)을 무시하고
    /// 이 카드의 CheckWin으로만 승패를 가린다.
    /// </summary>
    public virtual bool SuppressNormalEnd => false;
}