using System.Collections;
using UnityEngine;

/// <summary>돌 1개 제거: 상대 돌 하나를 골라 지운다(보호된 돌은 대상 제외).</summary>
[CreateAssetMenu(menuName = "Omok/Cards/RemoveStone (돌 1개 제거)", fileName = "RemoveStoneCard")]
public class RemoveStoneCardData : CardData
{
    public override CardType Type => CardType.Active;

    // 지울 수 있는(보호 안 된) 상대 돌이 하나라도 있어야 사용 가능.
    public override bool CanUse(CardContext ctx) => HasValidTarget(ctx);

    public override IEnumerator Execute(CardContext ctx)
    {
        yield return ctx.PickTarget((c, r) =>
            ctx.Board.GetCell(c, r) == ctx.Opponent && !ctx.IsProtected(c, r));
        if (ctx.Cancelled) yield break;

        ctx.RemoveStoneAt(ctx.PickedCol, ctx.PickedRow);
        Debug.Log($"돌 제거 사용 → ({ctx.PickedCol},{ctx.PickedRow})");
    }

    // 제거 가능한 상대 돌이 보드에 존재하는가.
    private bool HasValidTarget(CardContext ctx)
    {
        int size = ctx.Board.Size;
        for (int c = 0; c < size; c++)
        for (int r = 0; r < size; r++)
            if (ctx.Board.GetCell(c, r) == ctx.Opponent && !ctx.IsProtected(c, r))
                return true;
        return false;
    }
}