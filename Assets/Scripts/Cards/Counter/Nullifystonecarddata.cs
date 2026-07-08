using System.Collections;
using UnityEngine;

/// <summary>돌 무효화(카운터): 상대가 방금 둔 돌을, 사용 여부를 물어 지운다.</summary>
[CreateAssetMenu(menuName = "Omok/Cards/Nullify (돌 무효화·카운터)", fileName = "NullifyStoneCard")]
public class NullifyStoneCardData : CardData
{
    public override CardType Type => CardType.Counter;

    public override bool CanCounter(GameEventInfo evt, CardContext ctx)
    {
        return evt.Trigger == GameTrigger.StonePlaced
            && ctx.Board.GetCell(evt.Col, evt.Row) == ctx.Opponent
            && !ctx.IsProtected(evt.Col, evt.Row);
    }

    public override IEnumerator Execute(CardContext ctx)
    {
        int c = ctx.TriggerInfo.Col, r = ctx.TriggerInfo.Row;
        ctx.RemoveStoneAt(c, r);
        Debug.Log($"카운터: 돌 무효화 → ({c},{r})");
        yield break;
    }
}