using System.Collections;
using UnityEngine;

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