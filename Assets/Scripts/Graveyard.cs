using System.Collections.Generic;

/// <summary>묘지에 쌓인 한 장의 기록: 어떤 카드였고, 원래 누구 것이었나.</summary>
public struct GraveEntry
{
    public CardData Card;
    public CellState Owner;   // 이 카드를 원래 들고 있던 플레이어(회수 기능 대비)

    public GraveEntry(CardData card, CellState owner)
    {
        Card = card;
        Owner = owner;
    }
}

/// <summary>
/// 공용 묘지. 사용·폐기된 카드가 주인 정보와 함께 버려진 순서대로 쌓인다.
/// 순수 데이터 — 표시는 GraveyardView, 관리 지점은 GameManager.
/// </summary>
public class Graveyard
{
    private readonly List<GraveEntry> _entries = new List<GraveEntry>();

    public int Count => _entries.Count;
    public IReadOnlyList<GraveEntry> Entries => _entries;

    /// <summary>맨 위(가장 최근에 버려진) 카드. 비어 있으면 null.</summary>
    public GraveEntry? Top => _entries.Count > 0 ? _entries[_entries.Count - 1] : (GraveEntry?)null;

    public void Add(CardData card, CellState owner)
    {
        if (card != null) _entries.Add(new GraveEntry(card, owner));
    }

    public void Clear() => _entries.Clear();
}