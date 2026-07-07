using System.Collections;
using UnityEngine;

/// <summary>카드 분류. Active는 내 턴에, Counter는 상대 행동에 반응해, Passive는 상시.</summary>
public enum CardType { Active, Counter, Passive }

/// <summary>패시브가 상시 적용하는 효과의 종류. None은 패시브가 아님을 뜻한다.</summary>
public enum PassiveEffect { None, ExtraStonePerTurn, ProtectOwnStones }

/// <summary>모든 카드 정의의 베이스(ScriptableObject).</summary>
public abstract class CardData : ScriptableObject
{
    public string cardName = "이름 없음";
    [TextArea] public string description = "";

    [Header("이미지 (나중에 채움)")]
    public Sprite artFull;
    public Sprite artIcon;

    public abstract CardType Type { get; }

    /// <summary>패시브 효과(들고만 있어도 적용). 패시브가 아니면 None.</summary>
    public virtual PassiveEffect Passive => PassiveEffect.None;

    /// <summary>지금 이 카드를 (내 턴에) 능동 사용할 수 있는가.</summary>
    public virtual bool CanUse(CardContext ctx) => true;

    /// <summary>이 사건에 카운터로 반응하는가. 기본 false.</summary>
    public virtual bool CanCounter(GameEventInfo evt, CardContext ctx) => false;

    /// <summary>카드 효과(코루틴). 패시브는 능동 실행이 없어 비워둔다.</summary>
    public abstract IEnumerator Execute(CardContext ctx);
}

// ── 액티브 ──

/// <summary>돌 1개 제거: 상대 돌 하나를 골라 지운다.(보호 패시브가 걸린 돌은 대상 제외)</summary>
[CreateAssetMenu(menuName = "Omok/Cards/RemoveStone (돌 1개 제거)", fileName = "RemoveStoneCard")]
public class RemoveStoneCardData : CardData
{
    public override CardType Type => CardType.Active;

    public override IEnumerator Execute(CardContext ctx)
    {
        yield return ctx.PickTarget((c, r) =>
            ctx.Board.GetCell(c, r) == ctx.Opponent && !ctx.IsProtected(c, r));
        if (ctx.Cancelled) yield break;

        ctx.RemoveStoneAt(ctx.PickedCol, ctx.PickedRow);
        Debug.Log($"돌 제거 사용 → ({ctx.PickedCol},{ctx.PickedRow})");
    }
}

/// <summary>추가 돌 두기: 빈 칸을 골라 내 색 돌을 하나 더 놓는다.</summary>
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

/// <summary>돌+칸 제거: 상대 돌 하나를 지우고 그 칸을 막는다.(보호된 돌은 대상 제외)</summary>
[CreateAssetMenu(menuName = "Omok/Cards/RemoveAndBlock (돌+칸 제거)", fileName = "RemoveAndBlockCard")]
public class RemoveAndBlockCardData : CardData
{
    public override CardType Type => CardType.Active;

    public override IEnumerator Execute(CardContext ctx)
    {
        yield return ctx.PickTarget((c, r) =>
            ctx.Board.GetCell(c, r) == ctx.Opponent && !ctx.IsProtected(c, r));
        if (ctx.Cancelled) yield break;

        int c2 = ctx.PickedCol, r2 = ctx.PickedRow;
        ctx.RemoveStoneAt(c2, r2);
        ctx.BlockCell(c2, r2);
        Debug.Log($"돌+칸 제거 → ({c2},{r2}) 제거 후 차단");
    }
}

// ── 카운터 ──

/// <summary>돌 무효화(카운터): 상대가 방금 둔 돌을, 사용 여부를 물어 지운다.</summary>
[CreateAssetMenu(menuName = "Omok/Cards/Nullify (돌 무효화·카운터)", fileName = "NullifyStoneCard")]
public class NullifyStoneCardData : CardData
{
    public override CardType Type => CardType.Counter;

    public override bool CanCounter(GameEventInfo evt, CardContext ctx)
    {
        return evt.Trigger == GameTrigger.StonePlaced
            && ctx.Board.GetCell(evt.Col, evt.Row) == ctx.Opponent
            && !ctx.IsProtected(evt.Col, evt.Row);   // 보호된 돌은 무효화 불가
    }

    public override IEnumerator Execute(CardContext ctx)
    {
        int c = ctx.TriggerInfo.Col, r = ctx.TriggerInfo.Row;
        ctx.RemoveStoneAt(c, r);
        Debug.Log($"카운터: 돌 무효화 → ({c},{r})");
        yield break;
    }
}

// ── 패시브 ──

/// <summary>패시브: 내 턴에 돌을 두 번 둘 수 있다(들고 있는 동안 상시).</summary>
[CreateAssetMenu(menuName = "Omok/Cards/Passive-DoubleMove (턴당 돌 2회)", fileName = "DoubleMovePassive")]
public class DoubleMovePassiveData : CardData
{
    public override CardType Type => CardType.Passive;
    public override PassiveEffect Passive => PassiveEffect.ExtraStonePerTurn;
    public override IEnumerator Execute(CardContext ctx) { yield break; }  // 능동 실행 없음
}

/// <summary>패시브: 내 돌이 상대의 돌 제거·무효화 카드에 파괴되지 않는다(상시).</summary>
[CreateAssetMenu(menuName = "Omok/Cards/Passive-Guard (돌 파괴 방지)", fileName = "GuardPassive")]
public class GuardPassiveData : CardData
{
    public override CardType Type => CardType.Passive;
    public override PassiveEffect Passive => PassiveEffect.ProtectOwnStones;
    public override IEnumerator Execute(CardContext ctx) { yield break; }
}