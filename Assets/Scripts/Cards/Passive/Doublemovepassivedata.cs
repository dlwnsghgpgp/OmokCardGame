using System.Collections;
using UnityEngine;

/// <summary>패시브: 내 턴에 돌을 두 번 둘 수 있다(들고 있는 동안 상시).</summary>
[CreateAssetMenu(menuName = "Omok/Cards/Passive-DoubleMove (턴당 돌 2회)", fileName = "DoubleMovePassive")]
public class DoubleMovePassiveData : CardData
{
    public override CardType Type => CardType.Passive;
    public override PassiveEffect Passive => PassiveEffect.ExtraStonePerTurn;
    public override IEnumerator Execute(CardContext ctx) { yield break; }
}