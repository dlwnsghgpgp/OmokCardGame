using System.Collections.Generic;

/// <summary>공유 덱. 카드 목록을 섞어두고 위에서 한 장씩 뽑는다. 순수 C# 클래스.</summary>
public class Deck
{
    private readonly List<CardData> _cards = new List<CardData>();
    public int Count => _cards.Count;

    public Deck(IEnumerable<CardData> cards)
    {
        if (cards != null)
            foreach (var c in cards)
                if (c != null) _cards.Add(c);
    }

    public void Shuffle()
    {
        // 피셔-예이츠 셔플
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    /// <summary>맨 위 카드를 뽑아 반환. 비어 있으면 null.</summary>
    public CardData Draw()
    {
        if (_cards.Count == 0) return null;
        int last = _cards.Count - 1;
        var top = _cards[last];
        _cards.RemoveAt(last);
        return top;
    }
}

/// <summary>한 플레이어의 손패.</summary>
public class Hand
{
    private readonly List<CardData> _cards = new List<CardData>();
    public int Count => _cards.Count;
    public IReadOnlyList<CardData> Cards => _cards;

    public void Add(CardData c) { if (c != null) _cards.Add(c); }
    public CardData Get(int i) => (i >= 0 && i < _cards.Count) ? _cards[i] : null;
    public void RemoveAt(int i) { if (i >= 0 && i < _cards.Count) _cards.RemoveAt(i); }
    public void Clear() => _cards.Clear();
}