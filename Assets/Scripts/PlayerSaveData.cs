using System;
using System.Collections.Generic;

/// <summary>
/// 저장되는 덱 하나. 이름과 카드 ID 목록을 갖는다.
/// (JsonUtility는 Dictionary를 직렬화하지 못하므로 List 기반으로 둔다.)
/// </summary>
[Serializable]
public class DeckSaveData
{
    public string deckName = "새 덱";
    public List<string> cardIds = new List<string>();

    public DeckSaveData() { }

    public DeckSaveData(string name, IEnumerable<string> ids)
    {
        deckName = name;
        cardIds = new List<string>();
        if (ids != null) cardIds.AddRange(ids);
    }
}

/// <summary>
/// 플레이어의 저장 데이터 전체. JSON 파일 하나로 저장된다.
///  - ownedCardIds: 소지한 카드 ID(덱에 넣을 수 있는 카드)
///  - decks: 만들어 둔 덱들
///  - selectedDeckIndex: 게임 시작 시 쓸 덱
///  - lastThemeId: 마지막에 고른 테마
/// </summary>
[Serializable]
public class PlayerSaveData
{
    public List<string> ownedCardIds = new List<string>();
    public List<DeckSaveData> decks = new List<DeckSaveData>();
    public int selectedDeckIndex = 0;
    public string lastThemeId = "";

    /// <summary>그 카드를 소지하고 있는가.</summary>
    public bool Owns(string cardId)
        => !string.IsNullOrEmpty(cardId) && ownedCardIds.Contains(cardId);

    /// <summary>카드를 소지 목록에 추가한다(중복 방지).</summary>
    public void AddOwned(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return;
        if (!ownedCardIds.Contains(cardId)) ownedCardIds.Add(cardId);
    }

    /// <summary>현재 선택된 덱. 없으면 null.</summary>
    public DeckSaveData GetSelectedDeck()
    {
        if (decks == null || decks.Count == 0) return null;
        int i = Mathf_Clamp(selectedDeckIndex, 0, decks.Count - 1);
        return decks[i];
    }

    // UnityEngine 의존을 피하기 위한 간단한 clamp(이 클래스는 순수 데이터로 둔다).
    private static int Mathf_Clamp(int v, int min, int max)
        => v < min ? min : (v > max ? max : v);
}