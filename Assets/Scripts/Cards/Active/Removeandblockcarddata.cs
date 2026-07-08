using System.Collections;
using UnityEngine;

/// <summary>돌+칸 제거: 상대 돌 하나를 지우고 그 칸을 막는다(보호된 돌은 대상 제외).</summary>
[CreateAssetMenu(menuName = "Omok/Cards/RemoveAndBlock (돌+칸 제거)", fileName = "RemoveAndBlockCard")]
public class RemoveAndBlockCardData : CardData
{
    public override CardType Type => CardType.Active;

    // 지울 수 있는(보호 안 된) 상대 돌이 하나라도 있어야 사용 가능.
    public override bool CanUse(CardContext ctx)
    {
        int size = ctx.Board.Size;
        for (int c = 0; c < size; c++)
        for (int r = 0; r < size; r++)
            if (ctx.Board.GetCell(c, r) == ctx.Opponent && !ctx.IsProtected(c, r))
                return true;
        return false;
    }

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