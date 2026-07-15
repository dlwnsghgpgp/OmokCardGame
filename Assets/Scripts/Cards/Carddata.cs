using System.Collections;
using UnityEngine;

/// <summary>카드 분류. Active=내 턴에, Counter=상대 행동에 반응, Passive=상시, Field=양쪽 모두에 영향.</summary>
public enum CardType { Active, Counter, Passive, Field }

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