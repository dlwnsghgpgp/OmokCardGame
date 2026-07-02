using UnityEngine;

/// <summary>카드 분류. 이번(8a)에는 Active만 실제로 쓰고, Counter/Passive는 8b·8c에서.</summary>
public enum CardType { Active, Counter, Passive }

/// <summary>
/// 모든 카드 정의의 베이스. ScriptableObject라서 에디터에서 에셋으로 만들어 관리한다.
/// 지금은 메타데이터만. 효과(Execute)는 8a-2에서 CardContext와 함께 추가한다.
/// </summary>
public abstract class CardData : ScriptableObject
{
    public string cardName = "이름 없음";
    [TextArea] public string description = "";
    public abstract CardType Type { get; }
}

[CreateAssetMenu(menuName = "Omok/Cards/RemoveStone (돌 1개 제거)", fileName = "RemoveStoneCard")]
public class RemoveStoneCardData : CardData
{
    public override CardType Type => CardType.Active;
}

[CreateAssetMenu(menuName = "Omok/Cards/ExtraStone (추가 돌 두기)", fileName = "ExtraStoneCard")]
public class ExtraStoneCardData : CardData
{
    public override CardType Type => CardType.Active;
}

[CreateAssetMenu(menuName = "Omok/Cards/RemoveAndBlock (돌+칸 제거)", fileName = "RemoveAndBlockCard")]
public class RemoveAndBlockCardData : CardData
{
    public override CardType Type => CardType.Active;
}