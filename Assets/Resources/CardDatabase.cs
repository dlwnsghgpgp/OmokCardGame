using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 카드 에셋을 모아두고 "ID → 카드" 조회를 제공하는 중앙 데이터베이스.
/// JSON 덱은 카드 ID 목록으로 저장되므로, 게임 씬은 이 DB로 실제 카드 에셋을 찾아온다.
/// 씬이 바뀌어도 살아있게 하려면 게임 전역에서 하나만 참조하도록 둔다(10-2에서 연결).
///
/// 에셋으로 하나 만들어(Create → Omok/CardDatabase) allCards에 모든 카드를 넣는다.
/// </summary>
[CreateAssetMenu(menuName = "Omok/CardDatabase", fileName = "CardDatabase")]
public class CardDatabase : ScriptableObject
{
    [Tooltip("게임에 존재하는 모든 카드 에셋을 여기에 넣는다.")]
    public List<CardData> allCards = new List<CardData>();

    private Dictionary<string, CardData> _byId;

    private void EnsureBuilt()
    {
        if (_byId != null) return;
        _byId = new Dictionary<string, CardData>();

        foreach (var card in allCards)
        {
            if (card == null) continue;
            if (string.IsNullOrEmpty(card.id))
            {
                Debug.LogWarning($"[CardDatabase] ID가 비어 있는 카드: {card.name}");
                continue;
            }
            if (_byId.ContainsKey(card.id))
            {
                Debug.LogError($"[CardDatabase] ID 중복: '{card.id}' " +
                               $"({_byId[card.id].name} 와 {card.name})");
                continue;
            }
            _byId[card.id] = card;
        }
        Debug.Log($"[CardDatabase] 카드 {_byId.Count}종 로드 완료.");
    }

    /// <summary>ID로 카드를 찾는다. 없으면 null.</summary>
    public CardData Get(string id)
    {
        EnsureBuilt();
        return (id != null && _byId.TryGetValue(id, out var card)) ? card : null;
    }

    /// <summary>ID 목록으로 카드 목록을 만든다(JSON 덱 → 실제 카드). 없는 ID는 건너뛴다.</summary>
    public List<CardData> GetMany(IEnumerable<string> ids)
    {
        var result = new List<CardData>();
        if (ids == null) return result;
        foreach (var id in ids)
        {
            var card = Get(id);
            if (card != null) result.Add(card);
            else Debug.LogWarning($"[CardDatabase] 알 수 없는 카드 ID: '{id}'");
        }
        return result;
    }

    /// <summary>모든 카드(소지 목록 초기화·덱 편집 카드 풀 등에 사용).</summary>
    public IReadOnlyList<CardData> AllCards => allCards;

    /// <summary>런타임에 DB를 다시 짓게 한다(에디터에서 카드를 추가한 뒤 등).</summary>
    public void Rebuild() { _byId = null; EnsureBuilt(); }
}