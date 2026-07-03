using System.Collections;
using UnityEngine;

/// <summary>카드 분류. 이번(8a)에는 Active만 실제로 쓰고, Counter/Passive는 8b·8c에서.</summary>
public enum CardType { Active, Counter, Passive }

/// <summary>
/// 모든 카드 정의의 베이스(ScriptableObject). 메타데이터 + 이미지 + 효과(Execute).
/// 이미지 필드는 8a-2b에서 손패 UI가 참조한다(지금은 비워둬도 됨).
/// </summary>
public abstract class CardData : ScriptableObject
{
    public string cardName = "이름 없음";
    [TextArea] public string description = "";

    [Header("이미지 (나중에 채움)")]
    public Sprite artFull;   // 호버 시 크게 보일 전체 이미지
    public Sprite artIcon;   // 손패에 보일 작은 이미지(없으면 artFull 사용)

    public abstract CardType Type { get; }

    /// <summary>지금 이 카드를 쓸 수 있는가(미구현·대상없음 등이면 false → UI 비활성/무시).</summary>
    public virtual bool CanUse(CardContext ctx) => true;

    /// <summary>카드 효과(코루틴). 타겟팅 대기가 있어 IEnumerator로 둔다.</summary>
    public abstract IEnumerator Execute(CardContext ctx);
}

/// <summary>돌 1개 제거: 상대 돌 하나를 골라 지운다.</summary>
[CreateAssetMenu(menuName = "Omok/Cards/RemoveStone (돌 1개 제거)", fileName = "RemoveStoneCard")]
public class RemoveStoneCardData : CardData
{
    public override CardType Type => CardType.Active;

    public override IEnumerator Execute(CardContext ctx)
    {
        // 상대 돌이 있는 칸만 유효 대상으로 타겟팅.
        yield return ctx.PickTarget((c, r) => ctx.Board.GetCell(c, r) == ctx.Opponent);
        if (ctx.Cancelled) yield break;

        ctx.RemoveStoneAt(ctx.PickedCol, ctx.PickedRow);
        Debug.Log($"돌 제거 사용 → ({ctx.PickedCol},{ctx.PickedRow})");
    }
}

/// <summary>추가 돌 두기: 8a-3에서 구현 예정.</summary>
[CreateAssetMenu(menuName = "Omok/Cards/ExtraStone (추가 돌 두기)", fileName = "ExtraStoneCard")]
public class ExtraStoneCardData : CardData
{
    public override CardType Type => CardType.Active;
    public override bool CanUse(CardContext ctx) => false;         // 8a-3에서 활성화
    public override IEnumerator Execute(CardContext ctx) { yield break; }
}

/// <summary>돌+칸 제거: 8a-3에서 구현 예정(막힌 칸 기능 필요).</summary>
[CreateAssetMenu(menuName = "Omok/Cards/RemoveAndBlock (돌+칸 제거)", fileName = "RemoveAndBlockCard")]
public class RemoveAndBlockCardData : CardData
{
    public override CardType Type => CardType.Active;
    public override bool CanUse(CardContext ctx) => false;         // 8a-3에서 활성화
    public override IEnumerator Execute(CardContext ctx) { yield break; }
}