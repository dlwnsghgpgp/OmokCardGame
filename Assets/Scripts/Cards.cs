using System.Collections;
using UnityEngine;

/// <summary>카드 분류. Active는 내 턴에, Counter는 상대 행동에 반응해, Passive는 상시(8c).</summary>
public enum CardType { Active, Counter, Passive }

/// <summary>
/// 모든 카드 정의의 베이스(ScriptableObject). 메타데이터 + 이미지 + 효과(Execute).
/// </summary>
public abstract class CardData : ScriptableObject
{
    public string cardName = "이름 없음";
    [TextArea] public string description = "";

    [Header("이미지 (나중에 채움)")]
    public Sprite artFull;
    public Sprite artIcon;

    public abstract CardType Type { get; }

    /// <summary>지금 이 카드를 (내 턴에) 쓸 수 있는가. 미구현·대상없음 등이면 false.</summary>
    public virtual bool CanUse(CardContext ctx) => true;

    /// <summary>이 사건에 카운터로 반응하는가. 카운터가 아닌 카드는 기본 false.</summary>
    public virtual bool CanCounter(GameEventInfo evt, CardContext ctx) => false;

    /// <summary>카드 효과(코루틴). 액티브·카운터 모두 이걸 실행한다.</summary>
    public abstract IEnumerator Execute(CardContext ctx);
}

/// <summary>돌 1개 제거: 상대 돌 하나를 골라 지운다. (액티브)</summary>
[CreateAssetMenu(menuName = "Omok/Cards/RemoveStone (돌 1개 제거)", fileName = "RemoveStoneCard")]
public class RemoveStoneCardData : CardData
{
    public override CardType Type => CardType.Active;

    public override IEnumerator Execute(CardContext ctx)
    {
        yield return ctx.PickTarget((c, r) => ctx.Board.GetCell(c, r) == ctx.Opponent);
        if (ctx.Cancelled) yield break;

        ctx.RemoveStoneAt(ctx.PickedCol, ctx.PickedRow);
        Debug.Log($"돌 제거 사용 → ({ctx.PickedCol},{ctx.PickedRow})");
    }
}

/// <summary>추가 돌 두기: 빈 칸을 골라 내 색 돌을 하나 더 놓는다. (액티브)</summary>
[CreateAssetMenu(menuName = "Omok/Cards/ExtraStone (추가 돌 두기)", fileName = "ExtraStoneCard")]
public class ExtraStoneCardData : CardData
{
    public override CardType Type => CardType.Active;

    public override IEnumerator Execute(CardContext ctx)
    {
        yield return ctx.PickTarget((c, r) => ctx.Board.IsPlayable(c, r));
        if (ctx.Cancelled) yield break;

        var res = ctx.PlaceStoneAt(ctx.PickedCol, ctx.PickedRow, ctx.User);
        Debug.Log($"추가 돌 두기 → ({ctx.PickedCol},{ctx.PickedRow}) 득점:{res.PointsScored}");
    }
}

/// <summary>돌+칸 제거: 상대 돌 하나를 지우고 그 칸을 막는다. (액티브)</summary>
[CreateAssetMenu(menuName = "Omok/Cards/RemoveAndBlock (돌+칸 제거)", fileName = "RemoveAndBlockCard")]
public class RemoveAndBlockCardData : CardData
{
    public override CardType Type => CardType.Active;

    public override IEnumerator Execute(CardContext ctx)
    {
        yield return ctx.PickTarget((c, r) => ctx.Board.GetCell(c, r) == ctx.Opponent);
        if (ctx.Cancelled) yield break;

        int c2 = ctx.PickedCol, r2 = ctx.PickedRow;
        ctx.RemoveStoneAt(c2, r2);
        ctx.BlockCell(c2, r2);
        Debug.Log($"돌+칸 제거 → ({c2},{r2}) 제거 후 차단");
    }
}

/// <summary>
/// 돌 무효화 (카운터): 상대가 방금 둔 돌을, 사용 여부를 물어 지운다.
/// 사건이 좌표를 알려주므로 타겟팅이 필요 없다.
/// </summary>
[CreateAssetMenu(menuName = "Omok/Cards/Nullify (돌 무효화·카운터)", fileName = "NullifyStoneCard")]
public class NullifyStoneCardData : CardData
{
    public override CardType Type => CardType.Counter;

    // 상대가 돌을 놨고, 그 자리에 아직 상대 돌이 있으면 반응 가능.
    public override bool CanCounter(GameEventInfo evt, CardContext ctx)
    {
        return evt.Trigger == GameTrigger.StonePlaced
            && ctx.Board.GetCell(evt.Col, evt.Row) == ctx.Opponent;
    }

    public override IEnumerator Execute(CardContext ctx)
    {
        int c = ctx.TriggerInfo.Col, r = ctx.TriggerInfo.Row;
        ctx.RemoveStoneAt(c, r);   // 방금 둔 돌 제거
        Debug.Log($"카운터: 돌 무효화 → ({c},{r})");
        yield break;
    }
}