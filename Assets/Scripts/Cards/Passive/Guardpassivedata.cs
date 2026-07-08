using System.Collections;
using UnityEngine;

/// <summary>패시브: 내 돌이 상대의 돌 제거·무효화 카드에 파괴되지 않는다(상시).</summary>
[CreateAssetMenu(menuName = "Omok/Cards/Passive-Guard (돌 파괴 방지)", fileName = "GuardPassive")]
public class GuardPassiveData : CardData
{
    public override CardType Type => CardType.Passive;
    public override PassiveEffect Passive => PassiveEffect.ProtectOwnStones;
    public override IEnumerator Execute(CardContext ctx) { yield break; }
}